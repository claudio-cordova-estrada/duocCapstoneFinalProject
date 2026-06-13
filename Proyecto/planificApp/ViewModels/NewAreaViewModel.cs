using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlanificApp.Models;
using PlanificApp.Models.Enums;
using PlanificApp.Models.Services.Interfaces;
using PlanificApp.Models.Repositories.Interfaces;

namespace planificApp.ViewModels;

public partial class NewAreaViewModel : ViewModelBase
{
    private readonly IAreaInteresRepository _areaRepo;
    private readonly ISesionService _sesion;
    private readonly IUbicacionRepository _ubicacionRepo;

    [ObservableProperty] private string _nombre = string.Empty;
    [ObservableProperty] private string _colorHex = "#a78bfa";
    [ObservableProperty] private string? _ubicacionPred;
    [ObservableProperty] private MetodoTransporte? _metodoTransportePred;
    [ObservableProperty] private string? _tipoActividadFisicaPred;
    [ObservableProperty] private string? _tipoActividadMentalPred;
    [ObservableProperty] private int _prioridad = 1;
    [ObservableProperty] private int _horasSemanales;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _hasError;
    [ObservableProperty] private string _titulo = "Nueva área de interés";

    // SOLUCIÓN CS1503: Usamos el modelo correcto 'UbicacionGuardada'
    public ObservableCollection<UbicacionGuardada> UbicacionesGuardadasLista { get; } = new();
    [ObservableProperty] private UbicacionGuardada? _ubicacionSeleccionada;

    public bool EsModoEdicion { get; private set; }
    public string? IdAreaEditando { get; private set; }
    public bool GuardadoExitoso { get; private set; }

    public NewAreaViewModel(IAreaInteresRepository areaRepo, ISesionService sesion, IUbicacionRepository ubicacionRepo)
    {
        _areaRepo = areaRepo;
        _sesion = sesion;
        _ubicacionRepo = ubicacionRepo;
    }

    public async Task CargarUbicacionesAsync()
    {
        // SOLUCIÓN CS8604: Blindaje para asegurar que el ID no sea nulo antes de buscar
        if (_sesion.UsuarioActual == null || string.IsNullOrEmpty(_sesion.UsuarioActual.IdUsuario))
            return;

        try
        {
            var ubicaciones = await _ubicacionRepo.ObtenerUbicacionesPorUsuario(_sesion.UsuarioActual.IdUsuario);

            UbicacionesGuardadasLista.Clear();
            foreach (var ubi in ubicaciones)
            {
                UbicacionesGuardadasLista.Add(ubi);

                // Preseleccionar si estamos editando
                if (EsModoEdicion && !string.IsNullOrEmpty(UbicacionPred))
                {
                    if (ubi.Nombre == UbicacionPred)
                    {
                        UbicacionSeleccionada = ubi;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al cargar ubicaciones: {ex.Message}");
        }
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
        TipoActividadFisicaPred = area.TipoActividadFisicaPred;
        TipoActividadMentalPred = area.TipoActividadMentalPred;
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

        // Blindaje extra al guardar
        if (_sesion.UsuarioActual == null || string.IsNullOrEmpty(_sesion.UsuarioActual.IdUsuario))
        {
            ErrorMessage = "Sesión no encontrada.";
            HasError = true;
            return;
        }

        try
        {
            string? ubicacionParaGuardar = UbicacionSeleccionada?.Nombre;

            var area = new AreaInteres
            {
                Nombre = Nombre,
                ColorHex = ColorHex,
                UbicacionPred = ubicacionParaGuardar,
                MetodoTransportePred = MetodoTransportePred,
                TipoActividadFisicaPred = TipoActividadFisicaPred,
                TipoActividadMentalPred = TipoActividadMentalPred,
                Prioridad = (PrioridadAreaInteres)Prioridad,
                HorasSemanales = HorasSemanales,
                IdUsuario = _sesion.UsuarioActual.IdUsuario
            };

            if (EsModoEdicion && IdAreaEditando != null)
            {
                area.IdAreaInteres = IdAreaEditando;
                await _areaRepo.ActualizarAreaInteres(IdAreaEditando, area);
            }
            else
            {
                await _areaRepo.CrearAreaInteres(area);
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