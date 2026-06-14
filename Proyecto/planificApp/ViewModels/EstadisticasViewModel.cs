using CommunityToolkit.Mvvm.ComponentModel;
using planificApp.Data;
using planificApp.Services;
using PlanificApp.Models;
using PlanificApp.Models.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace planificApp.ViewModels;

public partial class EstadisticasViewModel : PageViewModel
{
    private readonly IRegionesService _regionesService;

    [ObservableProperty] private ObservableCollection<string> _regiones = new();
    [ObservableProperty] private ObservableCollection<string> _comunas = new();

    private string _regionSeleccionada = "Todas";
    public string RegionSeleccionada
    {
        get => _regionSeleccionada;
        set
        {
            if (SetProperty(ref _regionSeleccionada, value))
            {
                _ = CargarComunasAsync(value);
            }
        }
    }

    [ObservableProperty] private string _comunaSeleccionada = "Todas";

    public EstadisticasViewModel(IRegionesService regionesService)
    {
        PageName = ApplicationPageNames.AdminEstadisticas;
        _regionesService = regionesService;

        _ = CargarRegionesAsync();
    }

    private async Task CargarRegionesAsync()
    {
        var regionesDb = await _regionesService.ObtenerRegionesAsync();

        Regiones.Clear();
        Regiones.Add("Todas");

        foreach (var r in regionesDb) Regiones.Add(r);

        RegionSeleccionada = "Todas";
    }

    private async Task CargarComunasAsync(string region)
    {
        Comunas.Clear();
        Comunas.Add("Todas");
        ComunaSeleccionada = "Todas";

        if (!string.IsNullOrEmpty(region) && region != "Todas")
        {
            var comunasDb = await _regionesService.ObtenerComunasPorRegionAsync(region);
            foreach (var c in comunasDb) Comunas.Add(c);
        }
    }
}