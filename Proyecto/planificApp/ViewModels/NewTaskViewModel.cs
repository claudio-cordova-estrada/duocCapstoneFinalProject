using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlanificApp.Models;
using PlanificApp.Models.Services.Interfaces;
using PlanificApp.Models.Repositories.Interfaces;

namespace planificApp.ViewModels;

public partial class NewTaskViewModel : ViewModelBase
{
    private readonly ITareaRepository _tareaRepo;
    private readonly IAreaInteresRepository _areaRepo;
    private readonly ISesionService _sesion;
    private readonly Task _initTask;

    [ObservableProperty] private string _nombre = string.Empty;
    [ObservableProperty] private DateTime? _fecInicio = null;
    [ObservableProperty] private DateTime? _fecLimite;
    [ObservableProperty] private TimeSpan? _horaInicio;
    [ObservableProperty] private TimeSpan? _horaFin;
    [ObservableProperty] private int _prioridad = 1;
    [ObservableProperty] private int _tiempoEstimado;
    [ObservableProperty] private string? _ubicacion;
    [ObservableProperty] private DateTime? _recordatorio;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _hasError;
    [ObservableProperty] private ObservableCollection<AreaInteres> _areasInteres = new();
    [ObservableProperty] private string? _tipoActividadFisica;
    [ObservableProperty] private string? _tipoActividadMental;

    public string? IdAreaInteres { get; set; }

    public bool GuardadoExitoso { get; private set; }

    public NewTaskViewModel(ITareaRepository tareaRepo, IAreaInteresRepository areaRepo, ISesionService sesion)
    {
        _tareaRepo = tareaRepo;
        _areaRepo = areaRepo;
        _sesion = sesion;
        _initTask = LoadAreasAsync();
    }

    private async Task LoadAreasAsync()
    {
        if (_sesion.UsuarioActual?.IdUsuario == null) return;
        var areas = await _areaRepo.ObtenerAreasPorUsuario(_sesion.UsuarioActual.IdUsuario);
        AreasInteres = new ObservableCollection<AreaInteres>(areas);
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Nombre))
        {
            ErrorMessage = "Ingresa un nombre para la tarea.";
            HasError = true;
            return;
        }

        try
        {
            var tarea = new Tarea
            {
                Nombre = Nombre,
                FecInicio = FecInicio,
                FecLimite = FecLimite,
                HoraInicio = HoraInicio,
                HoraFin = HoraFin,
                Prioridad = Prioridad,
                TiempoEstimado = TiempoEstimado,
                Ubicacion = string.IsNullOrWhiteSpace(Ubicacion) ? null : Ubicacion,
                Recordatorio = Recordatorio,
                IdAreaInteres = IdAreaInteres,
                IdUsuario = _sesion.UsuarioActual?.IdUsuario,
                FecCreacion = DateTime.Now,
                TipoActividadFisica = TipoActividadFisica,
                TipoActividadMental = TipoActividadMental,
            };

            if (IdAreaInteres != null)
            {
                var area = AreasInteres.FirstOrDefault(a => a.IdAreaInteres == IdAreaInteres);
                if (area != null)
                    Helpers.DetalleTareaHelper.AplicarDefaultsArea(tarea, area);
            }

            await _tareaRepo.CrearTarea(tarea);
            GuardadoExitoso = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al crear la tarea: {ex.Message}";
            HasError = true;
        }
    }
    
    public Task WaitForAreasAsync() => _initTask;
}
