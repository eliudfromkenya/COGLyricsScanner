namespace COGLyricsScanner.Services;

public class ThemeService : IThemeService
{
    private const string ThemeKey = "app_theme";
    private AppTheme _currentTheme = AppTheme.System;
    
    public AppTheme CurrentTheme => _currentTheme;
    public bool IsDarkMode => _currentTheme == AppTheme.Dark;
    
    public event EventHandler<AppTheme>? ThemeChanged;
    
    public async Task InitializeAsync()
    {
        _currentTheme = await GetSavedThemeAsync();
        ApplyTheme();
    }
    
    public void SetTheme(AppTheme theme)
    {
        if (_currentTheme == theme)
            return;
            
        _currentTheme = theme;
        ApplyTheme();
        
        // Save preference
        Task.Run(async () => await SaveThemePreferenceAsync(theme));
        
        ThemeChanged?.Invoke(this, theme);
    }
    
    public void ToggleTheme()
    {
        var newTheme = _currentTheme switch
        {
            AppTheme.Light => AppTheme.Dark,
            AppTheme.Dark => AppTheme.Light,
            AppTheme.System => AppTheme.Dark,
            _ => AppTheme.Light
        };
        
        SetTheme(newTheme);
    }
    
    public void ApplyTheme()
    {
        if (Application.Current != null)
        {
            Application.Current.UserAppTheme = _currentTheme switch
            {
                AppTheme.Light => Microsoft.Maui.ApplicationModel.AppTheme.Light,
                AppTheme.Dark => Microsoft.Maui.ApplicationModel.AppTheme.Dark,
                AppTheme.System => Microsoft.Maui.ApplicationModel.AppTheme.Unspecified,
                _ => Microsoft.Maui.ApplicationModel.AppTheme.Unspecified
            };
        }
    }
    
    public async Task SaveThemePreferenceAsync(AppTheme theme)
    {
        try
        {
            Preferences.Set(ThemeKey, theme.ToString());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving theme preference: {ex.Message}");
        }
    }
    
    public async Task<AppTheme> GetSavedThemeAsync()
    {
        try
        {
            var savedTheme = Preferences.Get(ThemeKey, AppTheme.System.ToString());
            
            if (Enum.TryParse<AppTheme>(savedTheme, out var theme))
            {
                return theme;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting saved theme: {ex.Message}");
        }
        
        // Default to system theme
        return AppTheme.System;
    }

    public async Task SetThemeAsync(AppTheme theme)
    {
        SetTheme(theme);
        await SaveThemePreferenceAsync(theme);
    }

    public async Task ToggleDarkModeAsync()
    {
        ToggleTheme();
        await SaveThemePreferenceAsync(_currentTheme);
    }
}