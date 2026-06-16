using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace planificApp.ViewModels;

// Clase para representar la tarea dentro de la cuadrícula
public class TareaCalendario
{
    public string Titulo { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#ffffff";
    public DateTime Fecha { get; set; }
}

public partial class CalendarioDiaViewModel : ObservableObject
{
    [ObservableProperty] private DateTime _fecha;
    [ObservableProperty] private int _numeroDia;
    [ObservableProperty] private bool _esMesActual;
    [ObservableProperty] private bool _esHoy;

    public ObservableCollection<string> ColoresAreas { get; set; } = new();
    public ObservableCollection<TareaCalendario> TareasDelDia { get; set; } = new();
}