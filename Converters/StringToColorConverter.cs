using Microsoft.Maui.Controls;
using System.Globalization;

namespace COGLyricsScanner.Converters;

public class StringToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string colorString && !string.IsNullOrEmpty(colorString))
        {
            try
            {
                return Color.FromArgb(colorString);
            }
            catch
            {
                return Colors.Blue; // Default color
            }
        }
        return Colors.Blue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Color color)
        {
            return color.ToArgbHex();
        }
        return "#0000FF";
    }
}