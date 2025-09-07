using COGLyricsScanner.Services;
using COGLyricsScanner.ViewModels;
using COGLyricsScanner.Views;
using COGLyricsScanner.Helpers;
using Microsoft.Extensions.Logging;

namespace COGLyricsScanner;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        MainPage = new SplashPage();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = base.CreateWindow(activationState);
        
        window.Title = "COG Lyrics Scanner";
        
        // Set minimum window size for desktop platforms
        const int newWidth = 400;
        const int newHeight = 700;
        
        window.Width = newWidth;
        window.Height = newHeight;
        window.MinimumWidth = newWidth;
        window.MinimumHeight = newHeight;
        
        return window;
    }

    protected override async void OnStart()
    {
        base.OnStart();
        
        try
        {
            // Initialize database
            var databaseService = ServiceHelper.GetService<IDatabaseService>();
            await databaseService.InitializeAsync();

            // Initialize theme service
            var themeService = ServiceHelper.GetService<IThemeService>();
            await themeService.InitializeAsync();
            themeService.ApplyTheme();
            
            // Navigate to collections page as the default homepage
            await Shell.Current.GoToAsync("//collections");
        }
        catch (Exception ex)
        {
            // Log error or handle initialization failure
            System.Diagnostics.Debug.WriteLine($"App initialization error: {ex.Message}");
        }
    }

    protected override void OnSleep()
    {
        base.OnSleep();
        
        try
        {
            // Save any pending changes
            var settingsService = ServiceHelper.GetService<ISettingsService>();
            // Settings are automatically saved via Preferences
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"App sleep error: {ex.Message}");
        }
    }

    protected override void OnResume()
    {
        base.OnResume();
        
        try
        {
            // Refresh theme in case system theme changed
            var themeService = ServiceHelper.GetService<IThemeService>();
            themeService.ApplyTheme();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"App resume error: {ex.Message}");
        }
    }
}