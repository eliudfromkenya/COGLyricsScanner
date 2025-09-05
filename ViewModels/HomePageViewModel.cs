using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using COGLyricsScanner.Models;
using COGLyricsScanner.Services;
using System.Collections.ObjectModel;

namespace COGLyricsScanner.ViewModels;

public partial class HomePageViewModel : BaseViewModel
{
    private readonly IDatabaseService _databaseService;
    private readonly IExportService _exportService;
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private ObservableCollection<Hymn> hymns = new();

    [ObservableProperty]
    private ObservableCollection<Hymn> filteredHymns = new();

    [ObservableProperty]
    private ObservableCollection<HymnBook> hymnBooks = new();

    [ObservableProperty]
    private ObservableCollection<Collection> collections = new();

    [ObservableProperty]
    private ObservableCollection<Hymn> recentHymns = new();

    [ObservableProperty]
    private ObservableCollection<Hymn> favoriteHymns = new();

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private HymnBook? selectedHymnBook;

    [ObservableProperty]
    private Collection? selectedCollection;

    [ObservableProperty]
    private string selectedLanguage = "All";

    [ObservableProperty]
    private bool showFavoritesOnly;

    [ObservableProperty]
    private string sortBy = "Title";

    [ObservableProperty]
    private bool sortAscending = true;

    [ObservableProperty]
    private int totalHymns;

    [ObservableProperty]
    private int totalCollections;

    [ObservableProperty]
    private int totalFavorites;

    [ObservableProperty]
    private bool isSearching;

    [ObservableProperty]
    private bool hasSearchResults = true;

    [ObservableProperty]
    private string currentView = "All"; // All, Recent, Favorites, Collections

    [ObservableProperty]
    private ObservableCollection<string> availableLanguages = new();

    [ObservableProperty]
    private ObservableCollection<string> sortOptions = new()
    {
        "Title", "Number", "Created Date", "Modified Date", "View Count"
    };

    private Timer? _searchTimer;
    private readonly object _searchLock = new();

    public HomePageViewModel(IDatabaseService databaseService, IExportService exportService, ISettingsService settingsService)
    {
        _databaseService = databaseService;
        _exportService = exportService;
        _settingsService = settingsService;
        
        Title = "Hymn Library";
        
        // Initialize search timer
        _searchTimer = new Timer(SearchCallback, null, Timeout.Infinite, Timeout.Infinite);
        
        // Initialize
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await LoadDataAsync();
        await LoadStatisticsAsync();
    }

    private async Task LoadDataAsync()
    {
        await ExecuteWithBusyAsync(async () =>
        {
            try
            {
                // Load hymns
                var allHymns = await _databaseService.GetHymnsAsync();
                Hymns.Clear();
                foreach (var hymn in allHymns)
                {
                    Hymns.Add(hymn);
                }

                // Load hymn books
                var books = await _databaseService.GetAllHymnBooksAsync();
                HymnBooks.Clear();
                HymnBooks.Add(new HymnBook { Id = 0, Name = "All Books" }); // Add "All" option
                foreach (var book in books)
                {
                    HymnBooks.Add(book);
                }

                // Load collections
                var colls = await _databaseService.GetCollectionsAsync();
                Collections.Clear();
                Collections.Add(new Collection { Id = 0, Name = "All Collections" }); // Add "All" option
                foreach (var collection in colls)
                {
                    Collections.Add(collection);
                }

                // Load recent hymns
                var recent = await _databaseService.GetRecentHymnsAsync(10);
                RecentHymns.Clear();
                foreach (var hymn in recent)
                {
                    RecentHymns.Add(hymn);
                }

                // Load favorite hymns
                var favorites = await _databaseService.GetFavoriteHymnsAsync();
                FavoriteHymns.Clear();
                foreach (var hymn in favorites)
                {
                    FavoriteHymns.Add(hymn);
                }

                // Load available languages
                var languages = allHymns.Select(h => h.Language ?? "Unknown").Distinct().OrderBy(l => l).ToList();
                AvailableLanguages.Clear();
                AvailableLanguages.Add("All");
                foreach (var lang in languages)
                {
                    AvailableLanguages.Add(lang);
                }

                // Apply initial filter
                ApplyFilters();
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(ex, "Failed to load data");
            }
        }, "Loading hymns...");
    }

