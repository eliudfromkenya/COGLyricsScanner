using System.Collections.ObjectModel;
using COGLyricsScanner.Models;
using COGLyricsScanner.Services;
using COGLyricsScanner.Helpers;
using System.Windows.Input;

namespace COGLyricsScanner.ViewModels;

public class StatisticsPageViewModel : BaseViewModel
{
    private readonly IDatabaseService _databaseService;
    private readonly ISettingsService _settingsService;
    
    private int _totalHymns;
    private int _totalCollections;
    private int _totalFavorites;
    private int _totalHymnBooks;
    private int _recentlyAddedCount;
    private int _recentlyModifiedCount;
    private string _mostViewedHymn;
    private int _totalViews;
    private DateTime? _lastBackupDate;
    private string _databaseSize;
    private int _totalExports;
    
    private ObservableCollection<LanguageStatistic> _languageStats;
    private ObservableCollection<HymnBookStatistic> _hymnBookStats;

    public StatisticsPageViewModel()
    {
        _databaseService = ServiceHelper.GetService<IDatabaseService>();
        _settingsService = ServiceHelper.GetService<ISettingsService>();
        
        _languageStats = new ObservableCollection<LanguageStatistic>();
        _hymnBookStats = new ObservableCollection<HymnBookStatistic>();
        
        RefreshCommand = new Command(async () => await LoadStatisticsAsync());
        
        Title = "Statistics";
        _mostViewedHymn = "None";
        _databaseSize = "Calculating...";
    }

    public int TotalHymns
    {
        get => _totalHymns;
        set => SetProperty(ref _totalHymns, value);
    }

    public int TotalCollections
    {
        get => _totalCollections;
        set => SetProperty(ref _totalCollections, value);
    }

    public int TotalFavorites
    {
        get => _totalFavorites;
        set => SetProperty(ref _totalFavorites, value);
    }

    public int TotalHymnBooks
    {
        get => _totalHymnBooks;
        set => SetProperty(ref _totalHymnBooks, value);
    }

    public int RecentlyAddedCount
    {
        get => _recentlyAddedCount;
        set => SetProperty(ref _recentlyAddedCount, value);
    }

    public int RecentlyModifiedCount
    {
        get => _recentlyModifiedCount;
        set => SetProperty(ref _recentlyModifiedCount, value);
    }

    public string MostViewedHymn
    {
        get => _mostViewedHymn;
        set => SetProperty(ref _mostViewedHymn, value);
    }

    public int TotalViews
    {
        get => _totalViews;
        set => SetProperty(ref _totalViews, value);
    }

    public DateTime? LastBackupDate
    {
        get => _lastBackupDate;
        set => SetProperty(ref _lastBackupDate, value);
    }

    public string DatabaseSize
    {
        get => _databaseSize;
        set => SetProperty(ref _databaseSize, value);
    }

    public int TotalExports
    {
        get => _totalExports;
        set => SetProperty(ref _totalExports, value);
    }

    public ObservableCollection<LanguageStatistic> LanguageStats
    {
        get => _languageStats;
        set => SetProperty(ref _languageStats, value);
    }

    public ObservableCollection<HymnBookStatistic> HymnBookStats
    {
        get => _hymnBookStats;
        set => SetProperty(ref _hymnBookStats, value);
    }

    public new ICommand RefreshCommand { get; }

    public override async Task OnAppearingAsync()
    {
        await base.OnAppearingAsync();
        await LoadStatisticsAsync();
    }

    private async Task LoadStatisticsAsync()
    {
        await ExecuteAsync(async () =>
        {
            // Load basic counts
            await LoadBasicStatistics();
            
            // Load recent activity
            await LoadRecentActivity();
            
            // Load language distribution
            await LoadLanguageStatistics();
            
            // Load hymn book statistics
            await LoadHymnBookStatistics();
            
            // Load system statistics
            await LoadSystemStatistics();
        });
    }

    private async Task LoadBasicStatistics()
    {
        try
        {
            var hymns = await _databaseService.GetHymnsAsync();
            TotalHymns = hymns.Count;
            TotalFavorites = hymns.Count(h => h.IsFavorite);
            TotalViews = hymns.Sum(h => h.ViewCount);

            var collections = await _databaseService.GetCollectionsAsync();
            TotalCollections = collections.Count;

            var hymnBooks = await _databaseService.GetHymnBooksAsync();
            TotalHymnBooks = hymnBooks.Count;
        }
        catch (Exception ex)
        {
            HandleError(ex, "Failed to load basic statistics");
        }
    }

