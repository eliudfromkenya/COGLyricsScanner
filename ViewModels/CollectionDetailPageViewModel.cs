using System.Collections.ObjectModel;
using COGLyricsScanner.Models;
using COGLyricsScanner.Services;
using COGLyricsScanner.Helpers;
using System.Windows.Input;

namespace COGLyricsScanner.ViewModels;

public class CollectionDetailPageViewModel : BaseViewModel
{
    private readonly IDatabaseService _databaseService;
    private readonly Collection _collection;
    private readonly System.Timers.Timer _searchTimer;
    
    private ObservableCollection<HymnItemViewModel> _hymns;
    private ObservableCollection<HymnItemViewModel> _filteredHymns;
    private string _searchText;
    private string _collectionName;
    private string _collectionDescription;
    private DateTime _createdDate;
    private DateTime _updatedDate;
    private int _hymnCount;
    private bool _isRefreshing;

    public CollectionDetailPageViewModel(Collection collection)
    {
        _collection = collection ?? throw new ArgumentNullException(nameof(collection));
        _databaseService = ServiceHelper.GetService<IDatabaseService>();
        
        _hymns = new ObservableCollection<HymnItemViewModel>();
        _filteredHymns = new ObservableCollection<HymnItemViewModel>();
        _searchText = string.Empty;
        _collectionName = collection.Name;
        _collectionDescription = collection.Description ?? string.Empty;
        _createdDate = collection.CreatedAt;
        _updatedDate = collection.UpdatedAt;
        
        // Initialize search timer
        _searchTimer = new System.Timers.Timer(300);
        _searchTimer.Elapsed += OnSearchTimerElapsed;
        _searchTimer.AutoReset = false;
        
        // Initialize commands
        RefreshCommand = new Command(async () => await LoadHymnsAsync());
        ViewHymnCommand = new Command<HymnItemViewModel>(async (hymn) => await ViewHymnAsync(hymn));
        ToggleFavoriteCommand = new Command<HymnItemViewModel>(async (hymn) => await ToggleFavoriteAsync(hymn));
        ShowHymnOptionsCommand = new Command<HymnItemViewModel>(async (hymn) => await ShowHymnOptionsAsync(hymn));
        AddHymnsCommand = new Command(async () => await AddHymnsAsync());
        ShowOptionsCommand = new Command(async () => await ShowCollectionOptionsAsync());
        
        Title = collection.Name;
    }

    public string CollectionName
    {
        get => _collectionName;
        set => SetProperty(ref _collectionName, value);
    }

    public string CollectionDescription
    {
        get => _collectionDescription;
        set => SetProperty(ref _collectionDescription, value);
    }

    public DateTime CreatedDate
    {
        get => _createdDate;
        set => SetProperty(ref _createdDate, value);
    }

    public DateTime UpdatedDate
    {
        get => _updatedDate;
        set => SetProperty(ref _updatedDate, value);
    }