    private async Task LoadStatisticsAsync()
    {
        try
        {
            TotalHymns = await _databaseService.GetTotalHymnsCountAsync();
            TotalCollections = await _databaseService.GetTotalCollectionsCountAsync();
            TotalFavorites = await _databaseService.GetFavoriteHymnsCountAsync();
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex, "Failed to load statistics");
        }
    }

    [RelayCommand]
    private async Task SearchHymnsAsync()
    {
        // Reset search timer
        lock (_searchLock)
        {
            _searchTimer?.Change(TimeSpan.FromMilliseconds(500), Timeout.InfiniteTimeSpan);
        }
    }

    private async void SearchCallback(object? state)
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await PerformSearchAsync();
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Search failed: {ex.Message}");
        }
    }

    private async Task PerformSearchAsync()
    {
        IsSearching = true;
        
        try
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                ApplyFilters();
            }
            else
            {
                var searchResults = await _databaseService.SearchHymnsAsync(SearchText);
                var filteredResults = ApplyFiltersToResults(searchResults);
                
                FilteredHymns.Clear();
                foreach (var hymn in filteredResults)
                {
                    FilteredHymns.Add(hymn);
                }
            }
            
            HasSearchResults = FilteredHymns.Any();
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex, "Search failed");
        }
        finally
        {
            IsSearching = false;
        }
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
        ApplyFilters();
    }

    [RelayCommand]
    private void ApplyFilters()
    {
        var filtered = ApplyFiltersToResults(Hymns);
        
        FilteredHymns.Clear();
        foreach (var hymn in filtered)
        {
            FilteredHymns.Add(hymn);
        }
        
        HasSearchResults = FilteredHymns.Any();
    }

    private IEnumerable<Hymn> ApplyFiltersToResults(IEnumerable<Hymn> source)
    {
        var filtered = source.AsEnumerable();

        // Filter by hymn book
        if (SelectedHymnBook != null && SelectedHymnBook.Id > 0)
        {
            filtered = filtered.Where(h => h.HymnBookId == SelectedHymnBook.Id);
        }

        // Filter by collection
        if (SelectedCollection != null && SelectedCollection.Id > 0)
        {
            // This would require a join with HymnCollection table
            // For now, we'll implement this as a separate method
        }

        // Filter by language
        if (!string.IsNullOrEmpty(SelectedLanguage) && SelectedLanguage != "All")
        {
            filtered = filtered.Where(h => h.Language == SelectedLanguage);
        }

        // Filter favorites only
        if (ShowFavoritesOnly)
        {
            filtered = filtered.Where(h => h.IsFavorite);
        }

        // Apply sorting
        filtered = SortBy switch
        {
            "Title" => SortAscending ? filtered.OrderBy(h => h.Title) : filtered.OrderByDescending(h => h.Title),
            "Number" => SortAscending ? filtered.OrderBy(h => int.TryParse(h.Number, out var n) ? n : int.MaxValue) : filtered.OrderByDescending(h => int.TryParse(h.Number, out var n) ? n : int.MinValue),
            "Created Date" => SortAscending ? filtered.OrderBy(h => h.CreatedDate) : filtered.OrderByDescending(h => h.CreatedDate),
            "Modified Date" => SortAscending ? filtered.OrderBy(h => h.ModifiedDate) : filtered.OrderByDescending(h => h.ModifiedDate),
            "View Count" => SortAscending ? filtered.OrderBy(h => h.ViewCount) : filtered.OrderByDescending(h => h.ViewCount),
            _ => filtered.OrderBy(h => h.Title)
        };

        return filtered;
    }

    [RelayCommand]
    private void ToggleSortOrder()
    {
        SortAscending = !SortAscending;
        ApplyFilters();
    }

    [RelayCommand]
    private async Task OpenHymnAsync(Hymn hymn)
    {
        if (hymn == null) return;
        
        var parameters = new Dictionary<string, object>
        {
            { "HymnId", hymn.Id }
        };
        
        await NavigateToAsync("//edit", parameters);
    }

    [RelayCommand]
    private async Task CreateNewHymnAsync()
    {
        await NavigateToAsync("//edit");
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync(Hymn hymn)
    {
        if (hymn == null) return;
        
        try
        {
            hymn.IsFavorite = !hymn.IsFavorite;
            await _databaseService.UpdateHymnAsync(hymn);
            
            // Update favorite hymns collection
            if (hymn.IsFavorite && !FavoriteHymns.Contains(hymn))
            {
                FavoriteHymns.Add(hymn);
            }
            else if (!hymn.IsFavorite && FavoriteHymns.Contains(hymn))
            {
                FavoriteHymns.Remove(hymn);
            }
            
            // Update statistics
            await LoadStatisticsAsync();
            
            // Refresh filters if showing favorites only
            if (ShowFavoritesOnly)
            {
                ApplyFilters();
            }
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex, "Failed to update favorite status");
        }
    }

    [RelayCommand]
    private async Task DeleteHymnAsync(Hymn hymn)
    {
        if (hymn == null) return;
        
        var confirmed = await ShowConfirmationAsync(
            "Delete Hymn",
            $"Are you sure you want to delete '{hymn.Title}'? This action cannot be undone.",
            "Delete",
            "Cancel");
        
        if (!confirmed) return;
        
        try
        {
            await _databaseService.DeleteHymnAsync(hymn.Id);
            
            // Remove from collections
            Hymns.Remove(hymn);
            FilteredHymns.Remove(hymn);
            RecentHymns.Remove(hymn);
            FavoriteHymns.Remove(hymn);
            
            // Update statistics
            await LoadStatisticsAsync();
            
            await ShowSuccessAsync("Hymn deleted successfully!");
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex, "Failed to delete hymn");
        }
    }

    [RelayCommand]
    private async Task ExportHymnsAsync()
    {
        if (!FilteredHymns.Any())
        {
            await ShowErrorAsync("No hymns to export");
            return;
        }
        
        var format = await ShowActionSheetAsync(
            "Export Format",
            "Cancel",
            null,
            "Text (.txt)", "Word (.docx)", "PDF (.pdf)", "JSON (.json)", "CSV (.csv)");
        
        if (string.IsNullOrEmpty(format) || format == "Cancel")
            return;
        
        var exportFormat = format switch
        {
            "Text (.txt)" => ExportFormat.TXT,
            "Word (.docx)" => ExportFormat.DOCX,
            "PDF (.pdf)" => ExportFormat.PDF,
            "JSON (.json)" => ExportFormat.JSON,
            "CSV (.csv)" => ExportFormat.CSV,
            _ => ExportFormat.TXT
        };
        
        await ExecuteWithBusyAsync(async () =>
        {
            try
            {
                var fileName = $"hymns_export_{DateTime.Now:yyyyMMdd_HHmmss}.{exportFormat.ToString().ToLower()}";
                var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
                var success = await _exportService.ExportHymnsAsync(FilteredHymns.ToList(), exportFormat, filePath);
                if (success)
                {
                    await ShowSuccessAsync($"Exported {FilteredHymns.Count} hymns successfully!");
                }
                else
                {
                    await ShowErrorAsync("Export failed");
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(ex, "Export failed");
            }
        }, "Exporting hymns...");
    }

    [RelayCommand]
    private async Task ShowCollectionsAsync()
    {
        await NavigateToAsync("collections");
    }

    [RelayCommand]
    private async Task ShowStatisticsAsync()
    {
        await NavigateToAsync("statistics");
    }

    [RelayCommand]
    private async Task ShowSettingsAsync()
    {
        await NavigateToAsync("settings");
    }

    [RelayCommand]
    private void SetView(string view)
    {
        CurrentView = view;
        
        switch (view)
        {
            case "Recent":
                FilteredHymns.Clear();
                foreach (var hymn in RecentHymns)
                {
                    FilteredHymns.Add(hymn);
                }
                break;
            case "Favorites":
                FilteredHymns.Clear();
                foreach (var hymn in FavoriteHymns)
                {
                    FilteredHymns.Add(hymn);
                }
                break;
            default:
                ApplyFilters();
                break;
        }
        
        HasSearchResults = FilteredHymns.Any();
    }

    // Property change handlers
    partial void OnSearchTextChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            SearchHymnsCommand.Execute(null);
        }
        else
        {
            ApplyFilters();
        }
    }

    partial void OnSelectedHymnBookChanged(HymnBook? value) => ApplyFilters();
    partial void OnSelectedCollectionChanged(Collection? value) => ApplyFilters();
    partial void OnSelectedLanguageChanged(string value) => ApplyFilters();
    partial void OnShowFavoritesOnlyChanged(bool value) => ApplyFilters();
    partial void OnSortByChanged(string value) => ApplyFilters();

    protected override async Task OnRefreshAsync()
    {
        await LoadDataAsync();
        await LoadStatisticsAsync();
    }

    public override async Task OnAppearingAsync()
    {
        await base.OnAppearingAsync();
        await LoadStatisticsAsync(); // Refresh statistics when returning to page
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