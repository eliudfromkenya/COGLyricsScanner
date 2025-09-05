namespace COGLyricsScanner.Services;

public interface IThemeService
{
    AppTheme CurrentTheme { get; }
    bool IsDarkMode { get; }
    
    void SetTheme(AppTheme theme);
    void ToggleTheme();
    void ApplyTheme();
    
    event EventHandler<AppTheme>? ThemeChanged;
    
    Task InitializeAsync();
    Task SaveThemePreferenceAsync(AppTheme theme);
    Task<AppTheme> GetSavedThemeAsync();
}