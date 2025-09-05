using Plugin.Maui.OCR;

namespace COGLyricsScanner.Services;

public interface IOcrService
{
    /// <summary>
    /// Recognizes text from an image file
    /// </summary>
    /// <param name="imagePath">Path to the image file</param>
    /// <param name="language">OCR language (optional, defaults to English)</param>
    /// <returns>Recognized text</returns>
    Task<string> RecognizeTextAsync(string imagePath, string language = "en");
    
    /// <summary>
    /// Recognizes text from an image stream
    /// </summary>
    /// <param name="imageStream">Image stream</param>
    /// <param name="language">OCR language (optional, defaults to English)</param>
    /// <returns>Recognized text</returns>
    Task<string> RecognizeTextAsync(Stream imageStream, string language = "en");
    
    /// <summary>
    /// Recognizes text from a byte array
    /// </summary>
    /// <param name="imageBytes">Image byte array</param>
    /// <param name="language">OCR language (optional, defaults to English)</param>
    /// <returns>Recognized text</returns>
    Task<string> RecognizeTextAsync(byte[] imageBytes, string language = "en");
    
    /// <summary>
    /// Gets available OCR languages
    /// </summary>
    /// <returns>List of supported language codes</returns>
    Task<List<string>> GetAvailableLanguagesAsync();
    
    /// <summary>
    /// Checks if OCR is available on the current platform
    /// </summary>
    /// <returns>True if OCR is available</returns>
    Task<bool> IsAvailableAsync();
    
    /// <summary>
    /// Preprocesses image for better OCR results
    /// </summary>
    /// <param name="imagePath">Path to the image file</param>
    /// <returns>Path to the preprocessed image</returns>
    Task<string> PreprocessImageAsync(string imagePath);
    
    /// <summary>
    /// Gets OCR confidence score for the last recognition
    /// </summary>
    /// <returns>Confidence score (0-100)</returns>
    Task<float> GetLastConfidenceScoreAsync();
    
    /// <summary>
    /// Event fired when OCR progress changes
    /// </summary>
    event EventHandler<OcrProgressEventArgs> ProgressChanged;
    
    /// <summary>
    /// Event fired when OCR completes
    /// </summary>
    event EventHandler<OcrCompletedEventArgs> OcrCompleted;
}

public class OcrProgressEventArgs : EventArgs
{
    public int ProgressPercentage { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class OcrCompletedEventArgs : EventArgs
{
    public string RecognizedText { get; set; } = string.Empty;
    public float ConfidenceScore { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
}