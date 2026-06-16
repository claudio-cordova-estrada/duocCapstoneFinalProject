using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using planificApp.Data;
using planificApp.Services;
using PlanificApp.Models;
using PlanificApp.Models.Repositories.Interfaces;
using PlanificApp.Models.Services.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace planificApp.ViewModels;

public partial class EstadisticaUsuarioViewModel : PageViewModel
{
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IRegionesService _regionesService;
    private readonly INavigationService _navigation;
    private readonly IMetricasService _metricasService;

    [ObservableProperty] private Usuario? _usuarioActual;

    // Métricas del usuario
    [ObservableProperty] private int _tareasCreadas;
    [ObservableProperty] private int _tareasCompletadas;
    [ObservableProperty] private int _generacionesRealizadas;

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
                // Disparo normal cuando el usuario cambia la región manualmente desde la UI
                _ = CargarComunasAsync(value);
            }
        }
    }

    [ObservableProperty] private string _comunaSeleccionada = "Selecciona una comuna";

    public EstadisticaUsuarioViewModel(
        IUsuarioRepository usuarioRepo,
        IRegionesService regionesService,
        INavigationService navigation,
        IMetricasService metricasService)
    {
        _usuarioRepo = usuarioRepo;
        _regionesService = regionesService;
        _navigation = navigation;
        _metricasService = metricasService;
        PageName = ApplicationPageNames.AdminUsuarioDetalle;

        _ = CargarRegionesAsync();
    }

    // SOLUCIÓN: Cambiado a async para controlar la secuencia de inicialización geográfica de forma segura
    public async void SetUsuario(Usuario usuario)
    {
        if (usuario == null) return;

        UsuarioActual = usuario;
        Nombre = usuario.NombreCompleto ?? string.Empty;
        Correo = usuario.Correo ?? string.Empty;
        EstaActivo = usuario.EstaActivo;

        // Formateamos la fecha de registro de manera profesional
        FechaNacimiento = usuario.FecCreacion.ToString("dd 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("es-CL"));

        if (!string.IsNullOrEmpty(usuario.Ubicacion) && usuario.Ubicacion.Contains(","))
        {
            var partes = usuario.Ubicacion.Split(',');
            var comuna = partes[0].Trim();
            var region = partes[1].Trim();

            // 1. Asignamos el campo privado de la región para evitar el disparo automático descontrolado
            _regionSeleccionada = region;
            OnPropertyChanged(nameof(RegionSeleccionada));

            // 2. Esperamos de forma síncrona a que carguen las comunas y pasamos la comuna destino para seleccionarla al final
            await CargarComunasAsync(region, comuna);
        }
        else
        {
            RegionSeleccionada = "Selecciona una región";
            ComunaSeleccionada = "Selecciona una comuna";
        }

        // Ejecutamos el servicio central de métricas
        _ = CargarMetricasAsync(usuario);
    }

    private async Task CargarRegionesAsync()
    {
        var regs = await _regionesService.ObtenerRegionesAsync();
        Regiones = new ObservableCollection<string>(regs);
    }

    // REFACTORIZACIÓN SOLID: Permite coordinar la carga antes de forzar la selección de la comuna
    private async Task CargarComunasAsync(string region, string? comunaParaSeleccionar = null)
    {
        if (string.IsNullOrEmpty(region) || region.Contains("Selecciona")) return;
        var coms = await _regionesService.ObtenerComunasPorRegionAsync(region);
        Comunas = new ObservableCollection<string>(coms);

        // Si venimos del método SetUsuario, asignamos la comuna de forma segura tras poblar la lista
        if (!string.IsNullOrEmpty(comunaParaSeleccionar))
        {
            ComunaSeleccionada = comunaParaSeleccionar;
        }
    }

    [RelayCommand]
    private void Volver() => _navigation.NavigateToPage(ApplicationPageNames.AdminUsuarios);

    [RelayCommand]
    private async Task AlternarEstadoUsuario()
    {
        if (UsuarioActual == null) return;

        EstaActivo = !EstaActivo;
        UsuarioActual.EstaActivo = EstaActivo;

        await _usuarioRepo.ActualizarUsuario(UsuarioActual.IdUsuario!, UsuarioActual);
    }

    [RelayCommand]
    private async Task GuardarCambios()
    {
        if (UsuarioActual == null) return;

        // Mapeamos los datos limpios de la UI de vuelta al modelo original de MongoDB
        UsuarioActual.NombreCompleto = Nombre;
        UsuarioActual.Correo = Correo;
        UsuarioActual.Ubicacion = $"{ComunaSeleccionada}, {RegionSeleccionada}";

        // Persistimos los cambios en la base de datos de manera atómica
        await _usuarioRepo.ActualizarUsuario(UsuarioActual.IdUsuario!, UsuarioActual);

        // Redireccionamos al administrador para confirmar visualmente el flujo exitoso
        Volver();
    }

    private async Task CargarMetricasAsync(Usuario usuario)
    {
        var metricas = await _metricasService.ObtenerMetricasUsuarioAsync(usuario, DateTime.Now.Year);

        TareasCreadas = metricas.TareasCreadas;
        TareasCompletadas = metricas.TareasCompletadas;
        GeneracionesRealizadas = metricas.GeneracionesRealizadas;
    }
}