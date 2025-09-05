namespace COGLyricsScanner.Services;

public class SettingsService : ISettingsService
{
    // Keys for preferences
    private const string ThemeKey = "theme";
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
        var themeString = await Preferences.GetAsync(ThemeKey, AppTheme.Unspecified.ToString());
        return Enum.TryParse<AppTheme>(themeString, out var theme) ? theme : AppTheme.Unspecified;
    }

    public async Task SetThemeAsync(AppTheme theme)
    {
        await Preferences.SetAsync(ThemeKey, theme.ToString());
    }

    // OCR settings
    public async Task<string> GetDefaultOcrLanguageAsync()
    {
        return await Preferences.GetAsync(DefaultOcrLanguageKey, "en");
    }

    public async Task SetDefaultOcrLanguageAsync(string language)
    {
        await Preferences.SetAsync(DefaultOcrLanguageKey, language);
    }

    public async Task<bool> GetAutoSaveAfterOcrAsync()
    {
        return await Preferences.GetAsync(AutoSaveAfterOcrKey, true);
    }

    public async Task SetAutoSaveAfterOcrAsync(bool autoSave)
    {
        await Preferences.SetAsync(AutoSaveAfterOcrKey, autoSave);
    }

    // UI settings
    public async Task<double> GetFontSizeAsync()
    {
        return await Preferences.GetAsync(FontSizeKey, 16.0);
    }

    public async Task SetFontSizeAsync(double fontSize)
    {
        await Preferences.SetAsync(FontSizeKey, fontSize);
    }

    public async Task<bool> GetShowLineNumbersAsync()
    {
        return await Preferences.GetAsync(ShowLineNumbersKey, false);
    }

    public async Task SetShowLineNumbersAsync(bool showLineNumbers)
    {
        await Preferences.SetAsync(ShowLineNumbersKey, showLineNumbers);
    }

    // Export settings
    public async Task<string> GetDefaultExportFormatAsync()
    {
        return await Preferences.GetAsync(DefaultExportFormatKey, "TXT");
    }

    public async Task SetDefaultExportFormatAsync(string format)
    {
        await Preferences.SetAsync(DefaultExportFormatKey, format);
    }

    public async Task<string> GetExportDirectoryAsync()
    {
        var defaultPath = Path.Combine(FileSystem.AppDataDirectory, "Exports");
        return await Preferences.GetAsync(ExportDirectoryKey, defaultPath);
    }

    public async Task SetExportDirectoryAsync(string directory)
    {
        await Preferences.SetAsync(ExportDirectoryKey, directory);
    }

    // Search settings
    public async Task<bool> GetCaseSensitiveSearchAsync()
    {
        return await Preferences.GetAsync(CaseSensitiveSearchKey, false);
    }

    public async Task SetCaseSensitiveSearchAsync(bool caseSensitive)
    {
        await Preferences.SetAsync(CaseSensitiveSearchKey, caseSensitive);
    }

    public async Task<int> GetSearchHistoryLimitAsync()
    {
        return await Preferences.GetAsync(SearchHistoryLimitKey, 20);
    }

    public async Task SetSearchHistoryLimitAsync(int limit)
    {
        await Preferences.SetAsync(SearchHistoryLimitKey, limit);
    }

    // Backup settings
    public async Task<bool> GetAutoBackupAsync()
    {
        return await Preferences.GetAsync(AutoBackupKey, false);
    }

    public async Task SetAutoBackupAsync(bool autoBackup)
    {
        await Preferences.SetAsync(AutoBackupKey, autoBackup);
    }

    public async Task<int> GetBackupIntervalDaysAsync()
    {
        return await Preferences.GetAsync(BackupIntervalDaysKey, 7);
    }

    public async Task SetBackupIntervalDaysAsync(int days)
    {
        await Preferences.SetAsync(BackupIntervalDaysKey, days);
    }

    public async Task<DateTime?> GetLastBackupDateAsync()
    {
        var dateString = await Preferences.GetAsync(LastBackupDateKey, string.Empty);
        if (DateTime.TryParse(dateString, out var date))
            return date;
        return null;
    }

    public async Task SetLastBackupDateAsync(DateTime date)
    {
        await Preferences.SetAsync(LastBackupDateKey, date.ToString("O"));
    }

    // Privacy settings
    public async Task<bool> GetCollectAnalyticsAsync()
    {
        return await Preferences.GetAsync(CollectAnalyticsKey, false);
    }

    public async Task SetCollectAnalyticsAsync(bool collect)
    {
        await Preferences.SetAsync(CollectAnalyticsKey, collect);
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
        var binaryDate = Preferences.Get(BackupLastDateKey, 0L);
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
}