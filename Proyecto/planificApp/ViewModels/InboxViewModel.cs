using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlanificApp.Models;
using PlanificApp.Models.Services;
using planificApp.Data;
using planificApp.Helpers;

namespace planificApp.ViewModels;

public enum ModoVista { Inbox, Hoy, Semana, Mes }

public partial class InboxViewModel : PageViewModel
{
    private readonly MongoService _mongo;
    private readonly SesionService _sesion;
    private DispatcherTimer? _refreshTimer;

    [ObservableProperty] private ModoVista _modoActual = ModoVista.Inbox;
    [ObservableProperty] private string _tituloVista = "Inbox";
    [ObservableProperty] private string _subtituloVista = string.Empty;

    [ObservableProperty] private ObservableCollection<Tarea> _tareas = new();
    [ObservableProperty] private ObservableCollection<Tarea> _tareasPendientesFiltradas = new();
    [ObservableProperty] private ObservableCollection<Tarea> _tareasCompletadasFiltradas = new();
    [ObservableProperty] private int _tareasPendientes;
    [ObservableProperty] private int _tareasCompletadasCount;
    [ObservableProperty] private bool _hayCompletadas;
    [ObservableProperty] private bool _completadasVisibles = true;
    [ObservableProperty] private string _quickAddNombre = string.Empty;

    [ObservableProperty] private Tarea? _tareaSeleccionada;
    [ObservableProperty] private string _detalleNombre = string.Empty;
    [ObservableProperty] private string _detalleEstado = string.Empty;
    [ObservableProperty] private string _detalleMensaje = string.Empty;

    public DateTime? DetalleFecInicio { get; set; }
    public DateTime? DetalleFecLimite { get; set; }
    public TimeSpan? DetalleHoraInicio { get; set; }
    public TimeSpan? DetalleHoraFin { get; set; }
    public string? DetalleUbicacion { get; set; }
    public int DetallePrioridad { get; set; } = 1;
    public int DetalleTiempoEstimado { get; set; }
    public string? DetalleIdAreaInteres { get; set; }

    [ObservableProperty] private ObservableCollection<AreaInteres> _areasInteres = new();

    public InboxViewModel(MongoService mongo, SesionService sesion)
    {
        _mongo = mongo;
        _sesion = sesion;
        PageName = ApplicationPageNames.UserInbox;

        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _refreshTimer.Tick += async (_, _) => await RefreshTareasAsync();
        _refreshTimer.Start();
    }

    public void SetModo(ModoVista modo)
    {
        ModoActual = modo;
        PageName = modo switch
        {
            ModoVista.Hoy => ApplicationPageNames.UserHoy,
            ModoVista.Semana => ApplicationPageNames.UserSemana,
            ModoVista.Mes => ApplicationPageNames.UserMes,
            _ => ApplicationPageNames.UserInbox
        };

        TituloVista = modo switch
        {
            ModoVista.Hoy => "Hoy",
            ModoVista.Semana => "Esta semana",
            ModoVista.Mes => "Mes",
            _ => "Inbox"
        };

        OnPropertyChanged(nameof(PageName));
    }

    private async Task RefreshTareasAsync()
    {
        if (_sesion.UsuarioActual == null) return;
        await CargarTareasAsync();
    }

    public async Task CargarTareasAsync()
    {
        if (_sesion.UsuarioActual == null) return;

        var idUsuario = _sesion.UsuarioActual.IdUsuario!;

        AreasInteres = new ObservableCollection<AreaInteres>(
            await _mongo.ObtenerAreasPorUsuario(idUsuario));

        var tareas = ModoActual switch
        {
            ModoVista.Hoy => await _mongo.ObtenerTareasPorRango(idUsuario, DateTime.Today, DateTime.Today.AddDays(1)),
            ModoVista.Semana => await ObtenerTareasSemana(idUsuario),
            ModoVista.Mes => await ObtenerTareasMes(idUsuario),
            _ => await _mongo.ObtenerTareasPorUsuario(idUsuario)
        };

        Tareas = new ObservableCollection<Tarea>(tareas);

        SubtituloVista = ModoActual switch
        {
            ModoVista.Hoy => DateTime.Today.ToString("dddd d 'de' MMMM, yyyy"),
            ModoVista.Semana => ObtenerLabelSemana(),
            ModoVista.Mes => DateTime.Today.ToString("MMMM yyyy"),
            _ => $"{Tareas.Count(t => t.FecCompletado == null)} tareas activas"
        };

        AplicarFiltro();
    }

    private async Task<List<Tarea>> ObtenerTareasSemana(string idUsuario)
    {
        var hoy = DateTime.Today;
        var inicioSemana = hoy.AddDays(-(int)hoy.DayOfWeek + 1);
        if (hoy.DayOfWeek == DayOfWeek.Sunday) inicioSemana = inicioSemana.AddDays(-7);
        var finSemana = inicioSemana.AddDays(7);
        return await _mongo.ObtenerTareasPorRango(idUsuario, inicioSemana, finSemana);
    }

    private async Task<List<Tarea>> ObtenerTareasMes(string idUsuario)
    {
        var hoy = DateTime.Today;
        var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
        var finMes = inicioMes.AddMonths(1);
        return await _mongo.ObtenerTareasPorRango(idUsuario, inicioMes, finMes);
    }

