using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using COGLyricsScanner.Models;
using COGLyricsScanner.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace COGLyricsScanner.ViewModels;

public partial class ScanPageViewModel : BaseViewModel
{
    private readonly IOcrService _ocrService;
    private readonly IDatabaseService _databaseService;
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private string scannedText = string.Empty;

    [ObservableProperty]
    private string selectedLanguage = "en";

    [ObservableProperty]
    private bool isOcrInProgress;

    [ObservableProperty]
    private int ocrProgress;

    [ObservableProperty]
    private string ocrStatus = string.Empty;

    [ObservableProperty]
    private float confidenceScore;

    [ObservableProperty]
    private bool hasScannedText;

    [ObservableProperty]
    private string imagePath = string.Empty;

    [ObservableProperty]
    private bool canScan = true;

    [ObservableProperty]
    private bool autoSaveEnabled;

    [ObservableProperty]
    private ObservableCollection<string> availableLanguages = new();

    [ObservableProperty]
    private ObservableCollection<string> recentScans = new();

    public ICommand OnAppearingCommand => new RelayCommand(async () => await OnAppearingAsync());

    public ScanPageViewModel(IOcrService ocrService, IDatabaseService databaseService, ISettingsService settingsService)
    {
        _ocrService = ocrService;
        _databaseService = databaseService;
        _settingsService = settingsService;
        
        Title = "Scan Lyrics";
        
        // Subscribe to OCR events
        _ocrService.ProgressChanged += OnOcrProgressChanged;
        _ocrService.OcrCompleted += OnOcrCompleted;
        
        // Initialize
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await LoadSettingsAsync();
        await LoadAvailableLanguagesAsync();
        await LoadRecentScansAsync();
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            SelectedLanguage = await _settingsService.GetDefaultOcrLanguageAsync();
            AutoSaveEnabled = await _settingsService.GetAutoSaveAfterOcrAsync();
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex, "Failed to load settings");
        }
    }

    private async Task LoadAvailableLanguagesAsync()
    {
        try
        {
            var languages = await _ocrService.GetAvailableLanguagesAsync();
            AvailableLanguages.Clear();
            foreach (var lang in languages)
            {
                AvailableLanguages.Add(lang);
            }
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex, "Failed to load available languages");
        }
    }

    private async Task LoadRecentScansAsync()
    {
        try
        {
            var recentHymns = await _databaseService.GetRecentHymnsAsync(5);
            RecentScans.Clear();
            foreach (var hymn in recentHymns)
            {
                RecentScans.Add(hymn.DisplayTitle);
            }
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex, "Failed to load recent scans");
        }
    }

    [RelayCommand]
    private async Task TakePhotoAsync()
    {
        if (!CanScan || IsBusy)
            return;

        try
        {
            // Check if camera is available
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                await ShowErrorAsync("Camera is not available on this device");
                return;
            }

            // Request camera permission
            var status = await Permissions.RequestAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                await ShowErrorAsync("Camera permission is required to scan lyrics");
                return;
            }

            // Take photo
            var photo = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions
            {
                Title = "Take a photo of the lyrics"
            });

            if (photo != null)
            {
                await ProcessImageAsync(photo);
            }
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex, "Failed to take photo");
        }
    }

    [RelayCommand]
    private async Task PickImageAsync()
    {
        if (!CanScan || IsBusy)
            return;

        try
        {
            var photo = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Select an image with lyrics"
            });

            if (photo != null)
            {
                await ProcessImageAsync(photo);
            }
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex, "Failed to pick image");
        }
    }

    private async Task ProcessImageAsync(FileResult photo)
    {
        try
        {
            CanScan = false;
            IsOcrInProgress = true;
            OcrProgress = 0;
            OcrStatus = "Processing image...";
            HasScannedText = false;
            ScannedText = string.Empty;

            // Save image to local storage
            var localFilePath = Path.Combine(FileSystem.CacheDirectory, photo.FileName ?? $"scan_{DateTime.Now:yyyyMMdd_HHmmss}.jpg");
            
            using (var stream = await photo.OpenReadAsync())
            using (var fileStream = File.Create(localFilePath))
            {
                await stream.CopyToAsync(fileStream);
            }

            ImagePath = localFilePath;

            // Perform OCR
            var recognizedText = await _ocrService.RecognizeTextAsync(localFilePath, SelectedLanguage);
            
            ScannedText = recognizedText;
            HasScannedText = !string.IsNullOrWhiteSpace(recognizedText);
            ConfidenceScore = await _ocrService.GetLastConfidenceScoreAsync();

            if (HasScannedText)
            {
                // Auto-save if enabled
                if (AutoSaveEnabled)
                {
                    await AutoSaveScannedTextAsync();
                }
            }
            else
            {
                await ShowErrorAsync("No text was recognized in the image. Please try with a clearer image.");
            }
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex, "Failed to process image");
        }
        finally
        {
            IsOcrInProgress = false;
            CanScan = true;
        }
    }

    [RelayCommand]
    private async Task SaveScannedTextAsync()
    {
        if (string.IsNullOrWhiteSpace(ScannedText))
        {
            await ShowErrorAsync("No text to save");
            return;
        }

        try
        {
            var title = await Application.Current?.MainPage?.DisplayPromptAsync(
                "Save Hymn", 
                "Enter a title for this hymn:", 
                "Save", 
                "Cancel", 
                "Untitled Hymn")!;

            if (string.IsNullOrWhiteSpace(title))
                return;

            var hymn = new Hymn
            {
                Title = title,
                Lyrics = ScannedText,
                Language = SelectedLanguage,
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now
            };

            await _databaseService.AddHymnAsync(hymn);
            await ShowSuccessAsync($"Hymn '{title}' saved successfully!");
            
            // Navigate to edit page
            await NavigateToEditPageAsync(hymn.Id);
            
            // Clear current scan
            await ClearScanAsync();
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex, "Failed to save hymn");
        }
    }

    private async Task AutoSaveScannedTextAsync()
    {
        try
        {
            var title = $"Scanned Hymn {DateTime.Now:yyyy-MM-dd HH:mm}";
            
            var hymn = new Hymn
            {
                Title = title,
                Lyrics = ScannedText,
                Language = SelectedLanguage,
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now
            };

            await _databaseService.AddHymnAsync(hymn);
            await LoadRecentScansAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Auto-save failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task EditScannedTextAsync()
    {
        if (string.IsNullOrWhiteSpace(ScannedText))
        {
            await ShowErrorAsync("No text to edit");
            return;
        }

        // Create temporary hymn for editing
        var tempHymn = new Hymn
        {
            Title = "Scanned Text",
            Lyrics = ScannedText,
            Language = SelectedLanguage,
            CreatedDate = DateTime.Now,
            ModifiedDate = DateTime.Now
        };

        // Save temporarily
        await _databaseService.AddHymnAsync(tempHymn);
        
        // Navigate to edit page
        await NavigateToEditPageAsync(tempHymn.Id);
    }

    [RelayCommand]
    private async Task ClearScanAsync()
    {
        ScannedText = string.Empty;
        HasScannedText = false;
        ImagePath = string.Empty;
        ConfidenceScore = 0;
        OcrProgress = 0;
        OcrStatus = string.Empty;
    }

    [RelayCommand]
    private async Task ChangeLanguageAsync()
    {
        try
        {
            var selectedLang = await ShowActionSheetAsync(
                "Select OCR Language",
                "Cancel",
                null,
                AvailableLanguages.ToArray());

            if (!string.IsNullOrEmpty(selectedLang) && selectedLang != "Cancel")
            {
                SelectedLanguage = selectedLang;
                await _settingsService.SetDefaultOcrLanguageAsync(selectedLang);
            }
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex, "Failed to change language");
        }
    }

    [RelayCommand]
    private async Task ToggleAutoSaveAsync()
    {
        try
        {
            AutoSaveEnabled = !AutoSaveEnabled;
            await _settingsService.SetAutoSaveAfterOcrAsync(AutoSaveEnabled);
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex, "Failed to toggle auto-save");
        }
    }

    private async Task NavigateToEditPageAsync(int hymnId)
    {
        var parameters = new Dictionary<string, object>
        {
            ["HymnId"] = hymnId
        };
        await NavigateToAsync("edit", parameters);
    }

    private void OnOcrProgressChanged(object? sender, OcrProgressEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            OcrProgress = e.ProgressPercentage;
            OcrStatus = e.Status;
        });
    }

    private void OnOcrCompleted(object? sender, OcrCompletedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsOcrInProgress = false;
            CanScan = true;
            
            if (e.IsSuccessful)
            {
                OcrStatus = $"OCR completed in {e.ProcessingTime.TotalSeconds:F1}s";
            }
            else
            {
                OcrStatus = "OCR failed";
                if (!string.IsNullOrEmpty(e.ErrorMessage))
                {
                    _ = ShowErrorAsync(e.ErrorMessage);
                }
            }
        });
    }

    protected override async Task OnRefreshAsync()
    {
        await LoadRecentScansAsync();
    }

    public override async Task OnAppearingAsync()
    {
        await base.OnAppearingAsync();
        await LoadRecentScansAsync();
    }
}