using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlanificApp.Models;
using PlanificApp.Models.Repositories.Interfaces;
using PlanificApp.Models.Services.Interfaces;

namespace planificApp.ViewModels;

public partial class NewTaskViewModel : ViewModelBase
{
    private readonly ITareaRepository _tareaRepo;
    private readonly IAreaInteresRepository _areaRepo;
    private readonly ISesionService _sesion;
    private readonly IUbicacionRepository _ubicacionRepo;

    [ObservableProperty] private string _nombre = string.Empty;
    [ObservableProperty] private DateTime? _fecInicio;
    [ObservableProperty] private DateTime? _fecLimite;
    [ObservableProperty] private TimeSpan? _horaInicio;
    [ObservableProperty] private TimeSpan? _horaFin;
    [ObservableProperty] private DateTime? _recordatorio;

    // --- PROPIEDADES DINÁMICAS (Las mismas del Inbox) ---
    [ObservableProperty] private string _prioridadDisplay = "1 - Baja";
    [ObservableProperty] private string _tiempoDisplay = "— Sin definir —";
    [ObservableProperty] private string _actividadFisicaDisplay = "— Ninguna —";
    [ObservableProperty] private string _actividadMentalDisplay = "— Ninguna —";
    [ObservableProperty] private string _ubicacionDisplay = "— Sin ubicación —";
    [ObservableProperty] private string? _idAreaInteres;

    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _hasError;
    [ObservableProperty] private bool _guardadoExitoso;

    public ObservableCollection<AreaInteres> AreasInteres { get; } = new();
    public ObservableCollection<string> MisUbicaciones { get; } = new() { "— Sin ubicación —" };
    public ObservableCollection<string> Prioridades { get; } = new() { "1 - Baja", "2 - Media", "3 - Alta", "4 - Urgente", "5 - Fija" };
    public ObservableCollection<string> Tiempos { get; } = new() { "— Sin definir —", "5 minutos", "10 minutos", "15 minutos", "30 minutos", "45 minutos", "1 hora", "1.5 horas", "2 horas", "3 horas", "4 horas" };
    public ObservableCollection<string> NivelesActividadFisica { get; } = new() { "— Ninguna —", "Activa", "Pasiva" };
    public ObservableCollection<string> NivelesActividadMental { get; } = new() { "— Ninguna —", "Obligación", "Recreación" };

    public NewTaskViewModel(ITareaRepository tareaRepo, IAreaInteresRepository areaRepo, ISesionService sesion, IUbicacionRepository ubicacionRepo)
    {
        _tareaRepo = tareaRepo;
        _areaRepo = areaRepo;
        _sesion = sesion;
        _ubicacionRepo = ubicacionRepo;
    }

    public async Task WaitForAreasAsync()
    {
        if (_sesion.UsuarioActual?.IdUsuario == null) return;
        var areas = await _areaRepo.ObtenerAreasPorUsuario(_sesion.UsuarioActual.IdUsuario);
        AreasInteres.Clear();
        foreach (var area in areas) AreasInteres.Add(area);

        // Cargar las ubicaciones dinámicamente de la BD
        var ubicacionesDb = await _ubicacionRepo.ObtenerUbicacionesPorUsuario(_sesion.UsuarioActual.IdUsuario);
        MisUbicaciones.Clear();
        MisUbicaciones.Add("— Sin ubicación —");
        foreach (var ubi in ubicacionesDb)
        {
            if (!string.IsNullOrWhiteSpace(ubi.Nombre))
                MisUbicaciones.Add(ubi.Nombre);
        }
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Nombre))
        {
            ErrorMessage = "El nombre de la tarea es obligatorio.";
            HasError = true;
            return;
        }

        if (_sesion.UsuarioActual?.IdUsuario == null)
        {
            ErrorMessage = "No hay una sesión activa.";
            HasError = true;
            return;
        }

        // Convertir los textos del UI (Display) a valores de Base de Datos
        int prioridadInt = int.Parse(PrioridadDisplay.Split(' ')[0]);
        int tiempoInt = TiempoDisplay switch
        {
            "5 minutos" => 5,
            "10 minutos" => 10,
            "15 minutos" => 15,
            "30 minutos" => 30,
            "45 minutos" => 45,
            "1 hora" => 60,
            "1.5 horas" => 90,
            "2 horas" => 120,
            "3 horas" => 180,
            "4 horas" => 240,
            _ => 0
        };

        var nuevaTarea = new Tarea
        {
            Nombre = Nombre.Trim(),
            IdUsuario = _sesion.UsuarioActual.IdUsuario,
            FecInicio = FecInicio,
            FecLimite = FecLimite,
            HoraInicio = HoraInicio,
            HoraFin = HoraFin,
            Prioridad = prioridadInt,
            TiempoEstimado = tiempoInt,
            Ubicacion = UbicacionDisplay == "— Sin ubicación —" ? null : UbicacionDisplay,
            IdAreaInteres = IdAreaInteres,
            TipoActividadFisica = ActividadFisicaDisplay == "— Ninguna —" ? null : ActividadFisicaDisplay,
            TipoActividadMental = ActividadMentalDisplay == "— Ninguna —" ? null : ActividadMentalDisplay,
            Recordatorio = Recordatorio,
            FecCreacion = DateTime.Now
        };

        try
        {
            await _tareaRepo.CrearTarea(nuevaTarea);
            GuardadoExitoso = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al guardar: {ex.Message}";
            HasError = true;
        }
    }
}