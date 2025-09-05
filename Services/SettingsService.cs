namespace COGLyricsScanner.Services;

public class SettingsService : ISettingsService
{
    // Keys for preferences
    private const string ThemeKey = "theme";
    private const string ThemeColorKey = "theme_color";
    private const string DefaultOcrLanguageKey = "default_ocr_language";
    private const string AutoSaveAfterOcrKey = "auto_save_after_ocr";
    private const string FontSizeKey = "font_size";
    private const string ShowLineNumbersKey = "show_line_numbers";
    private const string DefaultExportFormatKey = "default_export_format";
    private const string ExportDirectoryKey = "export_directory";
    private const string CaseSensitiveSearchKey = "case_sensitive_search";
    private const string SearchHistoryLimitKey = "search_history_limit";
    private const string AutoBackupKey = "auto_backup";
    private const string BackupIntervalDaysKey = "backup_interval_days";
    private const string LastBackupDateKey = "last_backup_date";
    private const string CollectAnalyticsKey = "collect_analytics";
    private const string ExportCountKey = "export_count";

    // Theme settings
    public async Task<AppTheme> GetThemeAsync()
    {
        var themeString = Preferences.Get(ThemeKey, AppTheme.System.ToString());
        return Enum.TryParse<AppTheme>(themeString, out var theme) ? theme : AppTheme.System;
    }

    public async Task SetThemeAsync(AppTheme theme)
    {
        Preferences.Set(ThemeKey, theme.ToString());
    }

    // OCR settings
    public async Task<string> GetDefaultOcrLanguageAsync()
    {
        return Preferences.Get(DefaultOcrLanguageKey, "en");
    }

    public async Task SetDefaultOcrLanguageAsync(string language)
    {
        Preferences.Set(DefaultOcrLanguageKey, language);
    }

    public async Task<bool> GetAutoSaveAfterOcrAsync()
    {
        return await Task.FromResult(Preferences.Get(AutoSaveAfterOcrKey, true));
    }

    public async Task SetAutoSaveAfterOcrAsync(bool autoSave)
    {
        Preferences.Set(AutoSaveAfterOcrKey, autoSave);
    }

    // UI settings
    public async Task<double> GetFontSizeAsync()
    {
        return await Task.FromResult(Preferences.Get(FontSizeKey, 16.0));
    }

    public async Task SetFontSizeAsync(double fontSize)
    {
        Preferences.Set(FontSizeKey, fontSize);
    }

    public async Task<bool> GetShowLineNumbersAsync()
    {
        return await Task.FromResult(Preferences.Get(ShowLineNumbersKey, false));
    }

    public async Task SetShowLineNumbersAsync(bool showLineNumbers)
    {
        Preferences.Set(ShowLineNumbersKey, showLineNumbers);
    }

    // Export settings
    public async Task<string> GetDefaultExportFormatAsync()
    {
        return await Task.FromResult(Preferences.Get(DefaultExportFormatKey, "TXT"));
    }

    public async Task SetDefaultExportFormatAsync(string format)
    {
        Preferences.Set(DefaultExportFormatKey, format);
    }

    public async Task<string> GetExportDirectoryAsync()
    {
        var defaultPath = Path.Combine(FileSystem.AppDataDirectory, "Exports");
        return await Task.FromResult(Preferences.Get(ExportDirectoryKey, defaultPath));
    }

    public async Task SetExportDirectoryAsync(string directory)
    {
        Preferences.Set(ExportDirectoryKey, directory);
    }

    public async Task<bool> GetExportIncludeMetadataAsync()
    {
        return await Task.FromResult(Preferences.Get("export_include_metadata", false));
    }

    public async Task SetExportIncludeMetadataAsync(bool includeMetadata)
    {
        Preferences.Set("export_include_metadata", includeMetadata);
        await Task.CompletedTask;
    }

    // Search settings
    public async Task<bool> GetCaseSensitiveSearchAsync()
    {
        return await Task.FromResult(Preferences.Get(CaseSensitiveSearchKey, false));
    }

    public async Task SetCaseSensitiveSearchAsync(bool caseSensitive)
    {
        Preferences.Set(CaseSensitiveSearchKey, caseSensitive);
        await Task.CompletedTask;
    }

