using SQLite;

namespace COGLyricsScanner.Models;

[Table("HymnBooks")]
public class HymnBook
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Language { get; set; } = "English";

    [MaxLength(200)]
    public string? Publisher { get; set; }

    public int? Year { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public DateTime ModifiedDate { get; set; } = DateTime.Now;

    [MaxLength(7)] // For hex color codes like #FF5733
    public string? Color { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation properties (not stored in database)
    [Ignore]
    public List<Hymn> Hymns { get; set; } = new List<Hymn>();

    [Ignore]
    public int HymnCount { get; set; }

    [Ignore]
    public string DisplayName => !string.IsNullOrEmpty(Publisher) && Year.HasValue 
        ? $"{Name} ({Publisher}, {Year})" 
        : Name;

    [Ignore]
    public string Summary => $"{HymnCount} hymns • {Language}";
}