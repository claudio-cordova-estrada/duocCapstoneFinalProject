using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlanificApp.Models.Services.Interfaces;
using planificApp.Views;

namespace planificApp.ViewModels;

public partial class AddLocationWindowViewModel : ObservableObject
{
    private readonly IGeoService? _geoService;

    [ObservableProperty] private string _titulo = "Nueva ubicación";
    [ObservableProperty] private string _nombre = string.Empty;
    [ObservableProperty] private string _direccion = string.Empty;
    [ObservableProperty] private string _selectedColor = "#a78bfa";
    [ObservableProperty] private string _selectedTransport = "Auto";
    [ObservableProperty] private bool _mostrarErrorNombre;

    // El área de la ubicación ya no se elige aquí (se gestiona desde el área vía UbicacionPred).
    // Se conserva el valor que traía la ubicación al editar; en creación queda "General".
    private string _areaInteres = "General";

    public ObservableCollection<string> Sugerencias { get; } = new();

    // La vista se suscribe a este evento para cerrar la ventana con el resultado.
    public event Action<LocationFormData?>? CerrarSolicitado;

    // Constructor para el diseñador
    public AddLocationWindowViewModel() { }

    // Constructor real
    public AddLocationWindowViewModel(IGeoService geoService)
    {
        _geoService = geoService;
    }

    public void ConfigurarEdicion(string name, string direccion, string area, string color, string transport)
    {
        Titulo = "Editar ubicación";
        Nombre = name;
        Direccion = direccion;
        _areaInteres = string.IsNullOrWhiteSpace(area) ? "General" : area;
        if (!string.IsNullOrWhiteSpace(color)) SelectedColor = color;
        if (!string.IsNullOrWhiteSpace(transport)) SelectedTransport = transport;
    }

    public async Task BuscarSugerenciasAsync(string? texto)
    {
        if (_geoService == null) return;
        if (string.IsNullOrWhiteSpace(texto) || texto.Length < 3) return;

        var resultados = await _geoService.GetPredictionsAsync(texto);
        Sugerencias.Clear();
        foreach (var item in resultados) Sugerencias.Add(item);
    }

    [RelayCommand]
    private void SeleccionarColor(string? color)
    {
        if (!string.IsNullOrWhiteSpace(color)) SelectedColor = color;
    }

    [RelayCommand]
    private void SeleccionarTransporte(string? transporte)
    {
        if (!string.IsNullOrWhiteSpace(transporte)) SelectedTransport = transporte;
    }

    [RelayCommand]
    private void Guardar()
    {
        if (string.IsNullOrWhiteSpace(Nombre))
        {
            MostrarErrorNombre = true;
            return;
        }
        MostrarErrorNombre = false;

        CerrarSolicitado?.Invoke(new LocationFormData
        {
            Nombre = Nombre.Trim(),
            Direccion = Direccion ?? "",
            AreaInteres = _areaInteres,
            ColorHex = SelectedColor,
            Transporte = SelectedTransport
        });
    }

    [RelayCommand]
    private void Cancelar() => CerrarSolicitado?.Invoke(null);
}