    public int HymnCount
    {
        get => _hymnCount;
        set => SetProperty(ref _hymnCount, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                _searchTimer.Stop();
                _searchTimer.Start();
            }
        }
    }

    public new bool IsRefreshing
    {
        get => _isRefreshing;
        set => SetProperty(ref _isRefreshing, value);
    }

    public ObservableCollection<HymnItemViewModel> Hymns
    {
        get => _hymns;
        set => SetProperty(ref _hymns, value);
    }

    public ObservableCollection<HymnItemViewModel> FilteredHymns
    {
        get => _filteredHymns;
        set => SetProperty(ref _filteredHymns, value);
    }

    public new ICommand RefreshCommand { get; }
    public ICommand ViewHymnCommand { get; }
    public ICommand ToggleFavoriteCommand { get; }
    public ICommand ShowHymnOptionsCommand { get; }
    public ICommand AddHymnsCommand { get; }
    public ICommand ShowOptionsCommand { get; }

    public override async Task OnAppearingAsync()
    {
        await base.OnAppearingAsync();
        await LoadHymnsAsync();
    }

    public override async Task OnDisappearingAsync()
    {
        await base.OnDisappearingAsync();
        _searchTimer?.Stop();
    }

    private async Task LoadHymnsAsync()
    {
        await ExecuteAsync(async () =>
        {
            IsRefreshing = true;
            
            try
            {
                var collectionHymns = await _databaseService.GetCollectionHymnsAsync(_collection.Id);
                var hymnBooks = await _databaseService.GetHymnBooksAsync();
                
                var hymnViewModels = new List<HymnItemViewModel>();
                
                foreach (var hymn in collectionHymns)
                {
                    var hymnBook = hymnBooks.FirstOrDefault(hb => hb.Id == hymn.HymnBookId);
                    var viewModel = new HymnItemViewModel
                    {
                        Id = hymn.Id,
                        Title = hymn.Title,
                        HymnNumber = hymn.HymnNumber,
                        HymnBookName = hymnBook?.Name ?? string.Empty,
                        Language = hymn.Language ?? string.Empty,
                        IsFavorite = hymn.IsFavorite,
                        PreviewText = GetPreviewText(hymn.Content),
                        CreatedAt = hymn.CreatedAt,
                        UpdatedAt = hymn.UpdatedAt
                    };
                    hymnViewModels.Add(viewModel);
                }
                
                Hymns.Clear();
                foreach (var viewModel in hymnViewModels.OrderBy(h => h.Title))
                {
                    Hymns.Add(viewModel);
                }
                
                HymnCount = Hymns.Count;
                ApplyFilter();
            }
            catch (Exception ex)
            {
                HandleError(ex, "Failed to load collection hymns");
            }
            finally
            {
                IsRefreshing = false;
            }
        });
    }

    private void OnSearchTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() => ApplyFilter());
    }

    private void ApplyFilter()
    {
        var filtered = Hymns.AsEnumerable();
        
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.ToLowerInvariant();
            filtered = filtered.Where(h => 
                h.Title.ToLowerInvariant().Contains(searchLower) ||
                h.HymnBookName.ToLowerInvariant().Contains(searchLower) ||
                h.Language.ToLowerInvariant().Contains(searchLower) ||
                h.PreviewText.ToLowerInvariant().Contains(searchLower) ||
                (h.HymnNumber > 0 && h.HymnNumber.ToString().Contains(searchLower))
            );
        }
        
        FilteredHymns.Clear();
        foreach (var hymn in filtered)
        {
            FilteredHymns.Add(hymn);
        }
    }

    private async Task ViewHymnAsync(HymnItemViewModel hymnViewModel)
    {
        if (hymnViewModel == null) return;
        
        try
        {
            var hymn = await _databaseService.GetHymnAsync(hymnViewModel.Id);
            if (hymn != null)
            {
                // Increment view count
                hymn.ViewCount++;
                await _databaseService.UpdateHymnAsync(hymn);
                
                // Navigate to edit page
                await Shell.Current.GoToAsync($"//Edit?hymnId={hymn.Id}");
            }
        }
        catch (Exception ex)
        {
            HandleError(ex, "Failed to open hymn");
        }
    }

    private async Task ToggleFavoriteAsync(HymnItemViewModel hymnViewModel)
    {
        if (hymnViewModel == null) return;
        
        try
        {
            var hymn = await _databaseService.GetHymnAsync(hymnViewModel.Id);
            if (hymn != null)
            {
                hymn.IsFavorite = !hymn.IsFavorite;
                hymn.UpdatedAt = DateTime.Now;
                await _databaseService.UpdateHymnAsync(hymn);
                
                hymnViewModel.IsFavorite = hymn.IsFavorite;
            }
        }
        catch (Exception ex)
        {
            HandleError(ex, "Failed to update favorite status");
        }
    }

    private async Task ShowHymnOptionsAsync(HymnItemViewModel hymnViewModel)
    {
        if (hymnViewModel == null) return;
        
        try
        {
            var action = await Application.Current.MainPage.DisplayActionSheet(
                hymnViewModel.Title,
                "Cancel",
                null,
                "View Details",
                "Edit",
                "Remove from Collection",
                "Export",
                "Delete Hymn"
            );
            
            switch (action)
            {
                case "View Details":
                    await ViewHymnAsync(hymnViewModel);
                    break;
                case "Edit":
                    await Shell.Current.GoToAsync($"//Edit?hymnId={hymnViewModel.Id}");
                    break;
                case "Remove from Collection":
                    await RemoveFromCollectionAsync(hymnViewModel);
                    break;
                case "Export":
                    await ExportHymnAsync(hymnViewModel);
                    break;
                case "Delete Hymn":
                    await DeleteHymnAsync(hymnViewModel);
                    break;
            }
        }
        catch (Exception ex)
        {
            HandleError(ex, "Failed to show hymn options");
        }
    }

    private async Task AddHymnsAsync()
    {
        try
        {
            // Get all hymns not in this collection
            var allHymns = await _databaseService.GetHymnsAsync();
            var collectionHymns = await _databaseService.GetCollectionHymnsAsync(_collection.Id);
            var collectionHymnIds = collectionHymns.Select(h => h.Id).ToHashSet();
            
            var availableHymns = allHymns.Where(h => !collectionHymnIds.Contains(h.Id)).ToList();
            
            if (!availableHymns.Any())
            {
                await Application.Current.MainPage.DisplayAlert("No Hymns Available", "All hymns are already in this collection.", "OK");
                return;
            }
            
            // Show selection page (simplified - in a real app, you'd navigate to a selection page)
            var hymnTitles = availableHymns.Select(h => h.Title).ToArray();
            var selectedTitle = await Application.Current.MainPage.DisplayActionSheet(
                "Select Hymn to Add",
                "Cancel",
                null,
                hymnTitles.Take(10).ToArray() // Limit to first 10 for demo
            );
            
            if (!string.IsNullOrEmpty(selectedTitle) && selectedTitle != "Cancel")
            {
                var selectedHymn = availableHymns.FirstOrDefault(h => h.Title == selectedTitle);
                if (selectedHymn != null)
                {
                    await _databaseService.AddHymnToCollectionAsync(_collection.Id, selectedHymn.Id);
                    await LoadHymnsAsync();
                }
            }
        }
        catch (Exception ex)
        {
            HandleError(ex, "Failed to add hymns to collection");
        }
    }

    private async Task ShowCollectionOptionsAsync()
    {
        try
        {
            var action = await Application.Current.MainPage.DisplayActionSheet(
                "Collection Options",
                "Cancel",
                null,
                "Edit Collection",
                "Export Collection",
                "Share Collection",
                "Delete Collection"
            );
            
            switch (action)
            {
                case "Edit Collection":
                    await EditCollectionAsync();
                    break;
                case "Export Collection":
                    await ExportCollectionAsync();
                    break;
                case "Share Collection":
                    await ShareCollectionAsync();
                    break;
                case "Delete Collection":
                    await DeleteCollectionAsync();
                    break;
            }
        }
        catch (Exception ex)
        {
            HandleError(ex, "Failed to show collection options");
        }
    }

    private async Task RemoveFromCollectionAsync(HymnItemViewModel hymnViewModel)
    {
        try
        {
            var confirm = await Application.Current.MainPage.DisplayAlert(
                "Remove Hymn",
                $"Remove '{hymnViewModel.Title}' from this collection?",
                "Remove",
                "Cancel"
            );
            
            if (confirm)
            {
                await _databaseService.RemoveHymnFromCollectionAsync(_collection.Id, hymnViewModel.Id);
                await LoadHymnsAsync();
            }
        }
        catch (Exception ex)
        {
            HandleError(ex, "Failed to remove hymn from collection");
        }
    }

    private async Task ExportHymnAsync(HymnItemViewModel hymnViewModel)
    {
        try
        {
            var exportService = ServiceHelper.GetService<IExportService>();
            var hymn = await _databaseService.GetHymnAsync(hymnViewModel.Id);
            
            if (hymn != null)
            {
                await exportService.ExportHymnAsync(hymn, ExportFormat.Txt);
            }
        }
        catch (Exception ex)
        {
            HandleError(ex, "Failed to export hymn");
        }
    }

    private async Task DeleteHymnAsync(HymnItemViewModel hymnViewModel)
    {
        try
        {
            var confirm = await Application.Current.MainPage.DisplayAlert(
                "Delete Hymn",
                $"Permanently delete '{hymnViewModel.Title}'? This cannot be undone.",
                "Delete",
                "Cancel"
            );
            
            if (confirm)
            {
                await _databaseService.DeleteHymnAsync(hymnViewModel.Id);
                await LoadHymnsAsync();
            }
        }
        catch (Exception ex)
        {
            HandleError(ex, "Failed to delete hymn");
        }
    }

    private async Task EditCollectionAsync()
    {
        // In a real app, navigate to collection edit page
        await Application.Current.MainPage.DisplayAlert("Edit Collection", "Collection editing not implemented in this demo.", "OK");
    }

    private async Task ExportCollectionAsync()
    {
        try
        {
            var exportService = ServiceHelper.GetService<IExportService>();
            await exportService.ExportCollectionAsync(_collection, ExportFormat.Txt);
        }
        catch (Exception ex)
        {
            HandleError(ex, "Failed to export collection");
        }
    }

    private async Task ShareCollectionAsync()
    {
        // In a real app, implement sharing functionality
        await Application.Current.MainPage.DisplayAlert("Share Collection", "Collection sharing not implemented in this demo.", "OK");
    }

    private async Task DeleteCollectionAsync()
    {
        try
        {
            var confirm = await Application.Current.MainPage.DisplayAlert(
                "Delete Collection",
                $"Permanently delete '{_collection.Name}'? This will not delete the hymns themselves.",
                "Delete",
                "Cancel"
            );
            
            if (confirm)
            {
                await _databaseService.DeleteCollectionAsync(_collection.Id);
                await Shell.Current.GoToAsync(".."); // Navigate back
            }
        }
        catch (Exception ex)
        {
            HandleError(ex, "Failed to delete collection");
        }
    }

    private string GetPreviewText(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return "No content";
        
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var firstLine = lines.FirstOrDefault()?.Trim();
        
        if (string.IsNullOrWhiteSpace(firstLine))
            return "No content";
        
        return firstLine.Length > 100 ? firstLine.Substring(0, 100) + "..." : firstLine;
    }
}