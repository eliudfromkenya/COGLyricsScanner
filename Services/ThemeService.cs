namespace COGLyricsScanner.Services;

public class ThemeService : IThemeService
{
    private const string ThemeKey = "app_theme";
    private AppTheme _currentTheme = AppTheme.Unspecified;
    
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
            AppTheme.Unspecified => AppTheme.Dark,
            _ => AppTheme.Light
        };
        
        SetTheme(newTheme);
    }
    
    public void ApplyTheme()
    {
        if (Application.Current != null)
        {
            Application.Current.UserAppTheme = _currentTheme;
        }
    }
    
    public async Task SaveThemePreferenceAsync(AppTheme theme)
    {
        try
        {
            await Preferences.SetAsync(ThemeKey, theme.ToString());
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
            var savedTheme = await Preferences.GetAsync(ThemeKey, AppTheme.Unspecified.ToString());
            
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
        return AppTheme.Unspecified;
    }
}