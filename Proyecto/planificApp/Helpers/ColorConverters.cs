using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace planificApp.Helpers;

public class HexToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string hex && !string.IsNullOrEmpty(hex))
            return new SolidColorBrush(Avalonia.Media.Color.Parse(hex));
        return new SolidColorBrush(Colors.Transparent);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}