using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using planificApp.Data;
using planificApp.Services;
using PlanificApp.Models;
using PlanificApp.Models.Repositories.Interfaces;
using PlanificApp.Models.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace planificApp.ViewModels;

public partial class UsuariosViewModel : PageViewModel
{
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IRegionesService _regionesService;
    private readonly Func<ApplicationPageNames, PageViewModel> _pageFactory;
    private readonly INavigationService _navigationService;

    private List<Usuario> _todosLosUsuarios = new();

    [ObservableProperty] private ObservableCollection<Usuario> _usuariosVisibles = new();
    [ObservableProperty] private ObservableCollection<string> _regiones = new();
    [ObservableProperty] private ObservableCollection<string> _comunas = new();

    private string _regionSeleccionada = "Todas";
    public string RegionSeleccionada
    {
        get => _regionSeleccionada;
        set { if (SetProperty(ref _regionSeleccionada, value)) { _ = CargarComunasAsync(value); AplicarFiltros(); } }
    }

    private string _comunaSeleccionada = "Todas";
    public string ComunaSeleccionada
    {
        get => _comunaSeleccionada;
        set { if (SetProperty(ref _comunaSeleccionada, value)) AplicarFiltros(); }
    }

    private string _textoBusqueda = string.Empty;
    public string TextoBusqueda
    {
        get => _textoBusqueda;
        set { if (SetProperty(ref _textoBusqueda, value)) AplicarFiltros(); }
    }

    // INYECTAMOS LA FÁBRICA Y LA NAVEGACIÓN
    public UsuariosViewModel(
        IUsuarioRepository usuarioRepo,
        IRegionesService regionesService,
        Func<ApplicationPageNames, PageViewModel> pageFactory,
        INavigationService navigation)
    {
        PageName = ApplicationPageNames.AdminUsuarios;
        _usuarioRepo = usuarioRepo;
        _regionesService = regionesService;
        _pageFactory = pageFactory;
        _navigationService = navigation;

        _ = InicializarAsync();
    }

    private async Task InicializarAsync()
    {
        await CargarRegionesAsync();
        await CargarUsuariosAsync();
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
        _comunaSeleccionada = "Todas";
        OnPropertyChanged(nameof(ComunaSeleccionada));

        if (!string.IsNullOrEmpty(region) && region != "Todas")
        {
            var comunasDb = await _regionesService.ObtenerComunasPorRegionAsync(region);
            foreach (var c in comunasDb) Comunas.Add(c);
        }
        AplicarFiltros();
    }

    private async Task CargarUsuariosAsync()
    {
        try
        {
            var usuariosDb = await _usuarioRepo.ObtenerTodosLosUsuarios();
            _todosLosUsuarios = usuariosDb.ToList();
        }
        catch (Exception ex)
        {
            // SI MONGODB FALLA, DIBUJARÁ ESTE USUARIO FALSO EN TU TABLA PARA AVISARTE
            _todosLosUsuarios = new List<Usuario>
        {
            new Usuario
            {
                IdUsuario = "ERROR",
                NombreCompleto = "Error de Base de Datos",
                Correo = ex.Message, // Aquí veremos el error exacto
                Ubicacion = "Revisa la consola",
                EstaActivo = false
            }
        };
        }
        AplicarFiltros();
    }

    private void AplicarFiltros()
    {
        var filtrados = _todosLosUsuarios.AsEnumerable();

        // Agregamos !string.IsNullOrEmpty para evitar el crash con nulos
        if (!string.IsNullOrEmpty(RegionSeleccionada) && RegionSeleccionada != "Todas")
        {
            filtrados = filtrados.Where(u => !string.IsNullOrEmpty(u.Ubicacion) && u.Ubicacion.Contains(RegionSeleccionada));
        }

        if (!string.IsNullOrEmpty(ComunaSeleccionada) && ComunaSeleccionada != "Todas")
        {
            filtrados = filtrados.Where(u => !string.IsNullOrEmpty(u.Ubicacion) && u.Ubicacion.Contains(ComunaSeleccionada));
        }

        if (!string.IsNullOrWhiteSpace(TextoBusqueda))
        {
            var busqueda = TextoBusqueda.ToLower();
            filtrados = filtrados.Where(u =>
                (!string.IsNullOrEmpty(u.NombreCompleto) && u.NombreCompleto.ToLower().Contains(busqueda)) ||
                (!string.IsNullOrEmpty(u.IdUsuario) && u.IdUsuario.ToLower().Contains(busqueda)));
        }

        UsuariosVisibles.Clear();
        foreach (var u in filtrados)
        {
            UsuariosVisibles.Add(u);
        }
    }

    // --- NUEVO COMANDO DE NAVEGACIÓN ---
    [RelayCommand]
    private void VerDetalleUsuario(Usuario usuarioSeleccionado)
    {
        if (usuarioSeleccionado != null)
        {
            // Usamos el puente que acabamos de crear pasándole el usuario
            _navigationService.GoToAdminUsuarioDetalle(usuarioSeleccionado);
        }
    }
}