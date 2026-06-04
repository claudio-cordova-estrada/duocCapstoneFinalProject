using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlanificApp.Models;
using PlanificApp.Models.Enums;
using PlanificApp.Models.Services;

namespace planificApp.ViewModels;

public partial class NewAreaViewModel : ViewModelBase
{
    private readonly MongoService _mongo;
    private readonly SesionService _sesion;

    [ObservableProperty] private string _nombre = string.Empty;
    [ObservableProperty] private string _colorHex = "#a78bfa";
    [ObservableProperty] private string? _ubicacionPred;
    [ObservableProperty] private MetodoTransporte? _metodoTransportePred;
    [ObservableProperty] private int _prioridad = 1;
    [ObservableProperty] private int _horasSemanales;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _hasError;
    [ObservableProperty] private string _titulo = "Nueva área de interés";

    public bool EsModoEdicion { get; private set; }
    public string? IdAreaEditando { get; private set; }
    public bool GuardadoExitoso { get; private set; }

    public NewAreaViewModel(MongoService mongo, SesionService sesion)
    {
        _mongo = mongo;
        _sesion = sesion;
    }

    public void CargarParaEdicion(AreaInteres area)
    {
        EsModoEdicion = true;
        IdAreaEditando = area.IdAreaInteres;
        Titulo = "Editar área de interés";
        Nombre = area.Nombre;
        ColorHex = area.ColorHex;
        UbicacionPred = area.UbicacionPred;
        MetodoTransportePred = area.MetodoTransportePred;
        Prioridad = (int)area.Prioridad;
        HorasSemanales = area.HorasSemanales;
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Nombre))
        {
            ErrorMessage = "Ingresa un nombre para el área.";
            HasError = true;
            return;
        }

        if (_sesion.UsuarioActual == null)
        {
            ErrorMessage = "Sesión no encontrada.";
            HasError = true;
            return;
        }

        try
        {
            var area = new AreaInteres
            {
                Nombre = Nombre,
                ColorHex = ColorHex,
                UbicacionPred = UbicacionPred,
                MetodoTransportePred = MetodoTransportePred,
                Prioridad = (PrioridadAreaInteres)Prioridad,
                HorasSemanales = HorasSemanales,
                IdUsuario = _sesion.UsuarioActual.IdUsuario
            };

            if (EsModoEdicion && IdAreaEditando != null)
            {
                area.IdAreaInteres = IdAreaEditando;
                await _mongo.ActualizarAreaInteres(IdAreaEditando, area);
            }
            else
            {
                await _mongo.CrearAreaInteres(area);
            }

            GuardadoExitoso = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = EsModoEdicion
                ? $"Error al actualizar el área: {ex.Message}"
                : $"Error al crear el área: {ex.Message}";
            HasError = true;
        }
    }
}