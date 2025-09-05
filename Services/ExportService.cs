using COGLyricsScanner.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using OpenXmlParagraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using FontSize = DocumentFormat.OpenXml.Wordprocessing.FontSize;
using System.Linq;
using Document = DocumentFormat.OpenXml.Wordprocessing.Document;
using Text = DocumentFormat.OpenXml.Wordprocessing.Text;
using Paragraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;

namespace COGLyricsScanner.Services;

public class ExportService : IExportService
{
    private readonly IDatabaseService _databaseService;
    private readonly ISettingsService _settingsService;

    public event EventHandler<ExportProgressEventArgs>? ProgressChanged;
    public event EventHandler<ExportCompletedEventArgs>? ExportCompleted;

    public ExportService(IDatabaseService databaseService, ISettingsService settingsService)
    {
        _databaseService = databaseService;
        _settingsService = settingsService;
    }

    public async Task<bool> ExportHymnAsync(Hymn hymn, ExportFormat format, string filePath)
    {
        return await ExportHymnsAsync(new[] { hymn }, format, filePath, true);
    }

    public async Task<bool> ExportHymnsAsync(IEnumerable<Hymn> hymns, ExportFormat format, string filePath, bool includeMetadata = true)
    {
        try
        {
            var hymnList = hymns.ToList();
            if (!hymnList.Any())
            {
                OnExportCompleted(false, filePath, format, 0, TimeSpan.Zero, "No hymns to export");
                return false;
            }

            OnProgressChanged(0, "Starting export...", 0, hymnList.Count);
            var stopwatch = Stopwatch.StartNew();

            // Ensure directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            bool success = format switch
            {
                ExportFormat.TXT => await ExportToTextAsync(hymnList, filePath, includeMetadata),
                ExportFormat.DOCX => await ExportToDocxAsync(hymnList, filePath, includeMetadata),
                ExportFormat.PDF => await ExportToPdfAsync(hymnList, filePath, includeMetadata),
                ExportFormat.JSON => await ExportToJsonAsync(hymnList, filePath, includeMetadata),
                ExportFormat.CSV => await ExportToCsvAsync(hymnList, filePath, includeMetadata),
                _ => throw new NotSupportedException($"Export format {format} is not supported")
            };

            stopwatch.Stop();
            OnExportCompleted(success, filePath, format, hymnList.Count, stopwatch.Elapsed);
            return success;
        }
        catch (Exception ex)
        {
            OnExportCompleted(false, filePath, format, 0, TimeSpan.Zero, ex.Message);
            return false;
        }
    }

    public async Task<bool> ExportCollectionAsync(Collection collection, IEnumerable<Hymn> hymns, ExportFormat format, string filePath)
    {
        try
        {
            var hymnList = hymns.ToList();
            OnProgressChanged(0, $"Exporting collection: {collection.Name}", 0, hymnList.Count);

            // Add collection header information
            var content = new StringBuilder();
            content.AppendLine($"Collection: {collection.Name}");
            if (!string.IsNullOrEmpty(collection.Description))
            {
                content.AppendLine($"Description: {collection.Description}");
            }
            content.AppendLine($"Created: {collection.CreatedDate:yyyy-MM-dd}");
            content.AppendLine($"Hymns: {hymnList.Count}");
            content.AppendLine(new string('=', 50));
            content.AppendLine();

            return await ExportHymnsAsync(hymnList, format, filePath, true);
        }
        catch (Exception ex)
        {
            OnExportCompleted(false, filePath, format, 0, TimeSpan.Zero, ex.Message);
            return false;
        }
    }

