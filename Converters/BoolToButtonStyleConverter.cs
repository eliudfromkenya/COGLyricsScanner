using System.Globalization;

namespace COGLyricsScanner.Converters;

public class BoolToButtonStyleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isActive && isActive)
        {
            return Application.Current?.Resources["BaseButtonStyle"];
        }
        return Application.Current?.Resources["OutlineButtonStyle"];
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
