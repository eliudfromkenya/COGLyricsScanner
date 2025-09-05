using COGLyricsScanner.Models;

namespace COGLyricsScanner.Services;

public interface IDatabaseService
{
    Task InitializeAsync();
    Task<bool> IsInitializedAsync();

    // Hymn operations
    Task<List<Hymn>> GetHymnsAsync();
    Task<List<Hymn>> GetHymnsByBookAsync(int hymnBookId);
    Task<List<Hymn>> GetFavoriteHymnsAsync();
    Task<List<Hymn>> GetRecentHymnsAsync(int count = 10);
    Task<List<Hymn>> SearchHymnsAsync(string searchTerm);
    Task<List<Hymn>> GetHymnsByLanguageAsync(string language);
    Task<List<Hymn>> GetHymnsByTagAsync(string tag);
    Task<Hymn?> GetHymnAsync(int id);
    Task<int> SaveHymnAsync(Hymn hymn);
    Task<int> AddHymnAsync(Hymn hymn);
    Task<bool> UpdateHymnAsync(Hymn hymn);
    Task<bool> DeleteHymnAsync(int hymnId);
    Task<List<Hymn>> GetCollectionHymnsAsync(int collectionId);
    Task<List<HymnCollection>> GetHymnCollectionsByCollectionIdAsync(int collectionId);
    Task<bool> DeleteHymnCollectionAsync(int hymnCollectionId);
    Task<List<Collection>> GetCollectionsByHymnIdAsync(int hymnId);
    Task<bool> ToggleFavoriteAsync(int hymnId);
    Task UpdateViewCountAsync(int hymnId);

    // HymnBook operations
    Task<List<HymnBook>> GetHymnBooksAsync();
    Task<List<HymnBook>> GetHymnBooksByLanguageAsync(string language);
    Task<HymnBook?> GetHymnBookAsync(int id);
    Task<int> SaveHymnBookAsync(HymnBook hymnBook);
    Task<bool> DeleteHymnBookAsync(int id);
    Task<List<HymnBook>> GetHymnBooksWithCountsAsync();

    // Collection operations
    Task<List<Collection>> GetCollectionsAsync();
    Task<Collection?> GetCollectionAsync(int id);
    Task<int> SaveCollectionAsync(Collection collection);
    Task<bool> DeleteCollectionAsync(int id);
    Task<List<Collection>> GetCollectionsWithCountsAsync();

    // HymnCollection operations
    Task<List<Hymn>> GetHymnsInCollectionAsync(int collectionId);
    Task<bool> AddHymnToCollectionAsync(int hymnId, int collectionId);
    Task<bool> RemoveHymnFromCollectionAsync(int hymnId, int collectionId);
    Task<bool> IsHymnInCollectionAsync(int hymnId, int collectionId);
    Task<List<Collection>> GetCollectionsForHymnAsync(int hymnId);

    // Statistics
    Task<int> GetTotalHymnsCountAsync();
    Task<int> GetTotalBooksCountAsync();
    Task<int> GetTotalCollectionsCountAsync();
    Task<int> GetFavoriteHymnsCountAsync();
    Task<List<HymnBook>> GetAllHymnBooksAsync();
    Task<List<string>> GetAvailableLanguagesAsync();
    Task<List<string>> GetAvailableTagsAsync();

    // Backup and restore
    Task<string> ExportDatabaseAsync();
    Task<bool> ImportDatabaseAsync(string filePath);
    Task<bool> BackupDatabaseAsync(string filePath);
    Task<bool> RestoreDatabaseAsync(string filePath);

    // Maintenance
    Task OptimizeDatabaseAsync();
    Task<long> GetDatabaseSizeAsync();
    Task VacuumDatabaseAsync();
}