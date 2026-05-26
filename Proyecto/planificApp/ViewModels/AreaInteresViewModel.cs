using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using planificApp.Data;
using PlanificApp.Models;
using PlanificApp.Models.Services;

namespace planificApp.ViewModels;

public partial class AreaInteresViewModel : PageViewModel
{
    private readonly MongoService _mongo;
    private readonly SesionService _sesion;

    public AreaInteresViewModel(MongoService mongo, SesionService sesion)
    {
        _mongo = mongo;
        _sesion = sesion;
        PageName = ApplicationPageNames.UserAreaInteres;
    }

    [ObservableProperty] private AreaInteres? _areaSeleccionada;
    [ObservableProperty] private string _tituloVista = string.Empty;
    [ObservableProperty] private string _subtituloVista = string.Empty;
    [ObservableProperty] private ObservableCollection<Tarea> _tareasPendientesFiltradas = new();
    [ObservableProperty] private ObservableCollection<Tarea> _tareasCompletadasFiltradas = new();
    [ObservableProperty] private int _tareasPendientes;
    [ObservableProperty] private int _tareasCompletadasCount;
    [ObservableProperty] private int _tareasVencidasCount;
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

    public async Task CargarTareasAsync()
    {
        if (_sesion.UsuarioActual == null || AreaSeleccionada?.IdAreaInteres == null) return;
        
        var idUsuario = _sesion.UsuarioActual.IdUsuario!;

        AreasInteres = new ObservableCollection<AreaInteres>(
            await _mongo.ObtenerAreasPorUsuario(idUsuario));

        var tareas = await _mongo.ObtenerTareasPorArea(AreaSeleccionada.IdAreaInteres);

        var pendientes = tareas.Where(t => t.FecCompletado == null).ToList();
        var completadas = tareas.Where(t => t.FecCompletado != null).ToList();
        // me di cuenta de que no se estaba tomando en cuenta el caso donde no existiera fecha limite
        // por tanto lo agregue como segunda condición
        var vencidas = pendientes.Count(t => (t.FecLimite != null && t.FecLimite < DateTime.Now) || 
                                             (t.FecLimite == null && t.FecInicio != null && t.FecInicio < DateTime.Now));

        TareasPendientesFiltradas = new ObservableCollection<Tarea>(pendientes);
        TareasCompletadasFiltradas = new ObservableCollection<Tarea>(completadas);
        
        // Invertí la lógica de la variable de vencidas para tener solamente las tareas activas o pendientes
        // y no vencidas. Soy un genio
        TareasPendientes = pendientes.Count(t => !((t.FecLimite != null && t.FecLimite < DateTime.Now) ||
                                                  (t.FecLimite == null && t.FecInicio != null &&
                                                   t.FecInicio < DateTime.Now)));
        TareasCompletadasCount = completadas.Count;
        TareasVencidasCount = vencidas;
        HayCompletadas = completadas.Count > 0;

        SubtituloVista = $"{TareasPendientes} tareas activas";
    }

    public void SetArea(AreaInteres area)
    {
        AreaSeleccionada = area;
        TituloVista = area.Nombre ?? "Área";
        SubtituloVista = "Cargando...";
        _ = CargarTareasAsync();
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

        DetalleEstado = Helpers.DetalleTareaHelper.CalcularEstado(tarea);
        DetalleMensaje = string.Empty;
    }

    [RelayCommand]
    private void ToggleCompletadas()
    {
        CompletadasVisibles = !CompletadasVisibles;
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
    private async Task QuickAddAsync()
    {
        if (string.IsNullOrWhiteSpace(QuickAddNombre)) return;
        if (_sesion.UsuarioActual == null) return;
        if (AreaSeleccionada?.IdAreaInteres == null) return;

        var tarea = new Tarea
        {
            Nombre = QuickAddNombre.Trim(),
            IdUsuario = _sesion.UsuarioActual.IdUsuario,
            IdAreaInteres = AreaSeleccionada.IdAreaInteres,
            Prioridad = 1,
            FecCreacion = DateTime.Now
        };

        await _mongo.CrearTarea(tarea);
        QuickAddNombre = string.Empty;
        await CargarTareasAsync();
    }
}