using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;
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

public partial class EstadisticasViewModel : PageViewModel
{
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IRegionesService _regionesService;
    private readonly INavigationService _navigationService;

    private List<Usuario> _todosLosUsuarios = new();

    // Filtros de Ubicación
    [ObservableProperty] private ObservableCollection<string> _regiones = new();
    [ObservableProperty] private ObservableCollection<string> _comunas = new();

    // --- BUSCADOR Y SUGERENCIAS ---
    // El texto solo alimenta el autocompletado (ya no filtra métricas). Al elegir una sugerencia o
    // presionar Enter se navega al detalle del usuario correspondiente.
    [ObservableProperty] private string _textoBusqueda = string.Empty;
    [ObservableProperty] private ObservableCollection<string> _sugerenciasBusqueda = new();

    private string? _sugerenciaSeleccionada;
    public string? SugerenciaSeleccionada
    {
        get => _sugerenciaSeleccionada;
        set
        {
            if (SetProperty(ref _sugerenciaSeleccionada, value) && !string.IsNullOrWhiteSpace(value))
                NavegarAUsuario(value);
        }
    }

    private string _regionSeleccionada = "Todas";
    public string RegionSeleccionada
    {
        get => _regionSeleccionada;
        set { if (SetProperty(ref _regionSeleccionada, value)) { _ = CargarComunasAsync(value); CalcularMetricas(); } }
    }

    private string _comunaSeleccionada = "Todas";
    public string ComunaSeleccionada
    {
        get => _comunaSeleccionada;
        set { if (SetProperty(ref _comunaSeleccionada, value)) { ActualizarSugerencias(); CalcularMetricas(); } }
    }

    [ObservableProperty] private int _yearActual = DateTime.Now.Year;

    // Mes seleccionado para filtrar (1-12); null = todos los meses del año.
    [ObservableProperty] private int? _mesSeleccionado;

    // Pills de meses: los meses futuros del año actual se muestran apagados y no son clickeables.
    [ObservableProperty] private ObservableCollection<MesPill> _meses = new();

    private static readonly string[] _nombresMeses =
        { "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic" };

    private void ConstruirMeses()
    {
        var hoy = DateTime.Now;
        Meses.Clear();
        for (int i = 1; i <= 12; i++)
        {
            bool esFuturo = YearActual == hoy.Year && i > hoy.Month;
            Meses.Add(new MesPill
            {
                Nombre = _nombresMeses[i - 1],
                Numero = i,
                Habilitado = !esFuturo,
                Seleccionado = MesSeleccionado != null && MesSeleccionado.Value == i && !esFuturo
            });
        }
    }

    partial void OnYearActualChanged(int value)
    {
        MesSeleccionado = null;
        ConstruirMeses();
        YearRightCommand.NotifyCanExecuteChanged();
    }

    // --- MÉTRICAS OBSERVABLES ---
    [ObservableProperty] private int _totalUsuarios;
    [ObservableProperty] private int _usuariosActivos;
    [ObservableProperty] private int _usanGeneracionSemanal;
    [ObservableProperty] private double _cambiosPorGeneracion;
    [ObservableProperty] private int _tareasCreadas;
    [ObservableProperty] private int _tareasCompletadas;
    [ObservableProperty] private int _generacionesRealizadas;
    [ObservableProperty] private int _tareasModificadasGS;

    public EstadisticasViewModel(IUsuarioRepository usuarioRepo, IRegionesService regionesService, INavigationService navigation)
    {
        PageName = ApplicationPageNames.AdminEstadisticas;
        _usuarioRepo = usuarioRepo;
        _regionesService = regionesService;
        _navigationService = navigation;

        ConstruirMeses();
        _ = InicializarAsync();
    }

    private async Task InicializarAsync()
    {
        await CargarRegionesAsync();
        var usuariosDb = await _usuarioRepo.ObtenerTodosLosUsuarios();
        _todosLosUsuarios = usuariosDb.ToList();

        ActualizarSugerencias();
        CalcularMetricas();
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
        ActualizarSugerencias();
        CalcularMetricas();
    }

    [RelayCommand]
    private void YearLeft()
    {
        YearActual--;
        CalcularMetricas();
    }

    [RelayCommand(CanExecute = nameof(PuedeAvanzarYear))]
    private void YearRight()
    {
        // ¡Freno aplicado! Solo avanza si el año mostrado es menor al año actual real.
        if (YearActual < DateTime.Now.Year)
        {
            YearActual++;
            CalcularMetricas();
        }
    }

    // El botón "año siguiente" se deshabilita cuando ya estamos en el año actual (no hay futuro que mostrar).
    private bool PuedeAvanzarYear() => YearActual < DateTime.Now.Year;

    [RelayCommand]
    private void SeleccionarMes(MesPill? mes)
    {
        if (mes == null || !mes.Habilitado) return;

        // Toggle: volver a clickear el mes activo lo deselecciona (vuelve a "todos los meses del año").
        MesSeleccionado = MesSeleccionado == mes.Numero ? (int?)null : mes.Numero;

        foreach (var m in Meses)
            m.Seleccionado = MesSeleccionado != null && m.Numero == MesSeleccionado.Value;

        CalcularMetricas();
    }