    public async Task<int> GetSearchHistoryLimitAsync()
    {
        return await Task.FromResult(Preferences.Get(SearchHistoryLimitKey, 20));
    }

    public async Task SetSearchHistoryLimitAsync(int limit)
    {
        Preferences.Set(SearchHistoryLimitKey, limit);
        await Task.CompletedTask;
    }

    // Backup settings
    public async Task<bool> GetAutoBackupAsync()
    {
        return await Task.FromResult(Preferences.Get(AutoBackupKey, false));
    }

    public async Task SetAutoBackupAsync(bool autoBackup)
    {
        Preferences.Set(AutoBackupKey, autoBackup);
        await Task.CompletedTask;
    }

    public async Task<int> GetBackupIntervalDaysAsync()
    {
        return await Task.FromResult(Preferences.Get(BackupIntervalDaysKey, 7));
    }

    public async Task SetBackupIntervalDaysAsync(int days)
    {
        Preferences.Set(BackupIntervalDaysKey, days);
        await Task.CompletedTask;
    }

    public async Task<DateTime?> GetLastBackupDateAsync()
    {
        var dateString = await Task.FromResult(Preferences.Get(LastBackupDateKey, string.Empty));
        if (DateTime.TryParse(dateString, out var date))
            return date;
        return null;
    }

    public async Task SetLastBackupDateAsync(DateTime date)
    {
        Preferences.Set(LastBackupDateKey, date.ToString("O"));
        await Task.CompletedTask;
    }

    // Privacy settings
    public async Task<bool> GetCollectAnalyticsAsync()
    {
        return await Task.FromResult(Preferences.Get(CollectAnalyticsKey, false));
    }

    public async Task SetCollectAnalyticsAsync(bool collect)
    {
        Preferences.Set(CollectAnalyticsKey, collect);
        await Task.CompletedTask;
    }

    // Reset settings
    public async Task ResetAllSettingsAsync()
    {
        Preferences.Clear();
        await Task.CompletedTask;
    }

    public async Task<Dictionary<string, object>> ExportSettingsAsync()
    {
        var settings = new Dictionary<string, object>
        {
            [ThemeKey] = await GetThemeAsync(),
            [DefaultOcrLanguageKey] = await GetDefaultOcrLanguageAsync(),
            [AutoSaveAfterOcrKey] = await GetAutoSaveAfterOcrAsync(),
            [FontSizeKey] = await GetFontSizeAsync(),
            [ShowLineNumbersKey] = await GetShowLineNumbersAsync(),
            [DefaultExportFormatKey] = await GetDefaultExportFormatAsync(),
            [ExportDirectoryKey] = await GetExportDirectoryAsync(),
            [CaseSensitiveSearchKey] = await GetCaseSensitiveSearchAsync(),
            [SearchHistoryLimitKey] = await GetSearchHistoryLimitAsync(),
            [AutoBackupKey] = await GetAutoBackupAsync(),
            [BackupIntervalDaysKey] = await GetBackupIntervalDaysAsync(),
            [CollectAnalyticsKey] = await GetCollectAnalyticsAsync()
        };

        var lastBackup = await GetLastBackupDateAsync();
        if (lastBackup.HasValue)
            settings[LastBackupDateKey] = lastBackup.Value;

        return settings;
    }

