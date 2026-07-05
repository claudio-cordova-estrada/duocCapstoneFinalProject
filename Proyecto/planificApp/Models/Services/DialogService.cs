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
using planificApp.ViewModels;
using planificApp.Views;

namespace planificApp.Services;

public class DialogService : IDialogService
{
    private Window? GetMainWindow() =>
        Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

    // Evita que un diálogo supere el tamaño de la pantalla. En equipos con pantallas chicas
    // las ventanas se salían de los bordes y no se podían cerrar; topamos ancho/alto al 95%
    // del área útil del monitor donde está la app.
    private void AjustarATamanoPantalla(Window dialog)
    {
        var owner = GetMainWindow();
        var screen = owner?.Screens.ScreenFromVisual(owner) ?? owner?.Screens.Primary;
        if (screen == null) return;

        var scaling = screen.Scaling <= 0 ? 1 : screen.Scaling;
        dialog.MaxWidth = screen.WorkingArea.Width / scaling * 0.95;
        dialog.MaxHeight = screen.WorkingArea.Height / scaling * 0.95;
    }

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
        AjustarATamanoPantalla(dialog);
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
        AjustarATamanoPantalla(dialog);
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
        AjustarATamanoPantalla(dialog);
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

    public async Task<bool> ShowConfirmDeleteUsuarioDialog(string nombreUsuario)
    {
        var window = GetMainWindow();
        if (window == null) return false;

        var dialog = new ConfirmDeleteUsuarioWindow();
        dialog.SetUsuarioName(nombreUsuario);
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
        AjustarATamanoPantalla(dialog);
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
        // Todo el cuerpo va en try/catch: es 'async void', así que cualquier excepción que escape
        // (incluida la navegación o la resolución de servicios) tiraría la app entera.
        try
        {
            var navigation = App.Services.GetRequiredService<INavigationService>();
            var vm = App.Services.GetRequiredService<PropuestasSemanalesViewModel>();
            vm.SetCondiciones(condiciones);

            // Navegamos PRIMERO para que la página de propuestas (con su overlay
            // "Generando propuestas...") quede visible mientras corre la generación,
            // que puede tardar varios segundos (cálculo de traslados con Mongo + Google).
            navigation.NavigateToPage(ApplicationPageNames.UserPropuestasSemanales);

            await vm.GenerarPropuestasCommand.ExecuteAsync(null);
        }
        catch (Exception ex)
        {
            App.LogCrash("ShowPropuestasSemanales", ex);
            System.Diagnostics.Debug.WriteLine($"[DIALOG] Error generando propuestas: {ex.Message}");
        }
    }

    public async Task<LocationFormData?> ShowAddLocationDialog(IGeoService geoService)
    {
        var window = GetMainWindow();
        if (window == null) return null;

        var dialog = new AddLocationWindow(geoService);
        AjustarATamanoPantalla(dialog);
        return await dialog.ShowDialog<LocationFormData>(window);
    }

    public async Task<LocationFormData?> ShowEditLocationDialog(IGeoService geoService, string nombre, string direccion, string area, string color, string transporte)
    {
        var window = GetMainWindow();
        if (window == null) return null;

        var dialog = new AddLocationWindow(geoService);
        AjustarATamanoPantalla(dialog);
        dialog.SetEditMode(nombre, direccion, area, color, transporte);
        return await dialog.ShowDialog<LocationFormData>(window);
    }

    public async Task ShowRouteCalculatorDialog(IGeoService geoService, ObservableCollection<UbicacionVisual> ubicaciones)
    {
        var window = GetMainWindow();
        if (window == null) return;

        var dialog = new CalcularRutaWindow(geoService, ubicaciones);
        AjustarATamanoPantalla(dialog);
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