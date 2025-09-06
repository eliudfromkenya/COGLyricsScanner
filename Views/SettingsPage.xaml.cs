using COGLyricsScanner.Services;
using COGLyricsScanner.Helpers;
using MauiAppTheme = Microsoft.Maui.ApplicationModel.AppTheme;

namespace COGLyricsScanner.Views
{

public partial class SettingsPage : ContentPage
{
    private readonly ISettingsService _settingsService;
    private readonly IThemeService _themeService;
    private readonly IExportService _exportService;
    private readonly IOcrService _ocrService;
    private readonly IDatabaseService _databaseService;

    public SettingsPage()
    {
        InitializeComponent();
        
        _settingsService = ServiceHelper.GetService<ISettingsService>();
        _themeService = ServiceHelper.GetService<IThemeService>();
        _exportService = ServiceHelper.GetService<IExportService>();
        _ocrService = ServiceHelper.GetService<IOcrService>();
        _databaseService = ServiceHelper.GetService<IDatabaseService>();
        
        LoadSettings();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadOcrLanguages();
    }

    private void LoadSettings()
    {
        try
        {
            // Theme settings
            DarkModeSwitch.IsToggled = _settingsService.GetThemeIsDarkMode();
            var themeColor = _settingsService.GetThemeColor();
            // UpdateThemeButtons(themeColor); // Commented out as theme buttons are not in use

            // OCR settings
            AutoSaveSwitch.IsToggled = _settingsService.GetOcrAutoSave();
            
            // Export settings
            var exportFormat = _settingsService.GetExportDefaultFormat();
            ExportFormatPicker.SelectedItem = exportFormat;
            IncludeMetadataSwitch.IsToggled = _settingsService.GetExportIncludeMetadata();
        }
        catch (Exception ex)
        {
            DisplayAlert("Error", $"Failed to load settings: {ex.Message}", "OK");
        }
    }

    private async Task LoadOcrLanguages()
    {
        try
        {
            var languages = await _ocrService.GetAvailableLanguagesAsync();
            LanguagePicker.ItemsSource = languages;
            
            var currentLanguage = _settingsService.GetOcrDefaultLanguage();
            if (languages.Contains(currentLanguage))
            {
                LanguagePicker.SelectedItem = currentLanguage;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load OCR languages: {ex.Message}", "OK");
        }
    }

    // Theme buttons are commented out in XAML, so this method is not needed
    /*
    private void UpdateThemeButtons(string themeColor)
    {
        // Reset button styles
        BlueThemeButton.Style = (Style)Application.Current.Resources["OutlineButtonStyle"];
        GreenThemeButton.Style = (Style)Application.Current.Resources["OutlineButtonStyle"];

        // Highlight selected theme
        if (themeColor.ToLower().Contains("blue"))
        {
            BlueThemeButton.Style = (Style)Application.Current.Resources["BaseButtonStyle"];
        }
        else if (themeColor.ToLower().Contains("green"))
        {
            GreenThemeButton.Style = (Style)Application.Current.Resources["BaseButtonStyle"];
        }
    }
    */

    private async void OnDarkModeToggled(object sender, ToggledEventArgs e)
    {
        try
        {
            _settingsService.SetThemeIsDarkMode(e.Value);
            await _themeService.ToggleDarkModeAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to toggle dark mode: {ex.Message}", "OK");
        }
    }

    private async void OnBlueThemeClicked(object sender, EventArgs e)
    {
        try
        {
            _settingsService.SetThemeColor("Blue");
            await _themeService.SetThemeAsync(COGLyricsScanner.Services.AppTheme.Light);
            // UpdateThemeButtons("Blue"); // Commented out as theme buttons are not in use
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to set blue theme: {ex.Message}", "OK");
        }
    }

    private async void OnGreenThemeClicked(object sender, EventArgs e)
    {
        try
        {
            _settingsService.SetThemeColor("Green");
            await _themeService.SetThemeAsync(COGLyricsScanner.Services.AppTheme.Light);
            // UpdateThemeButtons("Green"); // Commented out as theme buttons are not in use
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to set green theme: {ex.Message}", "OK");
        }
    }

    private void OnAutoSaveToggled(object sender, ToggledEventArgs e)
    {
        try
        {
            _settingsService.SetOcrAutoSave(e.Value);
        }
        catch (Exception ex)
        {
            DisplayAlert("Error", $"Failed to save auto-save setting: {ex.Message}", "OK");
        }
    }

    private void OnLanguageChanged(object sender, EventArgs e)
    {
        try
        {
            if (LanguagePicker.SelectedItem is string selectedLanguage)
            {
                _settingsService.SetOcrDefaultLanguage(selectedLanguage);
            }
        }
        catch (Exception ex)
        {
            DisplayAlert("Error", $"Failed to save language setting: {ex.Message}", "OK");
        }
    }

    private void OnExportFormatChanged(object sender, EventArgs e)
    {
        try
        {
            if (ExportFormatPicker.SelectedItem is string selectedFormat)
            {
                _settingsService.SetExportDefaultFormat(selectedFormat);
            }
        }
        catch (Exception ex)
        {
            DisplayAlert("Error", $"Failed to save export format setting: {ex.Message}", "OK");
        }
    }

    private void OnIncludeMetadataToggled(object sender, ToggledEventArgs e)
    {
        try
        {
            _settingsService.SetExportIncludeMetadata(e.Value);
        }
        catch (Exception ex)
        {
            DisplayAlert("Error", $"Failed to save metadata setting: {ex.Message}", "OK");
        }
    }

    private async void OnBackupClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await _exportService.CreateBackupAsync(Path.Combine(FileSystem.AppDataDirectory, "backup.db"));
            if (result)
            {
                await DisplayAlert("Success", "Database backup completed successfully.", "OK");
            }
            else
            {
                await DisplayAlert("Error", "Failed to backup database.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Backup failed: {ex.Message}", "OK");
        }
    }

    private async void OnRestoreClicked(object sender, EventArgs e)
    {
        try
        {
            var confirm = await DisplayAlert("Confirm Restore", 
                "This will replace all current data with the backup. Continue?", 
                "Yes", "No");
            
            if (confirm)
            {
                var result = await _exportService.RestoreBackupAsync(Path.Combine(FileSystem.AppDataDirectory, "backup.db"));
                if (result)
                {
                    await DisplayAlert("Success", "Database restored successfully. Please restart the app.", "OK");
                }
                else
                {
                    await DisplayAlert("Error", "Failed to restore database.", "OK");
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Restore failed: {ex.Message}", "OK");
        }
    }

    private async void OnResetSettingsClicked(object sender, EventArgs e)
    {
        try
        {
            var confirm = await DisplayAlert("Confirm Reset", 
                "This will reset all settings to default values. Continue?", 
                "Yes", "No");
            
            if (confirm)
            {
                await _settingsService.ResetAllSettingsAsync();
                LoadSettings();
                await _themeService.InitializeAsync();
                await DisplayAlert("Success", "Settings have been reset to default values.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to reset settings: {ex.Message}", "OK");
        }
    }

    private async void OnBreathingAnimationsClicked(object sender, EventArgs e)
    {
        try
        {
            await Shell.Current.GoToAsync("breathing-animations");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to navigate to breathing animations demo: {ex.Message}", "OK");
        }
    }

    private async void OnAboutClicked(object sender, EventArgs e)
    {
        try
        {
            await Shell.Current.GoToAsync("//about");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to navigate to about page: {ex.Message}", "OK");
        }
    }
}
}