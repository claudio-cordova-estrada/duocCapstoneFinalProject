using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using PlanificApp.Models;
using PlanificApp.Models.Repositories.Interfaces;
using PlanificApp.Models.Services.Interfaces;
using planificApp.Services;
using planificApp.ViewModels;

namespace planificApp;

public partial class MainView : Window
{
    public MainView()
    {
        InitializeComponent();

        var appearance = App.Services.GetRequiredService<IAppearanceService>();
        appearance.ThemeChanged += OnThemeChanged;
    }

    // Transición suave al cambiar tema: cubrimos con el color del tema ANTERIOR
    // y lo desvanecemos para revelar el nuevo (rampa de brillo, sin fogonazo).
    private async void OnThemeChanged()
    {
        var app = Avalonia.Application.Current;
        if (app == null) return;

        var variantePrevia = app.ActualThemeVariant == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark;
        if (app.TryGetResource("AppBackground", variantePrevia, out var res) && res is IBrush brushPrevio)
            ThemeFadeOverlay.Background = brushPrevio;

        ThemeFadeOverlay.Opacity = 1;
        ThemeFadeOverlay.IsVisible = true;

        const int steps = 15;
        const int durationMs = 250;
        for (int i = steps; i >= 0; i--)
        {
            ThemeFadeOverlay.Opacity = (double)i / steps;
            await Task.Delay(durationMs / steps);
        }

        ThemeFadeOverlay.IsVisible = false;
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