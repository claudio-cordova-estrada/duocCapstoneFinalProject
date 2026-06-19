using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using PlanificApp.Models.Enums;

namespace planificApp.Helpers;

public class TimeToTopConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TimeSpan time)
            return time.TotalMinutes * (64.0 / 60.0);
        return 0.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}

public class DurationToHeightConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TimeSpan duration)
            return Math.Max(duration.TotalMinutes * (64.0 / 60.0), 14);
        return 32.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}

public class BoolToAccentBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true)
            return new SolidColorBrush(Avalonia.Media.Color.Parse("#241f45"));
        return Brushes.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}

public class MetodoTransporteToIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is MetodoTransporte metodo)
            return metodo switch
            {
                MetodoTransporte.Caminar => "\uE80F",
                MetodoTransporte.Auto => "\uE804",
                MetodoTransporte.Bus => "\uE806",
                _ => "\uE804"
            };
        return "\uE804";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}

public class MetodoTransporteToTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is MetodoTransporte metodo)
            return metodo switch
            {
                MetodoTransporte.Caminar => "A pie",
                MetodoTransporte.Auto => "Auto",
                MetodoTransporte.Bus => "Bus",
                _ => "Auto"
            };
        return "Auto";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}