    public async Task ImportSettingsAsync(Dictionary<string, object> settings)
    {
        foreach (var setting in settings)
        {
            try
            {
                switch (setting.Key)
                {
                    case ThemeKey:
                        if (Enum.TryParse<AppTheme>(setting.Value.ToString(), out var theme))
                            await SetThemeAsync(theme);
                        break;
                    case DefaultOcrLanguageKey:
                        await SetDefaultOcrLanguageAsync(setting.Value.ToString() ?? "en");
                        break;
                    case AutoSaveAfterOcrKey:
                        if (bool.TryParse(setting.Value.ToString(), out var autoSave))
                            await SetAutoSaveAfterOcrAsync(autoSave);
                        break;
                    case FontSizeKey:
                        if (double.TryParse(setting.Value.ToString(), out var fontSize))
                            await SetFontSizeAsync(fontSize);
                        break;
                    case ShowLineNumbersKey:
                        if (bool.TryParse(setting.Value.ToString(), out var showLineNumbers))
                            await SetShowLineNumbersAsync(showLineNumbers);
                        break;
                    case DefaultExportFormatKey:
                        await SetDefaultExportFormatAsync(setting.Value.ToString() ?? "TXT");
                        break;
                    case ExportDirectoryKey:
                        await SetExportDirectoryAsync(setting.Value.ToString() ?? FileSystem.AppDataDirectory);
                        break;
                    case CaseSensitiveSearchKey:
                        if (bool.TryParse(setting.Value.ToString(), out var caseSensitive))
                            await SetCaseSensitiveSearchAsync(caseSensitive);
                        break;
                    case SearchHistoryLimitKey:
                        if (int.TryParse(setting.Value.ToString(), out var historyLimit))
                            await SetSearchHistoryLimitAsync(historyLimit);
                        break;
                    case AutoBackupKey:
                        if (bool.TryParse(setting.Value.ToString(), out var autoBackup))
                            await SetAutoBackupAsync(autoBackup);
                        break;
                    case BackupIntervalDaysKey:
                        if (int.TryParse(setting.Value.ToString(), out var intervalDays))
                            await SetBackupIntervalDaysAsync(intervalDays);
                        break;
                    case LastBackupDateKey:
                        if (DateTime.TryParse(setting.Value.ToString(), out var lastBackupDate))
                            await SetLastBackupDateAsync(lastBackupDate);
                        break;
                    case CollectAnalyticsKey:
                        if (bool.TryParse(setting.Value.ToString(), out var collectAnalytics))
                            await SetCollectAnalyticsAsync(collectAnalytics);
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error importing setting {setting.Key}: {ex.Message}");
            }
        }
    }

    // Statistics and tracking methods
    public DateTime? GetBackupLastDate()
    {
        var binaryDate = Preferences.Get(LastBackupDateKey, 0L);
        return binaryDate == 0L ? null : DateTime.FromBinary(binaryDate);
    }

    public int GetExportCount()
    {
        return Preferences.Get(ExportCountKey, 0);
    }

    public void IncrementExportCount()
    {
        var currentCount = GetExportCount();
        Preferences.Set(ExportCountKey, currentCount + 1);
    }
    
    // Synchronous helper methods for UI
    public bool GetThemeIsDarkMode()
    {
        var themeString = Preferences.Get(ThemeKey, AppTheme.System.ToString());
        return Enum.TryParse<AppTheme>(themeString, out var theme) && theme == AppTheme.Dark;
    }

    public string GetThemeColor()
    {
        var themeString = Preferences.Get(ThemeKey, AppTheme.System.ToString());
        return Enum.TryParse<AppTheme>(themeString, out var theme) ? theme.ToString() : AppTheme.System.ToString();
    }

    public bool GetOcrAutoSave()
    {
        return Preferences.Get(AutoSaveAfterOcrKey, true);
    }

    public string GetOcrDefaultLanguage()
    {
        return Preferences.Get(DefaultOcrLanguageKey, "en");
    }

    public string GetExportDefaultFormat()
    {
        return Preferences.Get(DefaultExportFormatKey, "TXT");
    }

    public bool GetExportIncludeMetadata()
    {
        return Preferences.Get("export_include_metadata", false);
    }

    public void SetExportDefaultFormat(string format)
    {
        Preferences.Set(DefaultExportFormatKey, format);
    }

    public void SetExportIncludeMetadata(bool includeMetadata)
    {
        Preferences.Set("export_include_metadata", includeMetadata);
    }

    public void SetThemeColor(string color)
    {
        Preferences.Set(ThemeColorKey, color);
    }

    public void SetThemeIsDarkMode(bool isDarkMode)
    {
        var theme = isDarkMode ? AppTheme.Dark : AppTheme.Light;
        Preferences.Set(ThemeKey, theme.ToString());
    }

    public void SetOcrAutoSave(bool autoSave)
    {
        Preferences.Set(AutoSaveAfterOcrKey, autoSave);
    }

    public void SetOcrDefaultLanguage(string language)
    {
        Preferences.Set(DefaultOcrLanguageKey, language);
    }
}