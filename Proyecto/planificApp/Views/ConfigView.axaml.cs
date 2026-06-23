using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using PlanificApp.Models.Services.Interfaces;
using planificApp.ViewModels;

namespace planificApp.Views;

public partial class ConfigView : UserControl
{
    private readonly IAppearanceService _appearance;

    public ConfigView()
    {
        InitializeComponent();
        _appearance = App.Services.GetRequiredService<IAppearanceService>();
        Loaded += ConfigView_Loaded;
        Unloaded += ConfigView_Unloaded;
    }

    private async void ConfigView_Loaded(object? sender, RoutedEventArgs e)
    {
        // Mientras la vista está abierta, escuchamos cambios de tema hechos desde otro lado
        // (ej. el botón del SB1) para mantener el toggle en sincronía.
        _appearance.ThemeChanged += OnAppearanceThemeChanged;

        if (DataContext is ConfigViewModel vm)
            await vm.CargarConfigAsync();
    }

    private void ConfigView_Unloaded(object? sender, RoutedEventArgs e)
    {
        _appearance.ThemeChanged -= OnAppearanceThemeChanged;
    }

    private void OnAppearanceThemeChanged()
    {
        if (DataContext is ConfigViewModel vm)
            vm.NotifyAppearanceChanged();
    }
}
