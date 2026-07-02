using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using PlanificApp.Models;
using PlanificApp.Models.Services.Interfaces;
using PlanificApp.Models.Repositories.Interfaces;
using planificApp.Data;
using planificApp.Services;
using planificApp.Views;

namespace planificApp.ViewModels;

public partial class PropuestasSemanalesViewModel : PageViewModel
{
    private readonly IGeneradorSemanalService _generadorService;
    private readonly ITareaRepository _tareaRepo;
    private readonly IAreaInteresRepository _areaRepo;
    private readonly IUbicacionRepository _ubicacionRepo;
    private readonly ISesionService _sesionService;
    private readonly ICalendarioSemanalService _calendarioService;
    private readonly IDialogService _dialogService;

    [ObservableProperty] private ObservableCollection<PropuestaGeneracion> _propuestas = new();
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _mensajeError = string.Empty;

    private CondicionesGeneracion? _condiciones;

    public PropuestaGeneracion? PropuestaSeleccionada { get; private set; }

    public PropuestasSemanalesViewModel(
        IGeneradorSemanalService generadorService,
        ITareaRepository tareaRepo,
        IAreaInteresRepository areaRepo,
        IUbicacionRepository ubicacionRepo,
        ISesionService sesionService,
        ICalendarioSemanalService calendarioService,
        IDialogService dialogService)
    {
        PageName = ApplicationPageNames.UserPropuestasSemanales;

        _generadorService = generadorService;
        _tareaRepo = tareaRepo;
        _areaRepo = areaRepo;
        _ubicacionRepo = ubicacionRepo;
        _sesionService = sesionService;
        _calendarioService = calendarioService;
        _dialogService = dialogService;
    }

    public void SetCondiciones(CondicionesGeneracion condiciones)
    {
        _condiciones = condiciones;
        Propuestas = new ObservableCollection<PropuestaGeneracion>();
        MensajeError = string.Empty;
        PropuestaSeleccionada = null;
        // Mostramos el overlay de carga de inmediato, antes de navegar a la página,
        // para que el usuario vea feedback mientras se generan las propuestas.
        IsLoading = true;
    }

    [RelayCommand]
    private async Task GenerarPropuestasAsync()
    {
        if (_condiciones == null) return;

        IsLoading = true;
        MensajeError = string.Empty;

        try
        {
            var propuestas = await _generadorService.GenerarPropuestasAsync(_condiciones);

            var usuarioId = _sesionService.UsuarioActual?.IdUsuario;
            if (!string.IsNullOrEmpty(usuarioId))
            {
                foreach (var propuesta in propuestas)
                {
                    if (!propuesta.EsValida) continue;
                    foreach (var dia in propuesta.Semana.Dias)
                    {
                        try
                        {
                            await _calendarioService.CalcularTrasladosAsync(dia, usuarioId);
                        }
                        catch (Exception trasladoEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"[PROPUESTA] Error calculando traslados para {dia.NombreDia}: {trasladoEx.Message}");
                        }
                    }
                }
            }

            Propuestas = new ObservableCollection<PropuestaGeneracion>(propuestas);
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al generar propuestas: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SeleccionarPropuestaAsync(PropuestaGeneracion propuesta)
    {
        if (propuesta == null || !propuesta.EsValida) return;

        PropuestaSeleccionada = propuesta;

        var usuarioId = _sesionService.UsuarioActual?.IdUsuario;
        if (!string.IsNullOrEmpty(usuarioId))
        {
            foreach (var dia in propuesta.Semana.Dias)
            {
                await _calendarioService.CalcularTrasladosAsync(dia, usuarioId);
            }
            await _calendarioService.GuardarCambiosAsync(propuesta.Semana, usuarioId);
        }

        var navigation = App.Services.GetRequiredService<INavigationService>();
        navigation.NavigateToPage(ApplicationPageNames.UserCalendarioSemanal);
    }

    [RelayCommand]
    private async Task PrevisualizarAsync(PropuestaGeneracion propuesta)
    {
        if (propuesta == null || !propuesta.EsValida) return;
        PropuestaSeleccionada = propuesta;

        var usuarioId = _sesionService.UsuarioActual?.IdUsuario;
        if (!string.IsNullOrEmpty(usuarioId))
        {
            foreach (var dia in propuesta.Semana.Dias)
            {
                try
                {
                    await _calendarioService.CalcularTrasladosAsync(dia, usuarioId);
                }
                catch { }
            }
        }

        var window = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow : null;
        if (window == null) return;

        var preview = new PropuestaPreviewWindow();
        preview.SetPropuesta(propuesta);
        await preview.ShowDialog(window);
    }

    public SemanaCalendario? ObtenerSemanaSeleccionada()
    {
        return PropuestaSeleccionada?.Semana;
    }
}