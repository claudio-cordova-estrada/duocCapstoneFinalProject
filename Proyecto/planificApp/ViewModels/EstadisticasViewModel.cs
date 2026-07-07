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
    private readonly ITareaRepository _tareaRepo;
    private readonly IGeneracionRepository _generacionRepo;

    private List<Usuario> _todosLosUsuarios = new();
    private List<Tarea> _todasLasTareas = new();
    private List<Generacion> _todasLasGeneraciones = new();

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
    [ObservableProperty] private int _generacionesModificadas;
    [ObservableProperty] private int _tareasEnGS;
    [ObservableProperty] private int _tareasModificadasGS;

    // --- OBJETIVOS DEL PROYECTO (KPIs reales vs. meta) ---
    [ObservableProperty] private ObservableCollection<ObjetivoKpi> _objetivos = new();

    public EstadisticasViewModel(IUsuarioRepository usuarioRepo, IRegionesService regionesService, INavigationService navigation, ITareaRepository tareaRepo, IGeneracionRepository generacionRepo)
    {
        PageName = ApplicationPageNames.AdminEstadisticas;
        _usuarioRepo = usuarioRepo;
        _regionesService = regionesService;
        _navigationService = navigation;
        _tareaRepo = tareaRepo;
        _generacionRepo = generacionRepo;

        ConstruirMeses();
        _ = InicializarAsync();
    }

    private async Task InicializarAsync()
    {
        await CargarRegionesAsync();
        var usuariosDb = await _usuarioRepo.ObtenerTodosLosUsuarios();
        _todosLosUsuarios = usuariosDb.ToList();

        // Precargamos tareas y generaciones una sola vez; los filtros recalculan en memoria.
        _todasLasTareas = await _tareaRepo.ObtenerTodasLasTareas();
        _todasLasGeneraciones = await _generacionRepo.ObtenerTodas();

        ActualizarSugerencias();
        CalcularMetricas();
        CalcularObjetivos();
    }

    // Calcula los 4 objetivos específicos del proyecto con datos reales (globales, no filtrados).
    private void CalcularObjetivos()
    {
        try
        {
            var usuarios = _todosLosUsuarios;
            int totalU = usuarios.Count;
            int activos = usuarios.Count(u => u.EstaActivo);

            var usuariosConGen = _todasLasGeneraciones
                .Select(g => g.IdUsuario)
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .ToHashSet();
            int usanGen = usuarios.Count(u => !string.IsNullOrEmpty(u.IdUsuario) && usuariosConGen.Contains(u.IdUsuario!));

            var usadas = _todasLasTareas.Where(t => t.UsoGeneracion == true).ToList();
            int totalUsadas = usadas.Count;
            int noModif = usadas.Count(t => !t.ModificacionGeneracion);
            int completadasTiempo = usadas.Count(t => t.FecCompletado != null && t.CompletadoEnTiempo);

            double p1 = totalU > 0 ? 100.0 * activos / totalU : 0;
            double p2 = totalU > 0 ? 100.0 * usanGen / totalU : 0;
            double p3 = totalUsadas > 0 ? 100.0 * noModif / totalUsadas : 0;
            double p4 = totalUsadas > 0 ? 100.0 * completadasTiempo / totalUsadas : 0;

            Objetivos = new ObservableCollection<ObjetivoKpi>
            {
                new() { Titulo = "Usuarios activos", Descripcion = "Que un 70% de los usuarios sean activos en los primeros 3 meses",
                        Valor = p1, Meta = 70, Detalle = $"{activos} de {totalU} usuarios", Cumple = p1 >= 70 },
                new() { Titulo = "Uso del motor de generación", Descripcion = "Que un 70% use la generación semanal (uso continuo, 6 meses)",
                        Valor = p2, Meta = 70, Detalle = $"{usanGen} de {totalU} usuarios", Cumple = p2 >= 70 },
                new() { Titulo = "Tareas de generación sin modificar", Descripcion = "Que el 60% de las tareas usadas en generación no se modifiquen (3 meses)",
                        Valor = p3, Meta = 60, Detalle = $"{noModif} de {totalUsadas} tareas", Cumple = p3 >= 60 },
                new() { Titulo = "Tareas de generación completadas en tiempo", Descripcion = "Que el 80% de las tareas usadas en generación se completen en tiempo (5 meses)",
                        Valor = p4, Meta = 80, Detalle = $"{completadasTiempo} de {totalUsadas} tareas", Cumple = p4 >= 80 },
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al calcular objetivos: {ex.Message}");
        }
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
        // Ámbito geográfico (región/comuna): universo de usuarios para todas las métricas.
        var usuariosAmbito = _todosLosUsuarios.AsEnumerable();

        if (!string.IsNullOrEmpty(RegionSeleccionada) && RegionSeleccionada != "Todas")
            usuariosAmbito = usuariosAmbito.Where(u => !string.IsNullOrEmpty(u.Ubicacion) && u.Ubicacion.Contains(RegionSeleccionada));

        if (!string.IsNullOrEmpty(ComunaSeleccionada) && ComunaSeleccionada != "Todas")
            usuariosAmbito = usuariosAmbito.Where(u => !string.IsNullOrEmpty(u.Ubicacion) && u.Ubicacion.Contains(ComunaSeleccionada));

        var listaAmbito = usuariosAmbito.ToList();
        var idsAmbito = listaAmbito
            .Select(u => u.IdUsuario)
            .Where(id => !string.IsNullOrEmpty(id))
            .ToHashSet();

        // Usuarios: el filtro de año/mes aplica a su fecha de registro.
        var usuariosPeriodo = listaAmbito.Where(u => EnPeriodo(u.FecCreacion)).ToList();
        TotalUsuarios = usuariosPeriodo.Count;
        UsuariosActivos = usuariosPeriodo.Count(u => u.EstaActivo);

        // Tareas del ámbito, filtradas por periodo según su fecha de creación.
        var tareasPeriodo = _todasLasTareas
            .Where(t => !string.IsNullOrEmpty(t.IdUsuario) && idsAmbito.Contains(t.IdUsuario) && EnPeriodo(t.FecCreacion))
            .ToList();
        TareasCreadas = tareasPeriodo.Count;
        TareasCompletadas = tareasPeriodo.Count(t => t.FecCompletado != null);

        var tareasGS = tareasPeriodo.Where(t => t.UsoGeneracion == true).ToList();
        TareasEnGS = tareasGS.Count;
        TareasModificadasGS = tareasGS.Count(t => t.ModificacionGeneracion);

        // Generaciones del ámbito, filtradas por periodo según su fecha.
        var generacionesPeriodo = _todasLasGeneraciones
            .Where(g => idsAmbito.Contains(g.IdUsuario) && EnPeriodo(g.FecGeneracion))
            .ToList();
        GeneracionesRealizadas = generacionesPeriodo.Count;

        // Usuarios distintos del ámbito con al menos una generación en el periodo.
        UsanGeneracionSemanal = generacionesPeriodo.Select(g => g.IdUsuario).Distinct().Count();

        // El modelo no vincula cada tarea con la generación de la que salió, así que no se puede
        // contar "generaciones modificadas" de forma fiable. Queda en 0 hasta que se registre ese dato.
        GeneracionesModificadas = 0;

        // Promedio real de tareas modificadas por generación.
        CambiosPorGeneracion = GeneracionesRealizadas > 0
            ? (double)TareasModificadasGS / GeneracionesRealizadas
            : 0.0;
    }

    // ¿La fecha cae en el año seleccionado (y en el mes, si hay uno elegido)?
    private bool EnPeriodo(DateTime fecha) =>
        fecha.Year == YearActual && (MesSeleccionado == null || fecha.Month == MesSeleccionado.Value);

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

// KPI de un objetivo específico del proyecto: valor real vs. meta, con estado y color.
public class ObjetivoKpi
{
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public double Valor { get; set; }
    public double Meta { get; set; }
    public string Detalle { get; set; } = string.Empty;
    public bool Cumple { get; set; }

    public string ValorTexto => $"{Valor:F0}%";
    public string MetaTexto => $"Meta {Meta:F0}%";
    public string ColorHex => Cumple ? "#4ade80" : "#fbbf24";
    public string TextoEstado => Cumple ? "Cumple" : "En progreso";
}