using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace planificApp.Converters;

public class BoolToActionTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value is bool b && b) ? "Desactivar usuario" : "Reactivar Usuario";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}