using Microsoft.Maui.Controls;
using System.Globalization;

namespace COGLyricsScanner.Converters;

public class StringToViewButtonStyleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var currentView = value?.ToString();
        var targetView = parameter?.ToString();
        
        if (currentView == targetView)
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