using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlanificApp.Models;
using PlanificApp.Models.Repositories.Interfaces;
using PlanificApp.Models.Services.Interfaces;
using planificApp.Helpers;

namespace planificApp.ViewModels;

public abstract partial class TareaDetailViewModelBase : PageViewModel
{
    private readonly ITareaRepository _tareaRepo;
    private readonly IAreaInteresRepository _areaRepo;
    private readonly ISesionService _sesion;

    protected TareaDetailViewModelBase(
        ITareaRepository tareaRepo,
        IAreaInteresRepository areaRepo,
        ISesionService sesion)
    {
        _tareaRepo = tareaRepo;
        _areaRepo = areaRepo;
        _sesion = sesion;
    }

    [ObservableProperty] private Tarea? _tareaSeleccionada;
    [ObservableProperty] private string _detalleNombre = string.Empty;
    [ObservableProperty] private string _detalleEstado = string.Empty;
    [ObservableProperty] private string _detalleMensaje = string.Empty;
    [ObservableProperty] private ObservableCollection<AreaInteres> _areasInteres = new();

    public DateTime? DetalleFecInicio { get; set; }
    public DateTime? DetalleFecLimite { get; set; }
    public TimeSpan? DetalleHoraInicio { get; set; }
    public TimeSpan? DetalleHoraFin { get; set; }
    public string? DetalleUbicacion { get; set; }
    public int DetallePrioridad { get; set; } = 1;
    public int DetalleTiempoEstimado { get; set; }
    public string? DetalleIdAreaInteres { get; set; }
    public string? DetalleTipoActividadFisica { get; set; }
    public string? DetalleTipoActividadMental { get; set; }

    protected ITareaRepository TareaRepo => _tareaRepo;
    protected IAreaInteresRepository AreaRepo => _areaRepo;
    protected ISesionService Sesion => _sesion;

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
        DetalleTipoActividadFisica = tarea.TipoActividadFisica;
        DetalleTipoActividadMental = tarea.TipoActividadMental;

        DetalleEstado = DetalleTareaHelper.CalcularEstado(tarea);
        DetalleMensaje = string.Empty;
    }

    [RelayCommand]
    private async Task GuardarDetalleAsync()
    {
        if (TareaSeleccionada == null || TareaSeleccionada.IdTarea == null) return;

        if (string.IsNullOrWhiteSpace(DetalleNombre))
        {
            DetalleNombre = TareaSeleccionada.Nombre;
            DetalleMensaje = "El nombre no puede estar vac\u00EDo.";
            return;
        }

        DetalleMensaje = string.Empty;

        var jornadaInicio = _sesion.UsuarioActual?.HoraInicioJornada ?? TimeSpan.FromHours(7.5);
        var jornadaFin = _sesion.UsuarioActual?.HoraFinJornada ?? TimeSpan.FromHours(22);

        if (DetalleHoraInicio.HasValue && DetalleHoraFin.HasValue)
        {
            if (DetalleHoraInicio < jornadaInicio || DetalleHoraFin > jornadaFin)
            {
                DetalleMensaje = $"Guardado (hora fuera de jornada {jornadaInicio:hh\\:mm}-{jornadaFin:hh\\:mm})";
            }
            else
            {
                DetalleMensaje = "Guardado";
            }
        }
        else if (DetalleHoraInicio.HasValue && DetalleHoraInicio < jornadaInicio)
        {
            DetalleMensaje = $"Guardado (hora inicio fuera de jornada {jornadaInicio:hh\\:mm}-{jornadaFin:hh\\:mm})";
        }
        else if (DetalleHoraFin.HasValue && DetalleHoraFin > jornadaFin)
        {
            DetalleMensaje = $"Guardado (hora fin fuera de jornada {jornadaInicio:hh\\:mm}-{jornadaFin:hh\\:mm})";
        }
        else
        {
            DetalleMensaje = "Guardado";
        }

        try
        {
            var oldAreaId = TareaSeleccionada.IdAreaInteres;

            TareaSeleccionada.Nombre = DetalleNombre;
            TareaSeleccionada.FecInicio = DetalleFecInicio;
            TareaSeleccionada.FecLimite = DetalleFecLimite;
            TareaSeleccionada.HoraInicio = DetalleHoraInicio;
            TareaSeleccionada.HoraFin = DetalleHoraFin;
            TareaSeleccionada.Prioridad = DetallePrioridad;
            TareaSeleccionada.Ubicacion = string.IsNullOrWhiteSpace(DetalleUbicacion) ? null : DetalleUbicacion;
            TareaSeleccionada.TiempoEstimado = DetalleTiempoEstimado;
            TareaSeleccionada.IdAreaInteres = DetalleIdAreaInteres;
            TareaSeleccionada.TipoActividadFisica = DetalleTipoActividadFisica;
            TareaSeleccionada.TipoActividadMental = DetalleTipoActividadMental;

            if (DetalleIdAreaInteres != oldAreaId)
            {
                var nuevaArea = AreasInteres.FirstOrDefault(a => a.IdAreaInteres == DetalleIdAreaInteres);
                if (nuevaArea != null)
                {
                    DetalleTareaHelper.AplicarDefaultsArea(TareaSeleccionada, nuevaArea);
                    DetalleUbicacion = TareaSeleccionada.Ubicacion;
                    DetalleTipoActividadFisica = TareaSeleccionada.TipoActividadFisica;
                    DetalleTipoActividadMental = TareaSeleccionada.TipoActividadMental;
                }
            }

            await _tareaRepo.ActualizarTarea(TareaSeleccionada.IdTarea, TareaSeleccionada);

            if (ShouldDeselectAfterSave(TareaSeleccionada))
            {
                TareaSeleccionada = null;
            }

            await CargarTareasAsync();
        }
        catch (Exception ex)
        {
            DetalleMensaje = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ToggleTareaAsync(Tarea tarea)
    {
        if (tarea.IdTarea == null) return;

        if (tarea.FecCompletado == null)
            await _tareaRepo.CompletarTarea(tarea.IdTarea);
        else
            await _tareaRepo.DescompletarTarea(tarea.IdTarea);

        if (TareaSeleccionada?.IdTarea == tarea.IdTarea)
            TareaSeleccionada = null;
        await CargarTareasAsync();
    }

    [RelayCommand]
    private async Task EliminarTareaAsync(Tarea tarea)
    {
        if (tarea.IdTarea == null) return;

        await _tareaRepo.EliminarTarea(tarea.IdTarea);
        if (TareaSeleccionada?.IdTarea == tarea.IdTarea)
            TareaSeleccionada = null;
        await CargarTareasAsync();
    }

    protected abstract bool ShouldDeselectAfterSave(Tarea tarea);
    public abstract Task CargarTareasAsync();
}