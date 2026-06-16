using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace planificApp.Converters;

public class BoolToOpacityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value is bool esMesActual && esMesActual) ? 1.0 : 0.25;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}