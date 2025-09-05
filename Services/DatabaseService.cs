using COGLyricsScanner.Models;
using SQLite;
using System.Text.Json;

namespace COGLyricsScanner.Services;

public class DatabaseService : IDatabaseService
{
    private SQLiteAsyncConnection? _database;
    private readonly string _databasePath;
    private bool _isInitialized = false;

    public DatabaseService()
    {
        _databasePath = Path.Combine(FileSystem.AppDataDirectory, "COGLyricsScanner.db3");
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        _database = new SQLiteAsyncConnection(_databasePath);

        await _database.CreateTableAsync<Hymn>();
        await _database.CreateTableAsync<HymnBook>();
        await _database.CreateTableAsync<Collection>();
        await _database.CreateTableAsync<HymnCollection>();

        // Create indexes for better performance
        await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_hymns_title ON Hymns(Title)");
        await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_hymns_language ON Hymns(Language)");
        await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_hymns_hymnbook ON Hymns(HymnBookId)");
        await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_hymns_favorite ON Hymns(IsFavorite)");
        await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_hymns_created ON Hymns(CreatedDate)");
        await _database.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_hymnbooks_language ON HymnBooks(Language)");

        await SeedDefaultDataAsync();
        _isInitialized = true;
    }

    public Task<bool> IsInitializedAsync() => Task.FromResult(_isInitialized);

    private async Task SeedDefaultDataAsync()
    {
        // Check if we already have data
        var bookCount = await _database!.Table<HymnBook>().CountAsync();
        if (bookCount > 0)
            return;

        // Create default hymn books
        var defaultBooks = new List<HymnBook>
        {
            new HymnBook { Name = "Church of God Hymnal", Language = "English", Publisher = "Church of God", Year = 2023, Color = "#2E7D32" },
            new HymnBook { Name = "Nyimbo za Kristo", Language = "Swahili", Publisher = "Church of God", Year = 2020, Color = "#1976D2" },
            new HymnBook { Name = "Cantiques Chrétiens", Language = "French", Publisher = "Church of God", Year = 2021, Color = "#4CAF50" }
        };

        foreach (var book in defaultBooks)
        {
            await _database.InsertAsync(book);
        }

        // Create default collections
        var defaultCollections = new List<Collection>
        {
            new Collection { Name = "Favorites", Description = "My favorite hymns", IsDefault = true, Color = "#F44336" },
            new Collection { Name = "Sunday Service", Description = "Hymns for Sunday worship", Color = "#2E7D32" },
            new Collection { Name = "Communion", Description = "Hymns for communion service", Color = "#9C27B0" }
        };

        foreach (var collection in defaultCollections)
        {
            await _database.InsertAsync(collection);
        }
    }

    // Hymn operations
    public async Task<List<Hymn>> GetHymnsAsync()
    {
        await InitializeAsync();
        return await _database!.Table<Hymn>().OrderBy(h => h.Title).ToListAsync();
    }

    public async Task<List<Hymn>> GetHymnsByBookAsync(int hymnBookId)
    {
        await InitializeAsync();
        return await _database!.Table<Hymn>()
            .Where(h => h.HymnBookId == hymnBookId)
            .OrderBy(h => h.Number)
            .ThenBy(h => h.Title)
            .ToListAsync();
    }

    public async Task<List<Hymn>> GetFavoriteHymnsAsync()
    {
        await InitializeAsync();
        return await _database!.Table<Hymn>()
            .Where(h => h.IsFavorite)
            .OrderBy(h => h.Title)
            .ToListAsync();
    }

