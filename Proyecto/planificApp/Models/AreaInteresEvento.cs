using System;

namespace planificApp.Models;

public class AreaInteresEvento
{
    public DateTime Fecha { get; set; }
    public string NombreArea { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#ffffff"; // Color que pintará el punto
}