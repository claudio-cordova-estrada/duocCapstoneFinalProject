using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace planificApp.ViewModels;

// Un punto de color dentro de la celda del día = un área de interés que se trabaja ese día.
public class PuntoAreaDia
{
    public string ColorHex { get; set; } = "#ffffff";
    public string NombreArea { get; set; } = string.Empty;
}

public partial class CalendarioDiaViewModel : ObservableObject
{
    [ObservableProperty] private DateTime _fecha;
    [ObservableProperty] private int _numeroDia;
    [ObservableProperty] private bool _esMesActual;
    [ObservableProperty] private bool _esHoy;

    // Un punto por cada área distinta planificada ese día (color = área).
    public ObservableCollection<PuntoAreaDia> PuntosAreas { get; set; } = new();
}
