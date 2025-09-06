using Microsoft.Maui.Controls;
using System.Globalization;

namespace COGLyricsScanner.Converters;

public class StringToEmptyMessageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var searchText = value?.ToString();
        
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return "No hymns found.\nTry adjusting your filters or create a new hymn.";
        }
        
        return $"No results for '{searchText}'.\nTry a different search term or create a new hymn.";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}