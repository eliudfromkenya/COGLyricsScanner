using COGLyricsScanner.Models;
using COGLyricsScanner.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.VisualBasic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Collection = COGLyricsScanner.Models.Collection;

namespace COGLyricsScanner.ViewModels;

[QueryProperty(nameof(HymnId), "hymnId")]
[QueryProperty(nameof(CollectionId), "collectionId")]
public partial class EditPageViewModel : BaseViewModel
{
    private readonly IDatabaseService _databaseService;
    private readonly IExportService _exportService;
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private int hymnId;

    [ObservableProperty]
    private int collectionId;

    [ObservableProperty]
    private Hymn? currentHymn;

    [ObservableProperty]
    private string hymnTitle = string.Empty;

    [ObservableProperty]
    private string? hymnNumber;

    [ObservableProperty]
    private string hymnLyrics = string.Empty;

    [ObservableProperty]
    private string hymnLanguage = "en";

    [ObservableProperty]
    private string hymnTags = string.Empty;

    [ObservableProperty]
    private string hymnNotes = string.Empty;

    [ObservableProperty]
    private bool isFavorite;

    [ObservableProperty]
    private bool hasUnsavedChanges;

    [ObservableProperty]
    private bool isNewHymn;

    [ObservableProperty]
    private DateTime createdDate;

    [ObservableProperty]
    private DateTime modifiedDate;

    [ObservableProperty]
    private int wordCount;

    [ObservableProperty]
    private int lineCount;

    [ObservableProperty]
    private int viewCount;

    [ObservableProperty]
    private ObservableCollection<HymnBook> availableHymnBooks = new();

    [ObservableProperty]
    private HymnBook? selectedHymnBook;

    [ObservableProperty]
    private ObservableCollection<Collection> availableCollections = new();

    [ObservableProperty]
    private ObservableCollection<Collection> hymnCollections = new();

    [ObservableProperty]
    private bool showLineNumbers;

    [ObservableProperty]
    private double fontSize = 16.0;

    [ObservableProperty]
    private bool isAutoSaveEnabled = true;

    private Timer? _autoSaveTimer;
    private readonly object _autoSaveLock = new();
    
