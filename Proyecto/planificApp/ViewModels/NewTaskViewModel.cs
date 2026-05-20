using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlanificApp.Models;
using PlanificApp.Models.Services;

namespace planificApp.ViewModels;

public partial class NewTaskViewModel : ViewModelBase
{
    private readonly MongoService _mongo;
    private readonly SesionService _sesion;

    [ObservableProperty] private string _nombre = string.Empty;
    [ObservableProperty] private DateTime? _fecInicio = DateTime.Now;
    [ObservableProperty] private DateTime? _fecLimite;
    [ObservableProperty] private TimeSpan? _horaInicio;
    [ObservableProperty] private TimeSpan? _horaFin;
    [ObservableProperty] private int _prioridad = 1;
    [ObservableProperty] private int _tiempoEstimado;
    [ObservableProperty] private string? _ubicacion;
    [ObservableProperty] private DateTime? _recordatorio;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _hasError;

    public bool GuardadoExitoso { get; private set; }

    public NewTaskViewModel(MongoService mongo, SesionService sesion)
    {
        _mongo = mongo;
        _sesion = sesion;
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
                IdUsuario = _sesion.UsuarioActual?.IdUsuario,
                FecCreacion = DateTime.Now,
            };

            await _mongo.CrearTarea(tarea);
            GuardadoExitoso = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al crear la tarea: {ex.Message}";
            HasError = true;
        }
    }
}
