using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace planificApp.Converters; // ¡Asegúrate que sea planificApp.Converters!

public class BoolToTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value is bool b && b) ? "Desactivar usuario" : "Activar usuario";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}