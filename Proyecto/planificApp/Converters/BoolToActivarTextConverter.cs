using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace planificApp.Converters;

public class BoolToActivarTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Si value es true (Usuario ACTIVO), el botón debe decir para desactivar (Imagen 1)
        if (value is bool estaActivo && estaActivo)
        {
            return "Desactivar usuario";
        }

        // Si value es false (Usuario INACTIVO), el botón debe decir para reactivar (Petición actual)
        return "Reactivar Usuario";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}