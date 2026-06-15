using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;

namespace planificApp.ViewModels;

public partial class CalendarioDiaViewModel : ObservableObject
{
    public DateTime Fecha { get; set; }

    [ObservableProperty] private int _numeroDia;
    [ObservableProperty] private bool _esMesActual;
    [ObservableProperty] private bool _esHoy;

    // Colección de colores (Hex) de las áreas de interés para este día específico
    [ObservableProperty] private ObservableCollection<string> _coloresAreas = new();
}