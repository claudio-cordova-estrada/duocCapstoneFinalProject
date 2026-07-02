using System;
using System.Globalization;
using Avalonia.Data.Converters;
using PlanificApp.Models;

namespace planificApp.Helpers;

public static class TareaAtrasada
{
    public static bool EsAtrasada(Tarea tarea)
    {
        if (tarea.FecCompletado != null) return false;

        var ahora = DateTime.Now;

        // Si hay FecLimite, usar esa como referencia
        if (tarea.FecLimite.HasValue)
        {
            var fechaLimite = tarea.FecLimite.Value.Date;

            if (tarea.HoraFin.HasValue)
            {
                // FecLimite + HoraFin → atrasada si ahora > FecLimite.HoraFin
                var limite = fechaLimite + tarea.HoraFin.Value;
                return ahora > limite;
            }
            if (tarea.HoraInicio.HasValue)
            {
                // FecLimite + HoraInicio → atrasada si ahora > FecLimite.HoraInicio
                var limite = fechaLimite + tarea.HoraInicio.Value;
                return ahora > limite;
            }
            // Solo FecLimite → atrasada si pasó al día siguiente
            return ahora.Date > fechaLimite;
        }

        // Si hay FecInicio, usar esa
        if (tarea.FecInicio.HasValue)
        {
            var fechaInicio = tarea.FecInicio.Value.Date;

            if (tarea.HoraFin.HasValue)
            {
                // FecInicio + HoraFin → atrasada si ahora > FecInicio.HoraFin
                var limite = fechaInicio + tarea.HoraFin.Value;
                return ahora > limite;
            }
            if (tarea.HoraInicio.HasValue)
            {
                // FecInicio + HoraInicio → atrasada si ahora > FecInicio.HoraInicio
                var limite = fechaInicio + tarea.HoraInicio.Value;
                return ahora > limite;
            }
            // Solo FecInicio, sin horas → atrasada si pasó al día siguiente
            return ahora.Date > fechaInicio;
        }

        // Sin FecLimite ni FecInicio, pero con horas → asumir día actual
        if (tarea.HoraFin.HasValue)
        {
            var limite = ahora.Date + tarea.HoraFin.Value;
            return ahora > limite;
        }
        if (tarea.HoraInicio.HasValue)
        {
            var limite = ahora.Date + tarea.HoraInicio.Value;
            return ahora > limite;
        }

        // Sin fechas ni horas → no se puede determinar, no atrasada
        return false;
    }
}

// Devuelve true si la tarea está atrasada (para togglear la clase visual ".atrasada"
// y dejar que los colores los resuelva el tema vía DynamicResource).
public class TareaEsAtrasadaConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is Tarea tarea && TareaAtrasada.EsAtrasada(tarea);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}

public class BoolToChevronConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true)
            return ((char)0xE136).ToString(); // caret-down (Phosphor)
        return ((char)0xE13A).ToString(); // caret-right (Phosphor)
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}
