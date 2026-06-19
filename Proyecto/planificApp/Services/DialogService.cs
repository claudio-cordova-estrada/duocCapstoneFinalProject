using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.DependencyInjection;
using planificApp.Data;
using PlanificApp.Models;
using PlanificApp.Models.Services.Interfaces;
using PlanificApp.Models.Repositories.Interfaces;
using planificApp.Services;
using planificApp.ViewModels;
using planificApp.Views;

namespace planificApp.Services;

public class DialogService : IDialogService
{
    private Window? GetMainWindow() =>
        Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

    public async Task<bool> ShowNewTaskDialog(string? preSelectedArea = null)
    {
        var window = GetMainWindow();
        if (window == null) return false;

        var tareaRepo = App.Services.GetRequiredService<ITareaRepository>();
        var areaRepo = App.Services.GetRequiredService<IAreaInteresRepository>();
        var sesion = App.Services.GetRequiredService<ISesionService>();
        var ubicacionRepo = App.Services.GetRequiredService<IUbicacionRepository>();

        var viewModel = new NewTaskViewModel(tareaRepo, areaRepo, sesion, ubicacionRepo);
        var dialog = new NewTaskWindow { DataContext = viewModel, PreSelectedAreaId = preSelectedArea };
        await dialog.ShowDialog<bool>(window);
        return dialog.Result;
    }

    public async Task<bool> ShowNewAreaDialog()
    {
        var window = GetMainWindow();
        if (window == null) return false;

        var areaRepo = App.Services.GetRequiredService<IAreaInteresRepository>();
        var sesion = App.Services.GetRequiredService<ISesionService>();
        var ubicacionRepo = App.Services.GetRequiredService<IUbicacionRepository>();

        var viewModel = new NewAreaViewModel(areaRepo, sesion, ubicacionRepo);
        var dialog = new NewAreaWindow { DataContext = viewModel };
        await dialog.ShowDialog<bool>(window);
        return dialog.Result;
    }

    public async Task<bool> ShowEditAreaDialog(AreaInteres area)
    {
        var window = GetMainWindow();
        if (window == null) return false;

        var areaRepo = App.Services.GetRequiredService<IAreaInteresRepository>();
        var sesion = App.Services.GetRequiredService<ISesionService>();
        var ubicacionRepo = App.Services.GetRequiredService<IUbicacionRepository>();

        var viewModel = new NewAreaViewModel(areaRepo, sesion, ubicacionRepo);
        var dialog = new NewAreaWindow { DataContext = viewModel };
        dialog.SetEditMode(area);
        await dialog.ShowDialog<bool>(window);
        return dialog.Result;
    }

    public async Task<bool> ShowConfirmDeleteAreaDialog(AreaInteres area)
    {
        var window = GetMainWindow();
        if (window == null) return false;

        var dialog = new ConfirmDeleteAreaWindow();
        dialog.SetAreaName(area.Nombre ?? "esta área");
        return await dialog.ShowDialog<bool>(window);
    }

    public async Task<CondicionesGeneracion?> ShowCondicionesGeneracionDialog(DateTime fechaLunes)
    {
        var window = GetMainWindow();
        if (window == null) return null;

        var areaRepo = App.Services.GetRequiredService<IAreaInteresRepository>();
        var tareaRepo = App.Services.GetRequiredService<ITareaRepository>();
        var ubicacionRepo = App.Services.GetRequiredService<IUbicacionRepository>();
        var sesion = App.Services.GetRequiredService<ISesionService>();
        var calendarioService = App.Services.GetRequiredService<ICalendarioSemanalService>();

        var viewModel = new CondicionesGeneracionViewModel(areaRepo, tareaRepo, ubicacionRepo, sesion, calendarioService);
        var dialog = new CondicionesGeneracionWindow { DataContext = viewModel };
        dialog.SetFechaLunes(fechaLunes);

        var result = await dialog.ShowDialog<bool>(window);

        if (result && dialog.ResultadoConfirmado && dialog.Condiciones != null)
        {
            return dialog.Condiciones;
        }

        return null;
    }

    public async void ShowPropuestasSemanales(CondicionesGeneracion condiciones)
    {
        var navigation = App.Services.GetRequiredService<INavigationService>();
        var vm = App.Services.GetRequiredService<PropuestasSemanalesViewModel>();
        vm.SetCondiciones(condiciones);

        // Navegamos PRIMERO para que la página de propuestas (con su overlay
        // "Generando propuestas...") quede visible mientras corre la generación,
        // que puede tardar varios segundos (cálculo de traslados con Mongo + Google).
        navigation.NavigateToPage(ApplicationPageNames.UserPropuestasSemanales);

        try
        {
            await vm.GenerarPropuestasCommand.ExecuteAsync(null);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DIALOG] Error generando propuestas: {ex.Message}");
        }
    }

    public async Task<LocationFormData?> ShowAddLocationDialog(IGeoService geoService, ObservableCollection<string> areas)
    {
        var window = GetMainWindow();
        if (window == null) return null;

        var dialog = new AddLocationWindow(geoService);
        dialog.SetAreasDeInteres(areas);
        return await dialog.ShowDialog<LocationFormData>(window);
    }

    public async Task<LocationFormData?> ShowEditLocationDialog(IGeoService geoService, ObservableCollection<string> areas, string nombre, string direccion, string area, string color, string transporte)
    {
        var window = GetMainWindow();
        if (window == null) return null;

        var dialog = new AddLocationWindow(geoService);
        dialog.SetAreasDeInteres(areas);
        dialog.SetEditMode(nombre, direccion, area, color, transporte);
        return await dialog.ShowDialog<LocationFormData>(window);
    }

    public async Task ShowRouteCalculatorDialog(IGeoService geoService, ObservableCollection<UbicacionVisual> ubicaciones)
    {
        var window = GetMainWindow();
        if (window == null) return;

        var dialog = new CalcularRutaWindow(geoService, ubicaciones);
        await dialog.ShowDialog(window);
    }

    // Devuelto a 'async void' para respetar tu IDialogService
    public async void ShowConfirmDeleteLocationDialog(string nombre)
    {
        var window = GetMainWindow();
        if (window == null) return;

        var dialog = new ConfirmDeleteLocationWindow();
        dialog.SetLocationName(nombre);
        await dialog.ShowDialog(window);
    }
}