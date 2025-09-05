using SQLite;

namespace COGLyricsScanner.Models;

[Table("Collections")]
public class Collection
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public DateTime ModifiedDate { get; set; } = DateTime.Now;

    [MaxLength(7)] // For hex color codes
    public string? Color { get; set; }

    public bool IsDefault { get; set; } = false;

    public int SortOrder { get; set; } = 0;

    // Navigation properties (not stored in database)
    [Ignore]
    public List<Hymn> Hymns { get; set; } = new List<Hymn>();

    [Ignore]
    public int HymnCount { get; set; }

    [Ignore]
    public string Summary => $"{HymnCount} hymns";
}