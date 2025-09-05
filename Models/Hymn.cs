using SQLite;

namespace COGLyricsScanner.Models;

[Table("Hymns")]
public class Hymn
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Number { get; set; }

    [MaxLength(50000)]
    public string Lyrics { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Language { get; set; } = "English";

    public int HymnBookId { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public DateTime ModifiedDate { get; set; } = DateTime.Now;

    public bool IsFavorite { get; set; } = false;

    [MaxLength(500)]
    public string? Tags { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public int ViewCount { get; set; } = 0;

    public DateTime? LastViewedDate { get; set; }

    // Navigation property (not stored in database)
    [Ignore]
    public HymnBook? HymnBook { get; set; }

    // Helper properties
    [Ignore]
    public List<string> TagList
    {
        get => string.IsNullOrEmpty(Tags) ? new List<string>() : Tags.Split(',').Select(t => t.Trim()).ToList();
        set => Tags = string.Join(", ", value);
    }

    [Ignore]
    public string DisplayTitle => !string.IsNullOrEmpty(Number) ? $"{Number}. {Title}" : Title;

    [Ignore]
    public string PreviewText
    {
        get
        {
            if (string.IsNullOrEmpty(Lyrics))
                return "No lyrics available";
            
            var lines = Lyrics.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            return lines.Length > 0 ? lines[0] : "No lyrics available";
        }
    }

    [Ignore]
    public int WordCount => string.IsNullOrEmpty(Lyrics) ? 0 : Lyrics.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

    [Ignore]
    public int LineCount => string.IsNullOrEmpty(Lyrics) ? 0 : Lyrics.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;
}