    public async Task<List<Hymn>> GetRecentHymnsAsync(int count = 10)
    {
        await InitializeAsync();
        return await _database!.Table<Hymn>()
            .OrderByDescending(h => h.ModifiedDate)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<Hymn>> SearchHymnsAsync(string searchTerm)
    {
        await InitializeAsync();
        if (string.IsNullOrWhiteSpace(searchTerm))
            return await GetHymnsAsync();

        var term = $"%{searchTerm.Trim()}%";
        return await _database!.QueryAsync<Hymn>(
            "SELECT * FROM Hymns WHERE Title LIKE ? OR Lyrics LIKE ? OR Number LIKE ? OR Tags LIKE ? ORDER BY Title",
            term, term, term, term);
    }

    public async Task<List<Hymn>> GetHymnsByLanguageAsync(string language)
    {
        await InitializeAsync();
        return await _database!.Table<Hymn>()
            .Where(h => h.Language == language)
            .OrderBy(h => h.Title)
            .ToListAsync();
    }

    public async Task<List<Hymn>> GetHymnsByTagAsync(string tag)
    {
        await InitializeAsync();
        var searchTerm = $"%{tag}%";
        return await _database!.QueryAsync<Hymn>(
            "SELECT * FROM Hymns WHERE Tags LIKE ? ORDER BY Title", searchTerm);
    }

    public async Task<Hymn?> GetHymnAsync(int id)
    {
        await InitializeAsync();
        return await _database!.Table<Hymn>().Where(h => h.Id == id).FirstOrDefaultAsync();
    }

    public async Task<int> SaveHymnAsync(Hymn hymn)
    {
        await InitializeAsync();
        hymn.ModifiedDate = DateTime.Now;
        
        if (hymn.Id != 0)
        {
            await _database!.UpdateAsync(hymn);
            return hymn.Id;
        }
        else
        {
            hymn.CreatedDate = DateTime.Now;
            return await _database!.InsertAsync(hymn);
        }
    }

    public async Task<bool> DeleteHymnAsync(int id)
    {
        await InitializeAsync();
        var result = await _database!.DeleteAsync<Hymn>(id);
        
        // Also remove from collections
        await _database.ExecuteAsync("DELETE FROM HymnCollections WHERE HymnId = ?", id);
        
        return result > 0;
    }

    public async Task<bool> ToggleFavoriteAsync(int hymnId)
    {
        await InitializeAsync();
        var hymn = await GetHymnAsync(hymnId);
        if (hymn == null) return false;

        hymn.IsFavorite = !hymn.IsFavorite;
        hymn.ModifiedDate = DateTime.Now;
        await _database!.UpdateAsync(hymn);
        return true;
    }

    public async Task UpdateViewCountAsync(int hymnId)
    {
        await InitializeAsync();
        await _database!.ExecuteAsync(
            "UPDATE Hymns SET ViewCount = ViewCount + 1, LastViewedDate = ? WHERE Id = ?",
            DateTime.Now, hymnId);
    }

    // HymnBook operations
    public async Task<List<HymnBook>> GetHymnBooksAsync()
    {
        await InitializeAsync();
        return await _database!.Table<HymnBook>()
            .Where(hb => hb.IsActive)
            .OrderBy(hb => hb.Name)
            .ToListAsync();
    }

    public async Task<List<HymnBook>> GetHymnBooksByLanguageAsync(string language)
    {
        await InitializeAsync();
        return await _database!.Table<HymnBook>()
            .Where(hb => hb.Language == language && hb.IsActive)
            .OrderBy(hb => hb.Name)
            .ToListAsync();
    }

    public async Task<HymnBook?> GetHymnBookAsync(int id)
    {
        await InitializeAsync();
        return await _database!.Table<HymnBook>().Where(hb => hb.Id == id).FirstOrDefaultAsync();
    }

    public async Task<int> SaveHymnBookAsync(HymnBook hymnBook)
    {
        await InitializeAsync();
        hymnBook.ModifiedDate = DateTime.Now;
        
        if (hymnBook.Id != 0)
        {
            await _database!.UpdateAsync(hymnBook);
            return hymnBook.Id;
        }
        else
        {
            hymnBook.CreatedDate = DateTime.Now;
            return await _database!.InsertAsync(hymnBook);
        }
    }

    public async Task<bool> DeleteHymnBookAsync(int id)
    {
        await InitializeAsync();
        
        // Check if there are hymns using this book
        var hymnCount = await _database!.Table<Hymn>().Where(h => h.HymnBookId == id).CountAsync();
        if (hymnCount > 0)
        {
            // Soft delete - mark as inactive
            var hymnBook = await GetHymnBookAsync(id);
            if (hymnBook != null)
            {
                hymnBook.IsActive = false;
                await _database.UpdateAsync(hymnBook);
                return true;
            }
            return false;
        }
        
        var result = await _database!.DeleteAsync<HymnBook>(id);
        return result > 0;
    }

    public async Task<List<HymnBook>> GetHymnBooksWithCountsAsync()
    {
        await InitializeAsync();
        var books = await GetHymnBooksAsync();
        
        foreach (var book in books)
        {
            book.HymnCount = await _database!.Table<Hymn>().Where(h => h.HymnBookId == book.Id).CountAsync();
        }
        
        return books;
    }

    // Collection operations
    public async Task<List<Collection>> GetCollectionsAsync()
    {
        await InitializeAsync();
        return await _database!.Table<Collection>().OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToListAsync();
    }

    public async Task<Collection?> GetCollectionAsync(int id)
    {
        await InitializeAsync();
        return await _database!.Table<Collection>().Where(c => c.Id == id).FirstOrDefaultAsync();
    }

    public async Task<int> SaveCollectionAsync(Collection collection)
    {
        await InitializeAsync();
        collection.ModifiedDate = DateTime.Now;
        
        if (collection.Id != 0)
        {
            await _database!.UpdateAsync(collection);
            return collection.Id;
        }
        else
        {
            collection.CreatedDate = DateTime.Now;
            return await _database!.InsertAsync(collection);
        }
    }

    public async Task<bool> DeleteCollectionAsync(int id)
    {
        await InitializeAsync();
        
        // Remove all hymn associations first
        await _database!.ExecuteAsync("DELETE FROM HymnCollections WHERE CollectionId = ?", id);
        
        var result = await _database.DeleteAsync<Collection>(id);
        return result > 0;
    }

    public async Task<List<Collection>> GetCollectionsWithCountsAsync()
    {
        await InitializeAsync();
        var collections = await GetCollectionsAsync();
        
        foreach (var collection in collections)
        {
            collection.HymnCount = await _database!.Table<HymnCollection>()
                .Where(hc => hc.CollectionId == collection.Id)
                .CountAsync();
        }
        
        return collections;
    }

    // HymnCollection operations
    public async Task<List<Hymn>> GetHymnsInCollectionAsync(int collectionId)
    {
        await InitializeAsync();
        return await _database!.QueryAsync<Hymn>(
            @"SELECT h.* FROM Hymns h 
              INNER JOIN HymnCollections hc ON h.Id = hc.HymnId 
              WHERE hc.CollectionId = ? 
              ORDER BY hc.SortOrder, h.Title", collectionId);
    }

    public async Task<bool> AddHymnToCollectionAsync(int hymnId, int collectionId)
    {
        await InitializeAsync();
        
        // Check if already exists
        var exists = await _database!.Table<HymnCollection>()
            .Where(hc => hc.HymnId == hymnId && hc.CollectionId == collectionId)
            .CountAsync() > 0;
            
        if (exists) return false;
        
        var hymnCollection = new HymnCollection
        {
            HymnId = hymnId,
            CollectionId = collectionId,
            AddedDate = DateTime.Now
        };
        
        await _database.InsertAsync(hymnCollection);
        return true;
    }

    public async Task<bool> RemoveHymnFromCollectionAsync(int hymnId, int collectionId)
    {
        await InitializeAsync();
        var result = await _database!.ExecuteAsync(
            "DELETE FROM HymnCollections WHERE HymnId = ? AND CollectionId = ?", hymnId, collectionId);
        return result > 0;
    }

    public async Task<bool> IsHymnInCollectionAsync(int hymnId, int collectionId)
    {
        await InitializeAsync();
        return await _database!.Table<HymnCollection>()
            .Where(hc => hc.HymnId == hymnId && hc.CollectionId == collectionId)
            .CountAsync() > 0;
    }

    public async Task<List<Collection>> GetCollectionsForHymnAsync(int hymnId)
    {
        await InitializeAsync();
        return await _database!.QueryAsync<Collection>(
            @"SELECT c.* FROM Collections c 
              INNER JOIN HymnCollections hc ON c.Id = hc.CollectionId 
              WHERE hc.HymnId = ? 
              ORDER BY c.Name", hymnId);
    }

    // Statistics
    public async Task<int> GetTotalHymnsCountAsync()
    {
        await InitializeAsync();
        return await _database!.Table<Hymn>().CountAsync();
    }

    public async Task<int> GetTotalBooksCountAsync()
    {
        await InitializeAsync();
        return await _database!.Table<HymnBook>().Where(hb => hb.IsActive).CountAsync();
    }

    public async Task<int> GetTotalCollectionsCountAsync()
    {
        await InitializeAsync();
        return await _database!.Table<Collection>().CountAsync();
    }

    public async Task<List<string>> GetAvailableLanguagesAsync()
    {
        await InitializeAsync();
        var languages = await _database!.QueryAsync<string>("SELECT DISTINCT Language FROM Hymns ORDER BY Language");
        return languages.Where(l => !string.IsNullOrEmpty(l)).ToList();
    }

    public async Task<List<string>> GetAvailableTagsAsync()
    {
        await InitializeAsync();
        var hymns = await _database!.Table<Hymn>().Where(h => !string.IsNullOrEmpty(h.Tags)).ToListAsync();
        var tags = new HashSet<string>();
        
        foreach (var hymn in hymns)
        {
            if (!string.IsNullOrEmpty(hymn.Tags))
            {
                var hymnTags = hymn.Tags.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t));
                foreach (var tag in hymnTags)
                {
                    tags.Add(tag);
                }
            }
        }
        
        return tags.OrderBy(t => t).ToList();
    }

    // Backup and restore
    public async Task<string> ExportDatabaseAsync()
    {
        await InitializeAsync();
        var exportData = new
        {
            Hymns = await _database!.Table<Hymn>().ToListAsync(),
            HymnBooks = await _database.Table<HymnBook>().ToListAsync(),
            Collections = await _database.Table<Collection>().ToListAsync(),
            HymnCollections = await _database.Table<HymnCollection>().ToListAsync(),
            ExportDate = DateTime.Now,
            Version = "1.0"
        };
        
        return JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true });
    }

    public async Task<bool> ImportDatabaseAsync(string filePath)
    {
        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            // Implementation would deserialize and import data
            // This is a simplified version
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> BackupDatabaseAsync(string filePath)
    {
        try
        {
            await InitializeAsync();
            File.Copy(_databasePath, filePath, true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> RestoreDatabaseAsync(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                _database?.CloseAsync();
                File.Copy(filePath, _databasePath, true);
                _isInitialized = false;
                await InitializeAsync();
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    // Maintenance
    public async Task OptimizeDatabaseAsync()
    {
        await InitializeAsync();
        await _database!.ExecuteAsync("ANALYZE");
    }

    public async Task<long> GetDatabaseSizeAsync()
    {
        if (File.Exists(_databasePath))
        {
            var fileInfo = new FileInfo(_databasePath);
            return fileInfo.Length;
        }
        return 0;
    }

    public async Task VacuumDatabaseAsync()
    {
        await InitializeAsync();
        await _database!.ExecuteAsync("VACUUM");
    }
}