    public EditPageViewModel(IDatabaseService databaseService, IExportService exportService, ISettingsService settingsService)
    {
        _databaseService = databaseService;
        _exportService = exportService;
        _settingsService = settingsService;
        
        Title = "Edit Hymn";
        
        // Initialize auto-save timer
        _autoSaveTimer = new Timer(AutoSaveCallback, null, Timeout.Infinite, Timeout.Infinite);
        
        // Initialize
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await LoadSettingsAsync();
        await LoadHymnBooksAsync();
        await LoadCollectionsAsync();
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            ShowLineNumbers = await _settingsService.GetShowLineNumbersAsync();
            FontSize = await _settingsService.GetFontSizeAsync();
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex, "Failed to load settings");
        }
    }

    private async Task LoadHymnBooksAsync()
    {
        try
        {
            var hymnBooks = await _databaseService.GetAllHymnBooksAsync();
            AvailableHymnBooks.Clear();
            foreach (var book in hymnBooks)
            {
                AvailableHymnBooks.Add(book);
            }
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex, "Failed to load hymn books");
        }
    }

    private async Task LoadCollectionsAsync()
    {
        try
        {
            var collections = await _databaseService.GetCollectionsAsync();
            AvailableCollections.Clear();
            foreach (var collection in collections)
            {
                AvailableCollections.Add(collection);
            }
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex, "Failed to load collections");
        }
    }

    partial void OnHymnIdChanged(int value)
    {
        if (value > 0)
        {
            _ = LoadHymnAsync(value);
        }
        else
        {
            CreateNewHymn();
        }
    }

    private async Task LoadHymnAsync(int id)
    {
        try
        {
            var hymn = await _databaseService.GetHymnAsync(id);
            if (hymn != null)
            {
                CurrentHymn = hymn;
                HymnTitle = hymn.Title;
                HymnNumber = hymn.Number;
                HymnLyrics = hymn.Lyrics;
                HymnLanguage = hymn.Language ?? "en";
                HymnTags = hymn.Tags ?? string.Empty;
                HymnNotes = hymn.Notes ?? string.Empty;
                IsFavorite = hymn.IsFavorite;
                CreatedDate = hymn.CreatedDate;
                ModifiedDate = hymn.ModifiedDate;
                ViewCount = hymn.ViewCount;
                IsNewHymn = false;
                
                // Load hymn book
                if (hymn.HymnBookId > 0)
                {
                    SelectedHymnBook = AvailableHymnBooks.FirstOrDefault(b => b.Id == hymn.HymnBookId);
                }
                
                // Load collections
                await LoadHymnCollectionsAsync(id);
                
                // Update statistics
                UpdateTextStatistics();
                
                // Increment view count
                await IncrementViewCountAsync();
                
                HasUnsavedChanges = false;
            }
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex, "Failed to load hymn");
        }
    }

    private void CreateNewHymn()
    {
        CurrentHymn = null;
        HymnTitle = string.Empty;
        HymnNumber = null;
        HymnLyrics = string.Empty;
        HymnLanguage = "en";
        HymnTags = string.Empty;
        HymnNotes = string.Empty;
        IsFavorite = false;
        CreatedDate = DateTime.Now;
        ModifiedDate = DateTime.Now;
        ViewCount = 0;
        IsNewHymn = true;
        SelectedHymnBook = null;
        HymnCollections.Clear();
        HasUnsavedChanges = false;
        
        Title = "New Hymn";
    }

    private async Task LoadHymnCollectionsAsync(int hymnId)
    {
        try
        {
            var collections = await _databaseService.GetCollectionsByHymnIdAsync(hymnId);
            HymnCollections.Clear();
            foreach (var collection in collections)
            {
                HymnCollections.Add(collection);
            }
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex, "Failed to load hymn collections");
        }
    }

    [RelayCommand]
    private async Task SaveHymnAsync()
    {
        if (!ValidateHymn())
            return;

        await ExecuteWithBusyAsync(async () =>
        {
            try
            {
                if (CurrentHymn == null)
                {
                    // Create new hymn
                    var newHymn = new Hymn
                    {
                        Title = HymnTitle,
                        Number = HymnNumber,
                        Lyrics = HymnLyrics,
                        Language = HymnLanguage,
                        Tags = HymnTags,
                        Notes = HymnNotes,
                        IsFavorite = IsFavorite,
                        HymnBookId = SelectedHymnBook?.Id ?? 0,
                        CreatedDate = DateTime.Now,
                        ModifiedDate = DateTime.Now
                    };
                    
                    await _databaseService.AddHymnAsync(newHymn);
                    CurrentHymn = newHymn;
                    HymnId = newHymn.Id;
                    IsNewHymn = false;
                    
                    // Add to collection if CollectionId is specified
                    if (CollectionId > 0)
                    {
                        await _databaseService.AddHymnToCollectionAsync(newHymn.Id, CollectionId);
                    }
                }
                else
                {
                    // Update existing hymn
                    CurrentHymn.Title = HymnTitle;
                    CurrentHymn.Number = HymnNumber;
                    CurrentHymn.Lyrics = HymnLyrics;
                    CurrentHymn.Language = HymnLanguage;
                    CurrentHymn.Tags = HymnTags;
                    CurrentHymn.Notes = HymnNotes;
                    CurrentHymn.IsFavorite = IsFavorite;
                    CurrentHymn.HymnBookId = SelectedHymnBook?.Id ?? 0;
                    CurrentHymn.ModifiedDate = DateTime.Now;
                    
                    await _databaseService.UpdateHymnAsync(CurrentHymn);
                }
                
                HasUnsavedChanges = false;
                await ShowSuccessAsync("Hymn saved successfully!");
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(ex, "Failed to save hymn");
            }
        }, "Saving hymn...");
    }

    [RelayCommand]
    private async Task DeleteHymnAsync()
    {
        if (CurrentHymn == null)
            return;

        var confirmed = await ShowConfirmationAsync(
            "Delete Hymn", 
            $"Are you sure you want to delete '{HymnTitle}'? This action cannot be undone.",
            "Delete", 
            "Cancel");

        if (!confirmed)
            return;

        await ExecuteWithBusyAsync(async () =>
        {
            try
            {
                await _databaseService.DeleteHymnAsync(CurrentHymn.Id);
                await ShowSuccessAsync("Hymn deleted successfully!");
                await GoBackAsync();
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(ex, "Failed to delete hymn");
            }
        }, "Deleting hymn...");
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync()
    {
        IsFavorite = !IsFavorite;
        MarkAsChanged();
        
        if (CurrentHymn != null)
        {
            // Auto-save favorite status
            CurrentHymn.IsFavorite = IsFavorite;
            await _databaseService.UpdateHymnAsync(CurrentHymn);
        }
    }

    [RelayCommand]
    private async Task ExportHymnAsync()
    {
        if (CurrentHymn == null)
        {
            await ShowErrorAsync("Please save the hymn before exporting");
            return;
        }

        var format = await ShowActionSheetAsync(
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

        await ExecuteWithBusyAsync(async () =>
        {
            try
            {
                var success = await _exportService.ShareHymnAsync(CurrentHymn, exportFormat);
                if (success)
                {
                    await ShowSuccessAsync("Hymn exported successfully!");
                }
                else
                {
                    await ShowErrorAsync("Failed to export hymn");
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(ex, "Export failed");
            }
        }, "Exporting hymn...");
    }

    [RelayCommand]
    private async Task AddToCollectionAsync()
    {
        if (CurrentHymn == null)
        {
            await ShowErrorAsync("Please save the hymn before adding to collection");
            return;
        }

        var availableToAdd = AvailableCollections.Except(HymnCollections).ToList();
        if (!availableToAdd.Any())
        {
            await ShowErrorAsync("Hymn is already in all available collections");
            return;
        }

        var collectionNames = availableToAdd.Select(c => c.Name).ToArray();
        var selectedName = await ShowActionSheetAsync(
            "Add to Collection",
            "Cancel",
            null,
            collectionNames);

        if (string.IsNullOrEmpty(selectedName) || selectedName == "Cancel")
            return;

        var selectedCollection = availableToAdd.FirstOrDefault(c => c.Name == selectedName);
        if (selectedCollection != null)
        {
            try
            {
                await _databaseService.AddHymnToCollectionAsync(CurrentHymn.Id, selectedCollection.Id);
                HymnCollections.Add(selectedCollection);
                await ShowSuccessAsync($"Added to '{selectedCollection.Name}'");
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(ex, "Failed to add to collection");
            }
        }
    }

    [RelayCommand]
    private async Task RemoveFromCollectionAsync(Collection collection)
    {
        if (CurrentHymn == null || collection == null)
            return;

        var confirmed = await ShowConfirmationAsync(
            "Remove from Collection",
            $"Remove this hymn from '{collection.Name}'?",
            "Remove",
            "Cancel");

        if (!confirmed)
            return;

        try
        {
            await _databaseService.RemoveHymnFromCollectionAsync(CurrentHymn.Id, collection.Id);
            HymnCollections.Remove(collection);
            await ShowSuccessAsync($"Removed from '{collection.Name}'");
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex, "Failed to remove from collection");
        }
    }

    [RelayCommand]
    private async Task IncreaseFontSizeAsync()
    {
        if (FontSize < 24)
        {
            FontSize += 2;
            await _settingsService.SetFontSizeAsync(FontSize);
        }
    }

    [RelayCommand]
    private async Task DecreaseFontSizeAsync()
    {
        if (FontSize > 10)
        {
            FontSize -= 2;
            await _settingsService.SetFontSizeAsync(FontSize);
        }
    }

    [RelayCommand]
    private async Task ToggleLineNumbersAsync()
    {
        ShowLineNumbers = !ShowLineNumbers;
        await _settingsService.SetShowLineNumbersAsync(ShowLineNumbers);
    }

    [RelayCommand]
    private async Task OpenScanPageAsync()
    {
        try
        {
            var parameters = new Dictionary<string, object>();
            
            if (HymnId > 0)
            {
                parameters["hymnId"] = HymnId;
                parameters["existingLyrics"] = HymnLyrics ?? string.Empty;
            }
            
            if (parameters.Count > 0)
            {
                await Shell.Current.GoToAsync("//scan", parameters);
            }
            else
            {
                await Shell.Current.GoToAsync("//scan");
            }
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex, $"Failed to go scanning page: {ex.Message}");
        }
    }   

    [RelayCommand]
    private async Task BackAsync()
    {       
        try
        {
            //await GoBackAsync();
            await Shell.Current.GoToAsync($"//collection-detail?collectionId={collectionId}");
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex, $"Failed to go back: {ex.Message}");
        }
    }   

    private bool ValidateHymn()
    {
        if (string.IsNullOrWhiteSpace(HymnTitle))
        {
            ErrorMessage = "Title is required";
            HasError = true;
            return false;
        }

        if (string.IsNullOrWhiteSpace(HymnLyrics))
        {
            ErrorMessage = "Lyrics are required";
            HasError = true;
            return false;
        }

        return true;
    }

    private void MarkAsChanged()
    {
        HasUnsavedChanges = true;
        ModifiedDate = DateTime.Now;
        
        // Reset auto-save timer
        if (IsAutoSaveEnabled)
        {
            lock (_autoSaveLock)
            {
                _autoSaveTimer?.Change(TimeSpan.FromSeconds(5), Timeout.InfiniteTimeSpan);
            }
        }
    }

    private async void AutoSaveCallback(object? state)
    {
        if (!HasUnsavedChanges || !IsAutoSaveEnabled)
            return;

        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (ValidateHymn())
                {
                    await SaveHymnAsync();
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Auto-save failed: {ex.Message}");
        }
    }

    private async Task IncrementViewCountAsync()
    {
        if (CurrentHymn != null)
        {
            try
            {
                ViewCount++;
                CurrentHymn.ViewCount = ViewCount;
                CurrentHymn.LastViewedDate = DateTime.Now;
                await _databaseService.UpdateHymnAsync(CurrentHymn);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to update view count: {ex.Message}");
            }
        }
    }

    private void UpdateTextStatistics()
    {
        if (string.IsNullOrWhiteSpace(HymnLyrics))
        {
            WordCount = 0;
            LineCount = 0;
            return;
        }

        LineCount = HymnLyrics.Split('\n').Length;
        WordCount = HymnLyrics.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    // Property change handlers
    partial void OnHymnTitleChanged(string value) => MarkAsChanged();
    partial void OnHymnNumberChanged(string? value) => MarkAsChanged();
    partial void OnHymnLyricsChanged(string value)
    {
        MarkAsChanged();
        UpdateTextStatistics();
    }
    partial void OnHymnLanguageChanged(string value) => MarkAsChanged();
    partial void OnHymnTagsChanged(string value) => MarkAsChanged();
    partial void OnHymnNotesChanged(string value) => MarkAsChanged();
    partial void OnSelectedHymnBookChanged(HymnBook? value) => MarkAsChanged();

    protected override async Task OnRefreshAsync()
    {
        if (HymnId > 0)
        {
            await LoadHymnAsync(HymnId);
        }
        await LoadHymnBooksAsync();
        await LoadCollectionsAsync();
    }

    public override async Task OnAppearingAsync()
    {
        await base.OnAppearingAsync();
        if (HymnId > 0 && CurrentHymn == null)
        {
            await LoadHymnAsync(HymnId);
        }
    }

    public override async Task OnDisappearingAsync()
    {
        await base.OnDisappearingAsync();
        
        // Stop auto-save timer
        lock (_autoSaveLock)
        {
            _autoSaveTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        }
        
        // Prompt to save if there are unsaved changes
        if (HasUnsavedChanges)
        {
            var shouldSave = await ShowConfirmationAsync(
                "Unsaved Changes",
                "You have unsaved changes. Would you like to save them?",
                "Save",
                "Discard");
            
            if (shouldSave && ValidateHymn())
            {
                await SaveHymnAsync();
            }
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _autoSaveTimer?.Dispose();
        }
        base.Dispose(disposing);
    }
}