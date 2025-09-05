using System.Collections.ObjectModel;
using System.ComponentModel;
using COGLyricsScanner.Models;
using COGLyricsScanner.Services;
using COGLyricsScanner.Helpers;
using System.Windows.Input;

namespace COGLyricsScanner.ViewModels;

public class CollectionsPageViewModel : BaseViewModel
{
    private readonly IDatabaseService _databaseService;
    private readonly IExportService _exportService;
    private ObservableCollection<Collection> _collections;
    private ObservableCollection<Collection> _filteredCollections;
    private string _searchText;
    private Timer _searchTimer;

    public CollectionsPageViewModel()
    {
        _databaseService = ServiceHelper.GetService<IDatabaseService>();
        _exportService = ServiceHelper.GetService<IExportService>();
        
        _collections = new ObservableCollection<Collection>();
        _filteredCollections = new ObservableCollection<Collection>();
        
        RefreshCommand = new Command(async () => await LoadCollectionsAsync());
        
        Title = "Collections";
    }

    public ObservableCollection<Collection> Collections
    {
        get => _filteredCollections;
        set => SetProperty(ref _filteredCollections, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                PerformSearch();
            }
        }
    }

    public new ICommand RefreshCommand { get; }

    public override async Task OnAppearingAsync()
    {
        await base.OnAppearingAsync();
        await LoadCollectionsAsync();
    }

    private async Task LoadCollectionsAsync()
    {
        await ExecuteAsync(async () =>
        {
            var collections = await _databaseService.GetCollectionsAsync();
            
            // Get hymn counts for each collection
            foreach (var collection in collections)
            {
                var hymnCollections = await _databaseService.GetHymnCollectionsByCollectionIdAsync(collection.Id);
                collection.HymnCount = hymnCollections.Count;
            }
            
            _collections.Clear();
            foreach (var collection in collections.OrderBy(c => c.Name))
            {
                _collections.Add(collection);
            }
            
            ApplyFilter();
            
            IsEmpty = !_collections.Any();
            EmptyMessage = string.IsNullOrWhiteSpace(SearchText) 
                ? "No collections found.\nCreate your first collection to organize your hymns."
                : $"No collections match '{SearchText}'.\nTry a different search term.";
        });
    }

    private void PerformSearch()
    {
        _searchTimer?.Dispose();
        _searchTimer = new Timer(async _ => await Device.InvokeOnMainThreadAsync(ApplyFilter), null, 300, Timeout.Infinite);
    }

    private void ApplyFilter()
    {
        try
        {
            var filtered = _collections.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                filtered = filtered.Where(c => 
                    c.Name.ToLower().Contains(searchLower) ||
                    (c.Description?.ToLower().Contains(searchLower) ?? false));
            }

            Collections.Clear();
            foreach (var collection in filtered)
            {
                Collections.Add(collection);
            }

            IsEmpty = !Collections.Any();
            EmptyMessage = string.IsNullOrWhiteSpace(SearchText) 
                ? "No collections found.\nCreate your first collection to organize your hymns."
                : $"No collections match '{SearchText}'.\nTry a different search term.";
        }
        catch (Exception ex)
        {
            HandleError(ex, "Failed to filter collections");
        }
    }

    public async Task CreateCollectionAsync(string name, string description = null)
    {
        await ExecuteAsync(async () =>
        {
            var collection = new Collection
            {
                Name = name,
                Description = description,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            await _databaseService.SaveCollectionAsync(collection);
            await LoadCollectionsAsync();
            
            await ShowSuccessAsync($"Collection '{name}' created successfully.");
        });
    }

    public async Task UpdateCollectionAsync(Collection collection, string name, string description = null)
    {
        await ExecuteAsync(async () =>
        {
            collection.Name = name;
            collection.Description = description;
            collection.UpdatedAt = DateTime.Now;

            await _databaseService.SaveCollectionAsync(collection);
            await LoadCollectionsAsync();
            
            await ShowSuccessAsync($"Collection '{name}' updated successfully.");
        });
    }

    public async Task DeleteCollectionAsync(Collection collection)
    {
        await ExecuteAsync(async () =>
        {
            // First remove all hymn-collection relationships
            var hymnCollections = await _databaseService.GetHymnCollectionsByCollectionIdAsync(collection.Id);
            foreach (var hymnCollection in hymnCollections)
            {
                await _databaseService.DeleteHymnCollectionAsync(hymnCollection.Id);
            }
            
            // Then delete the collection
            await _databaseService.DeleteCollectionAsync(collection.Id);
            await LoadCollectionsAsync();
            
            await ShowSuccessAsync($"Collection '{collection.Name}' deleted successfully.");
        });
    }

    public async Task ExportCollectionAsync(Collection collection)
    {
        await ExecuteAsync(async () =>
        {
            // Get all hymns in the collection
            var hymnCollections = await _databaseService.GetHymnCollectionsByCollectionIdAsync(collection.Id);
            var hymnIds = hymnCollections.Select(hc => hc.HymnId).ToList();
            
            if (!hymnIds.Any())
            {
                await ShowMessageAsync("Export", "This collection is empty. Nothing to export.");
                return;
            }
            
            var hymns = new List<Hymn>();
            foreach (var hymnId in hymnIds)
            {
                var hymn = await _databaseService.GetHymnAsync(hymnId);
                if (hymn != null)
                {
                    hymns.Add(hymn);
                }
            }
            
            if (hymns.Any())
            {
                var success = await _exportService.ExportHymnsAsync(hymns, ExportFormat.JSON);
                if (success)
                {
                    await ShowSuccessAsync($"Collection '{collection.Name}' exported successfully.");
                }
                else
                {
                    await ShowMessageAsync("Export Failed", "Failed to export collection. Please try again.");
                }
            }
        });
    }

    public async Task NavigateToCollectionAsync(Collection collection)
    {
        try
        {
            await Shell.Current.GoToAsync($"//collection-detail?collectionId={collection.Id}");
        }
        catch (Exception ex)
        {
            HandleError(ex, "Failed to navigate to collection");
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _searchTimer?.Dispose();
        }
        base.Dispose(disposing);
    }
}