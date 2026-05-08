using Avalonia.Controls;
using planificApp.Views;

namespace planificApp.Helpers;

public static class DialogHelper
{
    public static async void ShowNewTaskDialog(Control parent)
    {
        var window = TopLevel.GetTopLevel(parent) as Window;
        if (window == null) return;

        var dialog = new NewTaskWindow();
        await dialog.ShowDialog(window);
    }

    public static async void ShowGenerarSemanaDialog(Control parent)
    {
        var window = TopLevel.GetTopLevel(parent) as Window;
        if (window == null) return;

        var dialog = new NewTaskWindow { Title = "Generar semana" };
        var result = await dialog.ShowDialog<bool>(window);

        if (result)
        {
            if (window.DataContext is ViewModels.MainViewModel vm)
            {
                vm.GoToPropuestasSemanales();
            }
        }
    }
}