    private static string ObtenerLabelSemana()
    {
        var hoy = DateTime.Today;
        var inicioSemana = hoy.AddDays(-(int)hoy.DayOfWeek + 1);
        if (hoy.DayOfWeek == DayOfWeek.Sunday) inicioSemana = inicioSemana.AddDays(-7);
        var finSemana = inicioSemana.AddDays(6);
        return $"{inicioSemana:dd} al {finSemana:dd} de {inicioSemana:MMMM}, {inicioSemana:yyyy}";
    }

    private void AplicarFiltro()
    {
        var pendientes = Tareas.Where(t => t.FecCompletado == null).ToList();
        var completadas = Tareas.Where(t => t.FecCompletado != null).ToList();
        TareasPendientes = pendientes.Count;
        TareasCompletadasCount = completadas.Count;
        HayCompletadas = completadas.Count > 0;

        TareasPendientesFiltradas = new ObservableCollection<Tarea>(pendientes);
        TareasCompletadasFiltradas = new ObservableCollection<Tarea>(completadas);
    }

    [RelayCommand]
    private void ToggleCompletadas()
    {
        CompletadasVisibles = !CompletadasVisibles;
    }

    [RelayCommand]
    private void SeleccionarTarea(Tarea tarea)
    {
        TareaSeleccionada = tarea;
        DetalleNombre = tarea.Nombre;
        DetalleFecInicio = tarea.FecInicio;
        DetalleFecLimite = tarea.FecLimite;
        DetalleHoraInicio = tarea.HoraInicio;
        DetalleHoraFin = tarea.HoraFin;
        DetalleUbicacion = tarea.Ubicacion;
        DetallePrioridad = tarea.Prioridad;
        DetalleTiempoEstimado = tarea.TiempoEstimado;
        DetalleIdAreaInteres = tarea.IdAreaInteres;

        if (tarea.FecCompletado != null)
            DetalleEstado = "Completada";
        else if (TareaAtrasada.EsAtrasada(tarea))
            DetalleEstado = "Vencida";
        else
            DetalleEstado = "Activa";

        DetalleMensaje = string.Empty;
    }

    [RelayCommand]
    private async Task GuardarDetalleAsync()
    {
        if (TareaSeleccionada == null || TareaSeleccionada.IdTarea == null) return;

        if (string.IsNullOrWhiteSpace(DetalleNombre))
        {
            DetalleNombre = TareaSeleccionada.Nombre;
            DetalleMensaje = "El nombre no puede estar vacío.";
            return;
        }

        DetalleMensaje = string.Empty;

        try
        {
            TareaSeleccionada.Nombre = DetalleNombre;
            TareaSeleccionada.FecInicio = DetalleFecInicio;
            TareaSeleccionada.FecLimite = DetalleFecLimite;
            TareaSeleccionada.HoraInicio = DetalleHoraInicio;
            TareaSeleccionada.HoraFin = DetalleHoraFin;
            TareaSeleccionada.Prioridad = DetallePrioridad;
            TareaSeleccionada.Ubicacion = string.IsNullOrWhiteSpace(DetalleUbicacion) ? null : DetalleUbicacion;
            TareaSeleccionada.TiempoEstimado = DetalleTiempoEstimado;
            TareaSeleccionada.IdAreaInteres = DetalleIdAreaInteres;

            await _mongo.ActualizarTarea(TareaSeleccionada.IdTarea, TareaSeleccionada);
            DetalleMensaje = "Guardado";
            await CargarTareasAsync();
        }
        catch (Exception ex)
        {
            DetalleMensaje = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task CompletarTareaAsync(Tarea tarea)
    {
        if (tarea.IdTarea == null) return;

        await _mongo.CompletarTarea(tarea.IdTarea);
        if (TareaSeleccionada?.IdTarea == tarea.IdTarea)
            TareaSeleccionada = null;
        await CargarTareasAsync();
    }

    [RelayCommand]
    private async Task ToggleTareaAsync(Tarea tarea)
    {
        if (tarea.IdTarea == null) return;

        if (tarea.FecCompletado == null)
            await _mongo.CompletarTarea(tarea.IdTarea);
        else
            await _mongo.DescompletarTarea(tarea.IdTarea);

        if (TareaSeleccionada?.IdTarea == tarea.IdTarea)
            TareaSeleccionada = null;
        await CargarTareasAsync();
    }

    [RelayCommand]
    private async Task EliminarTareaAsync(Tarea tarea)
    {
        if (tarea.IdTarea == null) return;

        await _mongo.EliminarTarea(tarea.IdTarea);
        if (TareaSeleccionada?.IdTarea == tarea.IdTarea)
            TareaSeleccionada = null;
        await CargarTareasAsync();
    }

    [RelayCommand]
    private async Task QuickAddAsync()
    {
        if (string.IsNullOrWhiteSpace(QuickAddNombre)) return;
        if (_sesion.UsuarioActual == null) return;

        var tarea = new Tarea
        {
            Nombre = QuickAddNombre.Trim(),
            IdUsuario = _sesion.UsuarioActual.IdUsuario,
            Prioridad = 1,
            FecCreacion = DateTime.Now
        };

        await _mongo.CrearTarea(tarea);
        QuickAddNombre = string.Empty;
        await CargarTareasAsync();
    }
}