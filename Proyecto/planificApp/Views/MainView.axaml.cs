using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using planificApp.Helpers;
using PlanificApp.Models;
using PlanificApp.Models.Services;
using Microsoft.Extensions.DependencyInjection;
using planificApp.ViewModels;

namespace planificApp;

public partial class MainView : Window
{
    public MainView()
    {
        InitializeComponent();
    }

    private async void NewAreaInteresButton_Click(object? sender, RoutedEventArgs e)
    {
        var result = await DialogHelper.ShowNewAreaDialog(this);
        if (result && DataContext is MainViewModel vm)
        {
            await vm.ReloadAreasAsync();
        }
    }

    private async void EditarAreaMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem mi) return;
        if (mi.Parent is not ContextMenu cm || cm.DataContext is not AreaInteres area) return;

        var result = await DialogHelper.ShowEditAreaDialog(this, area);
        if (result && DataContext is MainViewModel vm)
        {
            await vm.ReloadAreasAsync();
        }
    }

    private async void EliminarAreaMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem mi) return;
        if (mi.Parent is not ContextMenu cm || cm.DataContext is not AreaInteres area) return;

        var confirm = await DialogHelper.ShowConfirmDeleteAreaDialog(this, area);
        if (!confirm) return;

        var mongo = App.Services.GetRequiredService<MongoService>();
        if (area.IdAreaInteres != null)
        {
            await mongo.EliminarAreaInteres(area.IdAreaInteres);
        }

        if (DataContext is MainViewModel vm)
        {
            await vm.ReloadAreasAsync();
            vm.GoToInboxCommand.Execute(null);
        }
    }
}