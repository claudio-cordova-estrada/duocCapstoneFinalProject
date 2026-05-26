namespace planificApp.Helpers;
using System.Globalization;

public static class StringHelper
{
    public static string ToTitleCase(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return texto;
        var ti = CultureInfo.CurrentCulture.TextInfo;
        return ti.ToTitleCase(texto.ToLower());
    }
}