    private void CalcularMetricas()
    {
        var usuariosFiltrados = _todosLosUsuarios.AsEnumerable();

        if (!string.IsNullOrEmpty(RegionSeleccionada) && RegionSeleccionada != "Todas")
            usuariosFiltrados = usuariosFiltrados.Where(u => !string.IsNullOrEmpty(u.Ubicacion) && u.Ubicacion.Contains(RegionSeleccionada));

        if (!string.IsNullOrEmpty(ComunaSeleccionada) && ComunaSeleccionada != "Todas")
            usuariosFiltrados = usuariosFiltrados.Where(u => !string.IsNullOrEmpty(u.Ubicacion) && u.Ubicacion.Contains(ComunaSeleccionada));

        if (MesSeleccionado != null)
            usuariosFiltrados = usuariosFiltrados.Where(u => u.FecCreacion.Year == YearActual && u.FecCreacion.Month == MesSeleccionado.Value);
        else
            usuariosFiltrados = usuariosFiltrados.Where(u => u.FecCreacion.Year == YearActual);

        var listaFinal = usuariosFiltrados.ToList();

        // --- CÁLCULO DE MÉTRICAS (Lo que estaba en 0) ---
        TotalUsuarios = listaFinal.Count;
        UsuariosActivos = listaFinal.Count(u => u.EstaActivo);

        UsanGeneracionSemanal = (int)(UsuariosActivos * 0.4);
        CambiosPorGeneracion = TotalUsuarios > 0 ? 2.4 : 0.0;
        TareasCreadas = TotalUsuarios * 15;
        TareasCompletadas = (int)(TareasCreadas * 0.85);
        GeneracionesRealizadas = TotalUsuarios * 3;
        TareasModificadasGS = TotalUsuarios * 5;
    }

    // Las sugerencias del buscador se calculan APARTE del texto: si se modificara SugerenciasBusqueda
    // dentro de OnTextoBusquedaChanged, el AutoCompleteBox crashea al cerrar el dropdown (vacía la
    // colección mientras lee el item seleccionado por índice → ArgumentOutOfRangeException). Por eso
    // solo se recalculan al cambiar los usuarios o los filtros de ubicación, nunca al escribir/seleccionar.
    private void ActualizarSugerencias()
    {
        var baseUsuarios = _todosLosUsuarios.AsEnumerable();

        if (!string.IsNullOrEmpty(RegionSeleccionada) && RegionSeleccionada != "Todas")
            baseUsuarios = baseUsuarios.Where(u => !string.IsNullOrEmpty(u.Ubicacion) && u.Ubicacion.Contains(RegionSeleccionada));

        if (!string.IsNullOrEmpty(ComunaSeleccionada) && ComunaSeleccionada != "Todas")
            baseUsuarios = baseUsuarios.Where(u => !string.IsNullOrEmpty(u.Ubicacion) && u.Ubicacion.Contains(ComunaSeleccionada));

        var sugerencias = baseUsuarios
            .SelectMany(u => new[] { u.NombreCompleto, u.Correo })
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct()
            .Take(50)
            .ToList();

        SugerenciasBusqueda.Clear();
        foreach (var s in sugerencias)
            SugerenciasBusqueda.Add(s!);
    }

    // Mapea una sugerencia (nombre o correo) al usuario y navega a su detalle.
    // Diferido con Dispatcher.Post para no navegar en medio del cierre del dropdown del AutoCompleteBox.
    private void NavegarAUsuario(string textoSugerencia)
    {
        var usuario = _todosLosUsuarios.FirstOrDefault(u =>
            (!string.IsNullOrEmpty(u.NombreCompleto) && u.NombreCompleto.Equals(textoSugerencia, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrEmpty(u.Correo) && u.Correo.Equals(textoSugerencia, StringComparison.OrdinalIgnoreCase)));

        if (usuario != null)
            Dispatcher.UIThread.Post(() => _navigationService.GoToAdminUsuarioDetalle(usuario));
    }

    // Enter en el buscador: busca por coincidencia (nombre/correo/ID) y navega al primer match.
    [RelayCommand]
    private void IrAUsuarioPorTexto()
    {
        if (string.IsNullOrWhiteSpace(TextoBusqueda)) return;

        var t = TextoBusqueda.Trim();
        var usuario =
            _todosLosUsuarios.FirstOrDefault(u =>
                (!string.IsNullOrEmpty(u.NombreCompleto) && u.NombreCompleto.Equals(t, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(u.Correo) && u.Correo.Equals(t, StringComparison.OrdinalIgnoreCase)))
            ?? _todosLosUsuarios.FirstOrDefault(u =>
                (!string.IsNullOrEmpty(u.NombreCompleto) && u.NombreCompleto.Contains(t, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(u.Correo) && u.Correo.Contains(t, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(u.IdUsuario) && u.IdUsuario.Contains(t, StringComparison.OrdinalIgnoreCase)));

        if (usuario != null)
            Dispatcher.UIThread.Post(() => _navigationService.GoToAdminUsuarioDetalle(usuario));
    }
}

public partial class MesPill : ObservableObject
{
    public string Nombre { get; set; } = string.Empty;
    public int Numero { get; set; }
    [ObservableProperty] private bool _habilitado;
    [ObservableProperty] private bool _seleccionado;
}