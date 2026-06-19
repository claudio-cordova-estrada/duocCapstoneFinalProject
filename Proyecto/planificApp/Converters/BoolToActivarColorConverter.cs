using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace planificApp.Converters;

public class BoolToActivarColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Si value es true (Usuario ACTIVO), color ROJO para el botón "Desactivar" (Imagen 1)
        if (value is bool estaActivo && estaActivo)
        {
            return SolidColorBrush.Parse("#ef4444");
        }

        // Si value es false (Usuario INACTIVO), color CELESTE para "Reactivar" (Petición actual)
        // He usado un tono celeste cian (#38bdf8) claro y moderno.
        return SolidColorBrush.Parse("#38bdf8");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}