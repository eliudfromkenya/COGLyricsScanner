using COGLyricsScanner.Models;

namespace COGLyricsScanner.Services;

public interface IExportService
{
    /// <summary>
    /// Exports a single hymn to the specified format
    /// </summary>
    /// <param name="hymn">Hymn to export</param>
    /// <param name="format">Export format (TXT, DOCX, PDF)</param>
    /// <param name="filePath">Output file path</param>
    /// <returns>Success status</returns>
    Task<bool> ExportHymnAsync(Hymn hymn, ExportFormat format, string filePath);
    
    /// <summary>
    /// Exports multiple hymns to the specified format
    /// </summary>
    /// <param name="hymns">Hymns to export</param>
    /// <param name="format">Export format (TXT, DOCX, PDF)</param>
    /// <param name="filePath">Output file path</param>
    /// <param name="includeMetadata">Whether to include metadata</param>
    /// <returns>Success status</returns>
    Task<bool> ExportHymnsAsync(IEnumerable<Hymn> hymns, ExportFormat format, string filePath, bool includeMetadata = true);
    
    /// <summary>
    /// Exports a collection to the specified format
    /// </summary>
    /// <param name="collection">Collection to export</param>
    /// <param name="hymns">Hymns in the collection</param>
    /// <param name="format">Export format (TXT, DOCX, PDF)</param>
    /// <param name="filePath">Output file path</param>
    /// <returns>Success status</returns>
    Task<bool> ExportCollectionAsync(Collection collection, IEnumerable<Hymn> hymns, ExportFormat format, string filePath);
    
    /// <summary>
    /// Shares a hymn using the platform's sharing mechanism
    /// </summary>
    /// <param name="hymn">Hymn to share</param>
    /// <param name="format">Share format</param>
    /// <returns>Success status</returns>
    Task<bool> ShareHymnAsync(Hymn hymn, ExportFormat format);
    
    /// <summary>
    /// Gets available export formats
    /// </summary>
    /// <returns>List of supported export formats</returns>
    Task<List<ExportFormat>> GetAvailableFormatsAsync();
    
    /// <summary>
    /// Gets the default export directory
    /// </summary>
    /// <returns>Default export directory path</returns>
    Task<string> GetDefaultExportDirectoryAsync();
    
    /// <summary>
    /// Creates a backup of the entire database
    /// </summary>
    /// <param name="backupPath">Backup file path</param>
    /// <returns>Success status</returns>
    Task<bool> CreateBackupAsync(string backupPath);
    
    /// <summary>
    /// Restores database from backup
    /// </summary>
    /// <param name="backupPath">Backup file path</param>
    /// <returns>Success status</returns>
    Task<bool> RestoreBackupAsync(string backupPath);
    
    /// <summary>
    /// Event fired when export progress changes
    /// </summary>
    event EventHandler<ExportProgressEventArgs> ProgressChanged;
    
    /// <summary>
    /// Event fired when export completes
    /// </summary>
    event EventHandler<ExportCompletedEventArgs> ExportCompleted;
}

public enum ExportFormat
{
    TXT,
    DOCX,
    PDF,
    JSON,
    CSV
}

public class ExportProgressEventArgs : EventArgs
{
    public int ProgressPercentage { get; set; }
    public string Status { get; set; } = string.Empty;
    public int CurrentItem { get; set; }
    public int TotalItems { get; set; }
}

public class ExportCompletedEventArgs : EventArgs
{
    public bool IsSuccessful { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public ExportFormat Format { get; set; }
    public int ItemsExported { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public string? ErrorMessage { get; set; }
}