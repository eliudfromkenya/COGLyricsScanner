using COGLyricsScanner.Models;
using Plugin.Maui.OCR;
using System.Diagnostics;

namespace COGLyricsScanner.Services;

public class OcrService : IOcrService
{
    private readonly Plugin.Maui.OCR.IOcrService _ocrService;
    private float _lastConfidenceScore;
    private readonly List<string> _supportedLanguages;

    public event EventHandler<OcrProgressEventArgs>? ProgressChanged;
    public event EventHandler<OcrCompletedEventArgs>? OcrCompleted;

    public OcrService()
    {
        _ocrService = OcrPlugin.Default;
        _supportedLanguages = new List<string>
        {
            "en", "es", "fr", "de", "it", "pt", "ru", "zh", "ja", "ko",
            "ar", "hi", "th", "vi", "nl", "sv", "da", "no", "fi", "pl"
        };
    }

    public async Task<string> RecognizeTextAsync(string imagePath, string language = "en")
    {
        try
        {
            if (!File.Exists(imagePath))
                throw new FileNotFoundException($"Image file not found: {imagePath}");

            OnProgressChanged(10, "Loading image...");
            
            // Preprocess image for better OCR results
            var preprocessedPath = await PreprocessImageAsync(imagePath);
            OnProgressChanged(30, "Preprocessing image...");

            // Read image bytes
            var imageBytes = await File.ReadAllBytesAsync(preprocessedPath);
            OnProgressChanged(50, "Starting OCR recognition...");

            var stopwatch = Stopwatch.StartNew();
            var result = await RecognizeTextAsync(imageBytes, language);
            stopwatch.Stop();

            OnProgressChanged(100, "OCR completed");
            OnOcrCompleted(result, _lastConfidenceScore, stopwatch.Elapsed, true);

            // Clean up preprocessed image if it's different from original
            if (preprocessedPath != imagePath && File.Exists(preprocessedPath))
            {
                try
                {
                    File.Delete(preprocessedPath);
                }
                catch { /* Ignore cleanup errors */ }
            }

            return result;
        }
        catch (Exception ex)
        {
            OnOcrCompleted(string.Empty, 0, TimeSpan.Zero, false, ex.Message);
            throw;
        }
    }

    public async Task<string> RecognizeTextAsync(Stream imageStream, string language = "en")
    {
        try
        {
            OnProgressChanged(20, "Reading image stream...");
            
            using var memoryStream = new MemoryStream();
            await imageStream.CopyToAsync(memoryStream);
            var imageBytes = memoryStream.ToArray();
            
            return await RecognizeTextAsync(imageBytes, language);
        }
        catch (Exception ex)
        {
            OnOcrCompleted(string.Empty, 0, TimeSpan.Zero, false, ex.Message);
            throw;
        }
    }

    public async Task<string> RecognizeTextAsync(byte[] imageBytes, string language = "en")
    {
        try
        {
            if (imageBytes == null || imageBytes.Length == 0)
                throw new ArgumentException("Image bytes cannot be null or empty");

            OnProgressChanged(60, "Processing OCR...");

            var stopwatch = Stopwatch.StartNew();
            
            // Use the OCR plugin to recognize text
            var ocrResult = await _ocrService.RecognizeTextAsync(imageBytes, tryHard: true);

            stopwatch.Stop();

            var recognizedText = ocrResult?.AllText ?? string.Empty;
            _lastConfidenceScore = CalculateConfidenceScore(recognizedText);

            OnProgressChanged(90, "Processing results...");
            
            // Post-process the recognized text
            var processedText = PostProcessText(recognizedText);
            
            OnProgressChanged(100, "OCR completed");
            OnOcrCompleted(processedText, _lastConfidenceScore, stopwatch.Elapsed, true);

            return processedText;
        }
        catch (Exception ex)
        {
            OnOcrCompleted(string.Empty, 0, TimeSpan.Zero, false, ex.Message);
            throw new InvalidOperationException($"OCR recognition failed: {ex.Message}", ex);
        }
    }

    public async Task<List<string>> GetAvailableLanguagesAsync()
    {
        await Task.CompletedTask;
        return new List<string>(_supportedLanguages);
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            await Task.CompletedTask;
            return _ocrService != null;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string> PreprocessImageAsync(string imagePath)
    {
        try
        {
            // For now, return the original path
            // In a more advanced implementation, you could:
            // - Adjust brightness/contrast
            // - Convert to grayscale
            // - Apply noise reduction
            // - Correct skew/rotation
            // - Enhance text clarity
            
            await Task.CompletedTask;
            return imagePath;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Image preprocessing failed: {ex.Message}");
            return imagePath; // Return original if preprocessing fails
        }
    }

    public async Task<float> GetLastConfidenceScoreAsync()
    {
        await Task.CompletedTask;
        return _lastConfidenceScore;
    }

    private string PostProcessText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Clean up common OCR errors
        var processed = text
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Trim();

        // Remove excessive whitespace
        processed = System.Text.RegularExpressions.Regex.Replace(processed, @"\n\s*\n", "\n\n");
        processed = System.Text.RegularExpressions.Regex.Replace(processed, @"[ \t]+", " ");

        // Fix common character recognition errors for hymn lyrics
        var corrections = new Dictionary<string, string>
        {
            { "0", "O" }, // Zero to O
            { "1", "I" }, // One to I (in some contexts)
            { "5", "S" }, // Five to S (in some contexts)
            { "8", "B" }, // Eight to B (in some contexts)
        };

        // Apply corrections cautiously (only for single characters surrounded by letters)
        foreach (var correction in corrections)
        {
            var pattern = $@"\b{correction.Key}\b";
            if (System.Text.RegularExpressions.Regex.IsMatch(processed, @"[a-zA-Z]" + pattern + @"[a-zA-Z]"))
            {
                processed = System.Text.RegularExpressions.Regex.Replace(processed, pattern, correction.Value);
            }
        }

        return processed;
    }

    private float CalculateConfidenceScore(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0f;

        // Simple confidence calculation based on text characteristics
        float score = 50f; // Base score

        // Increase score for reasonable text length
        if (text.Length > 10)
            score += 10f;
        if (text.Length > 50)
            score += 10f;

        // Increase score for proper word structure
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var validWords = words.Count(w => w.Length > 1 && System.Text.RegularExpressions.Regex.IsMatch(w, @"^[a-zA-Z]+$"));
        var wordRatio = words.Length > 0 ? (float)validWords / words.Length : 0f;
        score += wordRatio * 20f;

        // Decrease score for excessive special characters
        var specialCharCount = text.Count(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c));
        var specialCharRatio = text.Length > 0 ? (float)specialCharCount / text.Length : 0f;
        if (specialCharRatio > 0.1f)
            score -= (specialCharRatio - 0.1f) * 30f;

        return Math.Max(0f, Math.Min(100f, score));
    }

    private void OnProgressChanged(int percentage, string status)
    {
        ProgressChanged?.Invoke(this, new OcrProgressEventArgs
        {
            ProgressPercentage = percentage,
            Status = status
        });
    }

    private void OnOcrCompleted(string text, float confidence, TimeSpan processingTime, bool isSuccessful, string? errorMessage = null)
    {
        OcrCompleted?.Invoke(this, new OcrCompletedEventArgs
        {
            RecognizedText = text,
            ConfidenceScore = confidence,
            ProcessingTime = processingTime,
            IsSuccessful = isSuccessful,
            ErrorMessage = errorMessage
        });
    }
}