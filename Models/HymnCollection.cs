using SQLite;

namespace COGLyricsScanner.Models;

[Table("HymnCollections")]
public class HymnCollection
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int HymnId { get; set; }

    public int CollectionId { get; set; }

    public DateTime AddedDate { get; set; } = DateTime.Now;

    public int SortOrder { get; set; } = 0;

    // Navigation properties (not stored in database)
    [Ignore]
    public Hymn? Hymn { get; set; }

    [Ignore]
    public Collection? Collection { get; set; }
}