using System.Globalization;

namespace COGLyricsScanner.Converters;

public class BoolToSortIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool ascending)
        {
            return ascending ? "↑" : "↓";
        }
        return "↑";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