    private async Task LoadRecentActivity()
    {
        try
        {
            var hymns = await _databaseService.GetHymnsAsync();
            var sevenDaysAgo = DateTime.Now.AddDays(-7);

            RecentlyAddedCount = hymns.Count(h => h.CreatedDate >= sevenDaysAgo);
            RecentlyModifiedCount = hymns.Count(h => h.ModifiedDate >= sevenDaysAgo && h.ModifiedDate != h.CreatedDate);

            var mostViewed = hymns.OrderByDescending(h => h.ViewCount).FirstOrDefault();
            MostViewedHymn = mostViewed != null ? $"{mostViewed.Title} ({mostViewed.ViewCount} views)" : "None";
        }
        catch (Exception ex)
        {
            HandleError(ex, "Failed to load recent activity");
        }
    }

    private async Task LoadLanguageStatistics()
    {
        try
        {
            var hymns = await _databaseService.GetHymnsAsync();
            var languageGroups = hymns
                .GroupBy(h => string.IsNullOrWhiteSpace(h.Language) ? "Unknown" : h.Language)
                .Select(g => new LanguageStatistic
                {
                    Language = g.Key,
                    Count = g.Count(),
                    Percentage = (double)g.Count() / hymns.Count
                })
                .OrderByDescending(ls => ls.Count)
                .ToList();

            LanguageStats.Clear();
            foreach (var stat in languageGroups)
            {
                LanguageStats.Add(stat);
            }
        }
        catch (Exception ex)
        {
            HandleError(ex, "Failed to load language statistics");
        }
    }

    private async Task LoadHymnBookStatistics()
    {
        try
        {
            var hymnBooks = await _databaseService.GetHymnBooksAsync();
            var hymns = await _databaseService.GetHymnsAsync();

            var hymnBookStats = new List<HymnBookStatistic>();

            foreach (var hymnBook in hymnBooks)
            {
                var hymnCount = hymns.Count(h => h.HymnBookId == hymnBook.Id);
                hymnBookStats.Add(new HymnBookStatistic
                {
                    Name = hymnBook.Name,
                    Description = hymnBook.Description,
                    HymnCount = hymnCount
                });
            }

            // Add "No Book" category
            var noBookCount = hymns.Count(h => h.HymnBookId == null || h.HymnBookId == 0);
            if (noBookCount > 0)
            {
                hymnBookStats.Add(new HymnBookStatistic
                {
                    Name = "No Book Assigned",
                    Description = "Hymns not assigned to any book",
                    HymnCount = noBookCount
                });
            }

            HymnBookStats.Clear();
            foreach (var stat in hymnBookStats.OrderByDescending(hbs => hbs.HymnCount))
            {
                HymnBookStats.Add(stat);
            }
        }
        catch (Exception ex)
        {
            HandleError(ex, "Failed to load hymn book statistics");
        }
    }

    private async Task LoadSystemStatistics()
    {
        try
        {
            // Load backup date from settings
            LastBackupDate = _settingsService.GetBackupLastDate();

            // Calculate database size
            await CalculateDatabaseSize();

            // Load export count from settings
            TotalExports = _settingsService.GetExportCount();
        }
        catch (Exception ex)
        {
            HandleError(ex, "Failed to load system statistics");
        }
    }

    private async Task CalculateDatabaseSize()
    {
        try
        {
            await Task.Run(() =>
            {
                var dbPath = Path.Combine(FileSystem.AppDataDirectory, "hymns.db");
                if (File.Exists(dbPath))
                {
                    var fileInfo = new FileInfo(dbPath);
                    var sizeInBytes = fileInfo.Length;
                    
                    if (sizeInBytes < 1024)
                    {
                        DatabaseSize = $"{sizeInBytes} B";
                    }
                    else if (sizeInBytes < 1024 * 1024)
                    {
                        DatabaseSize = $"{sizeInBytes / 1024.0:F1} KB";
                    }
                    else
                    {
                        DatabaseSize = $"{sizeInBytes / (1024.0 * 1024.0):F1} MB";
                    }
                }
                else
                {
                    DatabaseSize = "0 B";
                }
            });
        }
        catch (Exception)
        {
            DatabaseSize = "Unknown";
        }
    }
}

public class LanguageStatistic
{
    public string Language { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class HymnBookStatistic
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int HymnCount { get; set; }
}