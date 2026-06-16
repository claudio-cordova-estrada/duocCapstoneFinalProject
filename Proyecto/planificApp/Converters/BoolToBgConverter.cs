using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace planificApp.Converters;

public class BoolToBgConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value is bool b && b) ? "#1a10b981" : "#1aef4444";
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}