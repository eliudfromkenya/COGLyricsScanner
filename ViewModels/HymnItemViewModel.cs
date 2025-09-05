using COGLyricsScanner.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace COGLyricsScanner.ViewModels;

public partial class HymnItemViewModel : ObservableObject
{
    private readonly Hymn _hymn;
    private readonly Action<Hymn>? _onSelected;
    private readonly Action<Hymn>? _onFavoriteToggled;

    public HymnItemViewModel(Hymn hymn, Action<Hymn>? onSelected = null, Action<Hymn>? onFavoriteToggled = null)
    {
        _hymn = hymn;
        _onSelected = onSelected;
        _onFavoriteToggled = onFavoriteToggled;
    }

    public Hymn Hymn => _hymn;

    public string Title => _hymn.Title;
    public int? Number => _hymn.Number;
    public string Language => _hymn.Language;
    public string Tags => _hymn.Tags ?? string.Empty;
    public bool IsFavorite => _hymn.IsFavorite;
    public DateTime CreatedDate => _hymn.CreatedDate;
    public DateTime ModifiedDate => _hymn.ModifiedDate;
    public int ViewCount => _hymn.ViewCount;
    public string Preview => GetPreview();

    public ICommand SelectCommand => new RelayCommand(() => _onSelected?.Invoke(_hymn));
    public ICommand ToggleFavoriteCommand => new RelayCommand(() => _onFavoriteToggled?.Invoke(_hymn));

    private string GetPreview()
    {
        if (string.IsNullOrEmpty(_hymn.Lyrics))
            return "No lyrics available";

        var lines = _hymn.Lyrics.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var preview = string.Join(" ", lines.Take(2));
        
        if (preview.Length > 100)
        {
            preview = preview.Substring(0, 97) + "...";
        }
        
        return preview;
    }

    public string DisplayText => Number.HasValue ? $"{Number}. {Title}" : Title;
    public string SubtitleText => $"{Language} • {CreatedDate:MMM dd, yyyy}";
}