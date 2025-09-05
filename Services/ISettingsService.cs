namespace COGLyricsScanner.Services;

public interface ISettingsService
{
    // Theme settings
    Task<AppTheme> GetThemeAsync();
    Task SetThemeAsync(AppTheme theme);
    
    // OCR settings
    Task<string> GetDefaultOcrLanguageAsync();
    Task SetDefaultOcrLanguageAsync(string language);
    Task<bool> GetAutoSaveAfterOcrAsync();
    Task SetAutoSaveAfterOcrAsync(bool autoSave);
    
    // UI settings
    Task<double> GetFontSizeAsync();
    Task SetFontSizeAsync(double fontSize);
    Task<bool> GetShowLineNumbersAsync();
    Task SetShowLineNumbersAsync(bool showLineNumbers);
    
    // Export settings
    Task<string> GetDefaultExportFormatAsync();
    Task SetDefaultExportFormatAsync(string format);
    Task<string> GetExportDirectoryAsync();
    Task SetExportDirectoryAsync(string directory);
    Task<bool> GetExportIncludeMetadataAsync();
    Task SetExportIncludeMetadataAsync(bool includeMetadata);
    
    // Search settings
    Task<bool> GetCaseSensitiveSearchAsync();
    Task SetCaseSensitiveSearchAsync(bool caseSensitive);
    Task<int> GetSearchHistoryLimitAsync();
    Task SetSearchHistoryLimitAsync(int limit);
    
    // Backup settings
    Task<bool> GetAutoBackupAsync();
    Task SetAutoBackupAsync(bool autoBackup);
    Task<int> GetBackupIntervalDaysAsync();
    Task SetBackupIntervalDaysAsync(int days);
    Task<DateTime?> GetLastBackupDateAsync();
    Task SetLastBackupDateAsync(DateTime date);
    
    // Privacy settings
    Task<bool> GetCollectAnalyticsAsync();
    Task SetCollectAnalyticsAsync(bool collect);
    
    // Statistics methods
    DateTime? GetBackupLastDate();
    int GetExportCount();
    void IncrementExportCount();
    
    // Synchronous helper methods for UI
    bool GetThemeIsDarkMode();
    string GetThemeColor();
    bool GetOcrAutoSave();
    string GetOcrDefaultLanguage();
    string GetExportDefaultFormat();
    bool GetExportIncludeMetadata();
    void SetExportDefaultFormat(string format);
    void SetExportIncludeMetadata(bool includeMetadata);
    void SetThemeColor(string color);
    void SetThemeIsDarkMode(bool isDarkMode);
    void SetOcrAutoSave(bool autoSave);
    void SetOcrDefaultLanguage(string language);

    // Reset settings
    Task ResetAllSettingsAsync();
    Task<Dictionary<string, object>> ExportSettingsAsync();
    Task ImportSettingsAsync(Dictionary<string, object> settings);
}