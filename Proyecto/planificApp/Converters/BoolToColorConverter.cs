using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace planificApp.Converters;

public class BoolToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value is bool b && b) ? "#10b981" : "#ef4444";
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}