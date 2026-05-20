using System.Threading.Tasks;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using PlanificApp.Models.Services;
using planificApp.ViewModels;
using planificApp.Views;

namespace planificApp.Helpers;

public static class DialogHelper
{
    public static async Task<bool> ShowNewTaskDialog(Control parent)
    {
        var window = TopLevel.GetTopLevel(parent) as Window;
        if (window == null) return false;

        var mongo = App.Services.GetRequiredService<MongoService>();
        var sesion = App.Services.GetRequiredService<SesionService>();

        var viewModel = new NewTaskViewModel(mongo, sesion);
        var dialog = new NewTaskWindow { DataContext = viewModel };
        await dialog.ShowDialog<bool>(window);
        return dialog.Result;
    }

    public static async void ShowGenerarSemanaDialog(Control parent)
    {
        var window = TopLevel.GetTopLevel(parent) as Window;
        if (window == null) return;

        var mongo = App.Services.GetRequiredService<MongoService>();
        var sesion = App.Services.GetRequiredService<SesionService>();

        var viewModel = new NewTaskViewModel(mongo, sesion);
        var dialog = new NewTaskWindow { DataContext = viewModel, Title = "Generar semana" };
        var result = await dialog.ShowDialog<bool>(window);

        if (result)
        {
            if (window.DataContext is ViewModels.MainViewModel vm)
            {
                vm.GoToPropuestasSemanales();
            }
        }
    }

    public static async void ShowAddLocationDialog(Control parent)
    {
        var window = TopLevel.GetTopLevel(parent) as Window;
        if (window == null) return;

        var dialog = new AddLocationWindow();
        await dialog.ShowDialog(window);
    }

    public static async void ShowEditLocationDialog(Control parent)
    {
        var window = TopLevel.GetTopLevel(parent) as Window;
        if (window == null) return;

        var dialog = new AddLocationWindow();
        dialog.SetEditMode("Casa", "Hogar", "#34d399", "Metro");
        await dialog.ShowDialog(window);
    }

    public static async void ShowConfirmDeleteLocationDialog(Control parent)
    {
        var window = TopLevel.GetTopLevel(parent) as Window;
        if (window == null) return;

        var dialog = new ConfirmDeleteLocationWindow();
        dialog.SetLocationName("Casa");
        await dialog.ShowDialog(window);
    }
}