    public async Task<bool> ShareHymnAsync(Hymn hymn, ExportFormat format)
    {
        try
        {
            var tempDir = Path.Combine(FileSystem.CacheDirectory, "Shares");
            if (!Directory.Exists(tempDir))
                Directory.CreateDirectory(tempDir);

            var fileName = $"{SanitizeFileName(hymn.DisplayTitle)}.{format.ToString().ToLower()}";
            var tempFilePath = Path.Combine(tempDir, fileName);

            var success = await ExportHymnAsync(hymn, format, tempFilePath);
            if (!success)
                return false;

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = $"Share Hymn: {hymn.DisplayTitle}",
                File = new ShareFile(tempFilePath)
            });

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Share failed: {ex.Message}");
            return false;
        }
    }

    public async Task<List<ExportFormat>> GetAvailableFormatsAsync()
    {
        await Task.CompletedTask;
        return Enum.GetValues<ExportFormat>().ToList();
    }

    public async Task<string> GetDefaultExportDirectoryAsync()
    {
        return await _settingsService.GetExportDirectoryAsync();
    }

    public async Task<bool> CreateBackupAsync(string backupPath)
    {
        try
        {
            OnProgressChanged(0, "Creating backup...", 0, 1);
            
            var success = await _databaseService.BackupDatabaseAsync(backupPath);
            
            OnProgressChanged(100, "Backup completed", 1, 1);
            return success;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Backup failed: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RestoreBackupAsync(string backupPath)
    {
        try
        {
            OnProgressChanged(0, "Restoring backup...", 0, 1);
            
            var success = await _databaseService.RestoreDatabaseAsync(backupPath);
            
            OnProgressChanged(100, "Restore completed", 1, 1);
            return success;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Restore failed: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExportToTextAsync(List<Hymn> hymns, string filePath, bool includeMetadata)
    {
        try
        {
            using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
            
            for (int i = 0; i < hymns.Count; i++)
            {
                var hymn = hymns[i];
                OnProgressChanged((i * 100) / hymns.Count, $"Exporting {hymn.DisplayTitle}...", i, hymns.Count);

                if (includeMetadata)
                {
                    await writer.WriteLineAsync($"Title: {hymn.Title}");
                    if (!string.IsNullOrWhiteSpace(hymn.Number ))
                        await writer.WriteLineAsync($"Number: {hymn.Number}");
                    if (!string.IsNullOrEmpty(hymn.Language))
                        await writer.WriteLineAsync($"Language: {hymn.Language}");
                    if (!string.IsNullOrEmpty(hymn.Tags))
                        await writer.WriteLineAsync($"Tags: {hymn.Tags}");
                    await writer.WriteLineAsync($"Created: {hymn.CreatedDate:yyyy-MM-dd HH:mm}");
                    await writer.WriteLineAsync(new string('-', 50));
                }

                await writer.WriteLineAsync(hymn.Lyrics);
                
                if (i < hymns.Count - 1)
                {
                    await writer.WriteLineAsync();
                    await writer.WriteLineAsync(new string('=', 50));
                    await writer.WriteLineAsync();
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Text export failed: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExportToDocxAsync(List<Hymn> hymns, string filePath, bool includeMetadata)
    {
        try
        {
            using var document = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document);
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());

            for (int i = 0; i < hymns.Count; i++)
            {
                var hymn = hymns[i];
                OnProgressChanged((i * 100) / hymns.Count, $"Exporting {hymn.DisplayTitle}...", i, hymns.Count);

                // Add title
                var titleParagraph = new OpenXmlParagraph();
                var titleRun = new Run();
                var titleRunProperties = new RunProperties();
                titleRunProperties.Append(new Bold());
                titleRunProperties.Append(new FontSize() { Val = "24" });
                titleRun.Append(titleRunProperties);
                titleRun.Append(new Text(hymn.DisplayTitle));
                titleParagraph.Append(titleRun);
                body.Append(titleParagraph);

                if (includeMetadata)
                {
                    // Add metadata
                    if (!string.IsNullOrEmpty(hymn.Number))
                        body.Append(CreateParagraph($"Number: {hymn.Number}"));
                    if (!string.IsNullOrEmpty(hymn.Language))
                        body.Append(CreateParagraph($"Language: {hymn.Language}"));
                    if (!string.IsNullOrEmpty(hymn.Tags))
                        body.Append(CreateParagraph($"Tags: {hymn.Tags}"));
                    body.Append(CreateParagraph($"Created: {hymn.CreatedDate:yyyy-MM-dd HH:mm}"));
                    
                    // Add separator
                    body.Append(CreateParagraph(new string('-', 50)));
                }

                // Add lyrics
                var lyricsLines = hymn.Lyrics.Split('\n');
                foreach (var line in lyricsLines)
                {
                    body.Append(CreateParagraph(line));
                }

                // Add page break if not last hymn
                if (i < hymns.Count - 1)
                {
                    var pageBreakParagraph = new OpenXmlParagraph();
                    var pageBreakRun = new Run();
                    pageBreakRun.Append(new Break() { Type = BreakValues.Page });
                    pageBreakParagraph.Append(pageBreakRun);
                    body.Append(pageBreakParagraph);
                }
            }

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"DOCX export failed: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExportToPdfAsync(List<Hymn> hymns, string filePath, bool includeMetadata)
    {
        try
        {
            using var writer = new PdfWriter(filePath);
            using var pdf = new PdfDocument(writer);
            using var document = new iText.Layout.Document(pdf);

            for (int i = 0; i < hymns.Count; i++)
            {
                var hymn = hymns[i];
                OnProgressChanged((i * 100) / hymns.Count, $"Exporting {hymn.DisplayTitle}...", i, hymns.Count);

                // Add title
                document.Add(new iText.Layout.Element.Paragraph(hymn.DisplayTitle)
                    .SetFontSize(18)
                    .SetBold()
                    .SetMarginBottom(10));

                if (includeMetadata)
                {
                    // Add metadata
                    if (!string.IsNullOrEmpty(hymn.Number))
                        document.Add(new iText.Layout.Element.Paragraph($"Number: {hymn.Number}").SetFontSize(10));
                    if (!string.IsNullOrEmpty(hymn.Language))
                        document.Add(new iText.Layout.Element.Paragraph($"Language: {hymn.Language}").SetFontSize(10));
                    if (!string.IsNullOrEmpty(hymn.Tags))
                        document.Add(new iText.Layout.Element.Paragraph($"Tags: {hymn.Tags}").SetFontSize(10));
                    document.Add(new iText.Layout.Element.Paragraph($"Created: {hymn.CreatedDate:yyyy-MM-dd HH:mm}").SetFontSize(10));
                    
                    document.Add(new iText.Layout.Element.Paragraph(new string('-', 50)).SetMarginBottom(10));
                }

                // Add lyrics
                var lyricsLines = hymn.Lyrics.Split('\n');
                foreach (var line in lyricsLines)
                {
                    document.Add(new iText.Layout.Element.Paragraph(line).SetFontSize(12));
                }

                // Add page break if not last hymn
                if (i < hymns.Count - 1)
                {
                    document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
                }
            }

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"PDF export failed: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExportToJsonAsync(List<Hymn> hymns, string filePath, bool includeMetadata)
    {
        try
        {
            var exportData = hymns.Select(h => new
            {
                h.Id,
                h.Title,
                h.Number,
                h.Lyrics,
                h.Language,
                h.Tags,
                h.Notes,
                CreatedDate = h.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                ModifiedDate = h.ModifiedDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                h.IsFavorite,
                h.ViewCount
            });

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(exportData, options);
            await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"JSON export failed: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExportToCsvAsync(List<Hymn> hymns, string filePath, bool includeMetadata)
    {
        try
        {
            using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
            
            // Write header
            await writer.WriteLineAsync("Id,Title,Number,Language,Tags,CreatedDate,ModifiedDate,IsFavorite,ViewCount,Lyrics");
            
            for (int i = 0; i < hymns.Count; i++)
            {
                var hymn = hymns[i];
                OnProgressChanged((i * 100) / hymns.Count, $"Exporting {hymn.DisplayTitle}...", i, hymns.Count);

                var line = $"{hymn.Id}," +
                          $"\"{EscapeCsv(hymn.Title)}\"," +
                          $"{hymn.Number ?? ""}," +
                          $"\"{EscapeCsv(hymn.Language)}\"," +
                          $"\"{EscapeCsv(hymn.Tags)}\"," +
                          $"{hymn.CreatedDate:yyyy-MM-dd HH:mm:ss}," +
                          $"{hymn.ModifiedDate:yyyy-MM-dd HH:mm:ss}," +
                          $"{hymn.IsFavorite}," +
                          $"{hymn.ViewCount}," +
                          $"\"{EscapeCsv(hymn.Lyrics)}\"";
                
                await writer.WriteLineAsync(line);
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"CSV export failed: {ex.Message}");
            return false;
        }
    }

    private static OpenXmlParagraph CreateParagraph(string text)
    {
        var paragraph = new OpenXmlParagraph();
        var run = new Run();
        run.Append(new Text(text));
        paragraph.Append(run);
        return paragraph;
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        
        return value.Replace("\"", "\"\"").Replace("\n", " ").Replace("\r", "");
    }

    private void OnProgressChanged(int percentage, string status, int currentItem, int totalItems)
    {
        ProgressChanged?.Invoke(this, new ExportProgressEventArgs
        {
            ProgressPercentage = percentage,
            Status = status,
            CurrentItem = currentItem,
            TotalItems = totalItems
        });
    }

    private void OnExportCompleted(bool isSuccessful, string filePath, ExportFormat format, int itemsExported, TimeSpan processingTime, string? errorMessage = null)
    {
        ExportCompleted?.Invoke(this, new ExportCompletedEventArgs
        {
            IsSuccessful = isSuccessful,
            FilePath = filePath,
            Format = format,
            ItemsExported = itemsExported,
            ProcessingTime = processingTime,
            ErrorMessage = errorMessage
        });
        
        // Increment export count for statistics
        _settingsService.IncrementExportCount();
    }

    public async Task<bool> ExportCollectionAsync(Collection collection, ExportFormat format)
    {
        try
        {
            var hymns = await _databaseService.GetCollectionHymnsAsync(collection.Id);
            var fileName = $"{SanitizeFileName(collection.Name)}.{format.ToString().ToLower()}";
            var exportDir = await GetDefaultExportDirectoryAsync();
            var filePath = Path.Combine(exportDir, fileName);
            
            return await ExportHymnsAsync(hymns, format, filePath, true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Collection export failed: {ex.Message}");
            return false;
        }
    }
}