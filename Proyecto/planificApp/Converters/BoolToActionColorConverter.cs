using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace planificApp.Converters;

public class BoolToActionColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Si el usuario ESTÁ ACTIVO, el botón será ROJO para sugerir peligro al desactivar
        // Si el usuario ESTÁ INACTIVO, el botón será VERDE para sugerir reactivación
        return (value is bool b && b) ? SolidColorBrush.Parse("#ef4444") : SolidColorBrush.Parse("#10b981");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}