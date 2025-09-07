using COGLyricsScanner.Helpers;
using COGLyricsScanner.Models;
using COGLyricsScanner.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace COGLyricsScanner.ViewModels;

public class CollectionDetailPageViewModel : BaseViewModel
{
    private readonly IDatabaseService _databaseService;
    private Collection _collection = null!;
    private System.Timers.Timer _searchTimer = null!;
    
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
        
        InitializeCommonProperties();
        InitializeViewModel(collection);
    }

    public CollectionDetailPageViewModel(int collectionId)
    {
        _databaseService = ServiceHelper.GetService<IDatabaseService>();
        
        InitializeCommonProperties();
        
        // Load collection from database
        Task.Run(async () =>
        {
            var collection = await _databaseService.GetCollectionAsync(collectionId);
            if (collection != null)
            {
                _collection = collection;
                await MainThread.InvokeOnMainThreadAsync(() => InitializeViewModel(collection));
            }
        });
    }

    private void InitializeCommonProperties()
    {
        _hymns = new ObservableCollection<HymnItemViewModel>();
        _filteredHymns = new ObservableCollection<HymnItemViewModel>();
        _searchText = string.Empty;
        
        // Initialize search timer
        _searchTimer = new System.Timers.Timer(300);
        _searchTimer.Elapsed += OnSearchTimerElapsed;
        _searchTimer.AutoReset = false;
        
        // Initialize commands
        RefreshCommand = new Command(async () => await LoadHymnsAsync());
        ViewHymnCommand = new Command<HymnItemViewModel>(async (hymn) => await ViewHymnAsync(hymn));
        ToggleFavoriteCommand = new Command<HymnItemViewModel>(async (hymn) => await ToggleFavoriteAsync(hymn));
        ShowHymnOptionsCommand = new Command<HymnItemViewModel>(async (hymn) => await ShowHymnOptionsAsync(hymn));
        EditHymnCommand = new Command<HymnItemViewModel>(async (hymn) => await EditHymnAsync(hymn));
        DeleteHymnCommand = new Command<HymnItemViewModel>(async (hymn) => await DeleteHymnAsync(hymn));
        AddHymnsCommand = new Command(async () => await AddHymnsAsync());
        ShowOptionsCommand = new Command(async () => await ShowCollectionOptionsAsync());
        ExportCollectionCommand = new Command(async () => await ExportCollectionAsync());
    }

    private void InitializeViewModel(Collection collection)
    {
        _collectionName = collection.Name;
        _collectionDescription = collection.Description ?? string.Empty;
        _createdDate = collection.CreatedDate;
        _updatedDate = collection.ModifiedDate;
        
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

    public new ICommand RefreshCommand { get; private set; } = null!;
    public ICommand ViewHymnCommand { get; private set; } = null!;
    public ICommand ToggleFavoriteCommand { get; private set; } = null!;
    public ICommand ShowHymnOptionsCommand { get; private set; } = null!;
    public ICommand EditHymnCommand { get; private set; } = null!;
    public ICommand DeleteHymnCommand { get; private set; } = null!;
    public ICommand AddHymnsCommand { get; private set; } = null!;
    public ICommand ShowOptionsCommand { get; private set; } = null!;
    public ICommand ExportCollectionCommand { get; private set; } = null!;

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
                    var viewModel = new HymnItemViewModel(hymn, OnHymnSelected, OnFavoriteToggled);
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
                h.Language.ToLowerInvariant().Contains(searchLower) ||
                h.Preview.ToLowerInvariant().Contains(searchLower) ||
                h.Tags.ToLowerInvariant().Contains(searchLower) ||
                (!string.IsNullOrEmpty(h.Number) && h.Number.ToLowerInvariant().Contains(searchLower))
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
                
                // Navigate to hymn view page
                var navigationParameter = new Dictionary<string, object>
                {
                    { "hymn", hymn }
                };
                await Shell.Current.GoToAsync("hymn-view", navigationParameter);
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
                hymn.ModifiedDate = DateTime.Now;
                await _databaseService.UpdateHymnAsync(hymn);
                
                // Find and replace the hymn view model in the collection
                var index = Hymns.IndexOf(hymnViewModel);
                if (index >= 0)
                {
                    Hymns[index] = new HymnItemViewModel(hymn, OnHymnSelected, OnFavoriteToggled);
                }
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
            var favoriteText = hymnViewModel.IsFavorite ? "Remove from Favorites" : "Add to Favorites";
            
            var action = await Application.Current.MainPage.DisplayActionSheet(
                hymnViewModel.Title,
                "Cancel",
                null,
                "Edit",
                "View Details",
                favoriteText,
                "Share",
                "Export",
                "Remove from Collection",
                "Delete Hymn"
            );
            
            switch (action)
            {
                case "Edit":
                    await EditHymnAsync(hymnViewModel);
                    break;
                case "View Details":
                    await ViewHymnAsync(hymnViewModel);
                    break;
                case "Add to Favorites":
                case "Remove from Favorites":
                    await ToggleFavoriteAsync(hymnViewModel);
                    break;
                case "Share":
                    await ShareHymnAsync(hymnViewModel);
                    break;
                case "Export":
                    await ExportHymnAsync(hymnViewModel);
                    break;
                case "Remove from Collection":
                    await RemoveFromCollectionAsync(hymnViewModel);
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

    private async Task EditHymnAsync(HymnItemViewModel hymnViewModel)
    {
        if (hymnViewModel == null) return;
        
        try
        {
            await Shell.Current.GoToAsync($"//edit?hymnId={hymnViewModel.Id}&collectionId={_collection?.Id ?? 0}");
        }
        catch (Exception ex)
        {
            HandleError(ex, "Failed to open hymn editor");
        }
    }

    private async Task AddHymnsAsync()
    {
        try
        {
            // Navigate to EditPage for creating a new hymn and adding it to this collection
            await Shell.Current.GoToAsync($"edit?collectionId={_collection.Id}");
        }
        catch (Exception ex)
        {
            HandleError(ex, "Failed to open hymn editor");
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
                var fileName = $"{hymn.Title.Replace(" ", "_").Replace("/", "_")}.txt";
                var filePath = Path.Combine(FileSystem.Current.CacheDirectory, fileName);
                await exportService.ExportHymnAsync(hymn, ExportFormat.TXT, filePath);
                await Application.Current.MainPage.DisplayAlert("Export Complete", $"Hymn exported to: {filePath}", "OK");
            }
        }
        catch (Exception ex)
        {
            HandleError(ex, "Failed to export hymn");
        }
    }

    private async Task ShareHymnAsync(HymnItemViewModel hymnViewModel)
    {
        try
        {
            var hymn = await _databaseService.GetHymnAsync(hymnViewModel.Id);
            
            if (hymn != null)
            {
                var shareText = $"{hymn.Title}\n\n{hymn.Lyrics}";
                await Share.Default.RequestAsync(new ShareTextRequest
                {
                    Text = shareText,
                    Title = $"Share {hymn.Title}"
                });
            }
        }
        catch (Exception ex)
        {
            HandleError(ex, "Failed to share hymn");
        }
    }



    private void OnHymnSelected(Hymn hymn)
    {
       try {
         if (hymn != null)
        {
            Shell.Current.GoToAsync($"//edit?HymnId={hymn.Id}&collectionId={_collection?.Id ?? 0}");
        }
       }
        catch (Exception ex)
        {
            HandleError(ex, "Failed to navigate: "+ex.Message);
} 
    }

    private async void OnFavoriteToggled(Hymn hymn)
    {
        if (hymn != null)
        {
            var hymnViewModel = Hymns.FirstOrDefault(h => h.Id == hymn.Id);
            if (hymnViewModel != null)
            {
                await ToggleFavoriteAsync(hymnViewModel);
            }
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
        try
        {
            await Shell.Current.GoToAsync($"//CollectionModal?collectionId={_collection.Id}");
        }
        catch (Exception ex)
        {
            HandleError(ex, "Failed to open collection editor");
        }
    }

    private async Task ExportCollectionAsync()
    {
        try
        {
            var exportService = ServiceHelper.GetService<IExportService>();
            var databaseService = ServiceHelper.GetService<IDatabaseService>();
            
            // Get hymns in the collection
            var hymns = await databaseService.GetCollectionHymnsAsync(_collection.Id);
            
            if (!hymns.Any())
            {
                await Application.Current.MainPage.DisplayAlert("Export Collection", "Cannot export an empty collection.", "OK");
                return;
            }
            
            // Let user choose export format
            var format = await Application.Current.MainPage.DisplayActionSheet(
                "Export Format",
                "Cancel",
                null,
                "Text (.txt)", "Word (.docx)", "PDF (.pdf)");

            if (string.IsNullOrEmpty(format) || format == "Cancel")
                return;

            var exportFormat = format switch
            {
                "Text (.txt)" => ExportFormat.TXT,
                "Word (.docx)" => ExportFormat.DOCX,
                "PDF (.pdf)" => ExportFormat.PDF,
                _ => ExportFormat.TXT
            };
            
            var extension = exportFormat switch
            {
                ExportFormat.TXT => ".txt",
                ExportFormat.DOCX => ".docx",
                ExportFormat.PDF => ".pdf",
                _ => ".txt"
            };
            
            // Generate file path
            var fileName = $"{_collection.Name.Replace(" ", "_")}_Export{extension}";
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var filePath = Path.Combine(documentsPath, fileName);
            
            await exportService.ExportCollectionAsync(_collection, hymns, exportFormat, filePath);
            
            await Application.Current.MainPage.DisplayAlert("Export Complete", $"Collection exported to: {filePath}", "OK");
        }
        catch (Exception ex)
        {
            HandleError(ex, "Failed to export collection");
        }
    }

    private async Task ShareCollectionAsync()
    {
        try
        {
            var exportService = ServiceHelper.GetService<IExportService>();
            var databaseService = ServiceHelper.GetService<IDatabaseService>();
            
            // Get hymns in the collection
            var hymns = await databaseService.GetCollectionHymnsAsync(_collection.Id);
            
            if (!hymns.Any())
            {
                await Application.Current.MainPage.DisplayAlert("Share Collection", "Cannot share an empty collection.", "OK");
                return;
            }
            
            // Create a temporary file for sharing
            var fileName = $"{_collection.Name.Replace(" ", "_")}_Collection.txt";
            var tempFilePath = Path.Combine(FileSystem.Current.CacheDirectory, fileName);
            
            await exportService.ExportCollectionAsync(_collection, hymns, ExportFormat.TXT, tempFilePath);
            
            // Share the file
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = $"Share {_collection.Name} Collection",
                File = new ShareFile(tempFilePath)
            });
        }
        catch (Exception ex)
        {
            HandleError(ex, "Failed to share collection");
        }
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