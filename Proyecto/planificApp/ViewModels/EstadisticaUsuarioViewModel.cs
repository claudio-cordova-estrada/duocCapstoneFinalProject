using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using planificApp.Data;
using planificApp.Services;
using PlanificApp.Models;
using PlanificApp.Models.Repositories.Interfaces;
using PlanificApp.Models.Services.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace planificApp.ViewModels;

public partial class EstadisticaUsuarioViewModel : PageViewModel
{
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IRegionesService _regionesService;
    private readonly INavigationService _navigation;

    [ObservableProperty] private Usuario? _usuarioActual;

    // Propiedades editables
    [ObservableProperty] private string _nombre = string.Empty;
    [ObservableProperty] private string _correo = string.Empty;
    [ObservableProperty] private string _fechaNacimiento = string.Empty;
    [ObservableProperty] private bool _estaActivo;

    // Listas Geográficas
    [ObservableProperty] private ObservableCollection<string> _regiones = new();
    [ObservableProperty] private ObservableCollection<string> _comunas = new();

    private string _regionSeleccionada = "Selecciona una región";
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

    [ObservableProperty] private string _comunaSeleccionada = "Selecciona una comuna";

    public EstadisticaUsuarioViewModel(IUsuarioRepository usuarioRepo, IRegionesService regionesService, INavigationService navigation)
    {
        _usuarioRepo = usuarioRepo;
        _regionesService = regionesService;
        _navigation = navigation;
        PageName = ApplicationPageNames.AdminUsuarioDetalle;

        _ = CargarRegionesAsync();
    }

    // Método para recibir al usuario desde la tabla
    public void SetUsuario(Usuario usuario)
    {
        UsuarioActual = usuario;
        EstaActivo = usuario.EstaActivo;
        Nombre = usuario.NombreCompleto ?? "";
        Correo = usuario.Correo ?? "";
        FechaNacimiento = usuario.FecCreacion.ToString("dd MMM yyyy");

        // Intentar separar ubicación actual para pre-cargar combos
        if (!string.IsNullOrEmpty(usuario.Ubicacion) && usuario.Ubicacion.Contains(","))
        {
            var partes = usuario.Ubicacion.Split(',');
            RegionSeleccionada = partes[1].Trim();
            ComunaSeleccionada = partes[0].Trim();
        }
    }

    private async Task CargarRegionesAsync()
    {
        var regs = await _regionesService.ObtenerRegionesAsync();
        Regiones = new ObservableCollection<string>(regs);
    }

    private async Task CargarComunasAsync(string region)
    {
        if (string.IsNullOrEmpty(region) || region.Contains("Selecciona")) return;
        var coms = await _regionesService.ObtenerComunasPorRegionAsync(region);
        Comunas = new ObservableCollection<string>(coms);
    }

    [RelayCommand]
    private void Volver() => _navigation.NavigateToPage(ApplicationPageNames.AdminUsuarios);

    [RelayCommand]
    private async Task AlternarEstadoUsuario()
    {
        if (UsuarioActual == null) return;

        // Invertimos el estado actual
        EstaActivo = !EstaActivo;
        UsuarioActual.EstaActivo = EstaActivo;

        // Guardamos en MongoDB
        await _usuarioRepo.ActualizarUsuario(UsuarioActual.IdUsuario!, UsuarioActual);
    }

    [RelayCommand]
    private async Task GuardarCambios()
    {
        if (UsuarioActual == null) return;

        UsuarioActual.NombreCompleto = Nombre;
        UsuarioActual.Correo = Correo;
        UsuarioActual.Ubicacion = $"{ComunaSeleccionada}, {RegionSeleccionada}";

        await _usuarioRepo.ActualizarUsuario(UsuarioActual.IdUsuario!, UsuarioActual);
        Volver();
    }
}