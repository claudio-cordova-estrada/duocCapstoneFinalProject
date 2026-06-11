using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using PlanificApp.Models.Services.Interfaces;

namespace planificApp.ViewModels;

public partial class RegisterViewModel : ViewModelBase
{
    private readonly IRegionesService _regionesService;

    public ObservableCollection<string> Regiones { get; } = new();
    public ObservableCollection<string> ComunasDisponibles { get; } = new();

    private string? _regionSeleccionada;
    public string? RegionSeleccionada
    {
        get => _regionSeleccionada;
        set
        {
            if (SetProperty(ref _regionSeleccionada, value))
            {
                // Ahora es asíncrono, lo llamamos sin "await" en la propiedad
                _ = ActualizarComunasAsync();
            }
        }
    }

    [ObservableProperty] private string? _comunaSeleccionada;
    [ObservableProperty] private bool _comunasHabilitadas;

    public RegisterViewModel(IRegionesService regionesService)
    {
        _regionesService = regionesService;

        // Disparamos la descarga en segundo plano
        _ = CargarRegionesAsync();
    }

    private async Task CargarRegionesAsync()
    {
        var regiones = await _regionesService.ObtenerRegionesAsync();
        foreach (var region in regiones)
        {
            Regiones.Add(region);
        }
    }

    private async Task ActualizarComunasAsync()
    {
        ComunasDisponibles.Clear();
        ComunaSeleccionada = null;

        if (!string.IsNullOrEmpty(RegionSeleccionada))
        {
            var comunas = await _regionesService.ObtenerComunasPorRegionAsync(RegionSeleccionada);
            foreach (var comuna in comunas)
            {
                ComunasDisponibles.Add(comuna);
            }
            ComunasHabilitadas = true;
        }
        else
        {
            ComunasHabilitadas = false;
        }
    }
}