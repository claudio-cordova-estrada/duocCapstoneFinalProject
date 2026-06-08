using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using planificApp.Data;
using PlanificApp.Models;
using PlanificApp.Models.Repositories.Interfaces;
using planificApp.Services;
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
        var dialogService = App.Services.GetRequiredService<IDialogService>();
        var result = await dialogService.ShowNewAreaDialog();
        if (result && DataContext is MainViewModel vm)
        {
            await vm.ReloadAreasAsync();
        }
    }

    private async void EditarAreaMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem mi) return;
        if (mi.Parent is not ContextMenu cm || cm.DataContext is not AreaInteres area) return;

        var dialogService = App.Services.GetRequiredService<IDialogService>();
        var result = await dialogService.ShowEditAreaDialog(area);
        if (result && DataContext is MainViewModel vm)
        {
            await vm.ReloadAreasAsync();
        }
    }

    private async void EliminarAreaMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem mi) return;
        if (mi.Parent is not ContextMenu cm || cm.DataContext is not AreaInteres area) return;

        var dialogService = App.Services.GetRequiredService<IDialogService>();
        var confirm = await dialogService.ShowConfirmDeleteAreaDialog(area);
        if (!confirm) return;

        var areaRepo = App.Services.GetRequiredService<IAreaInteresRepository>();
        if (area.IdAreaInteres != null)
        {
            await areaRepo.EliminarAreaInteres(area.IdAreaInteres);
        }

        if (DataContext is MainViewModel vm)
        {
            await vm.ReloadAreasAsync();
            vm.GoToInboxCommand.Execute(null);
        }
    }
}