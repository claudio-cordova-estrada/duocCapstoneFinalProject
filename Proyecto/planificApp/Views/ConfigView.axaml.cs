using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using planificApp.ViewModels;

namespace planificApp.Views;

public partial class ConfigView : UserControl
{
    private readonly Dictionary<Button, string> _dayButtons = new();

    public ConfigView()
    {
        InitializeComponent();
        Loaded += ConfigView_Loaded;

        _dayButtons[BtnLunes] = "Lunes";
        _dayButtons[BtnMartes] = "Martes";
        _dayButtons[BtnMiercoles] = "Miercoles";
        _dayButtons[BtnJueves] = "Jueves";
        _dayButtons[BtnViernes] = "Viernes";
        _dayButtons[BtnSabado] = "Sabado";
        _dayButtons[BtnDomingo] = "Domingo";
    }

    private async void ConfigView_Loaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ConfigViewModel vm)
        {
            await vm.CargarConfigAsync();
            SyncDayButtons(vm);
        }
    }

    private void ToggleDia_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || DataContext is not ConfigViewModel vm) return;
        if (!_dayButtons.TryGetValue(btn, out var prop)) return;

        var currentValue = (bool?)vm.GetType().GetProperty(prop)?.GetValue(vm);
        vm.GetType().GetProperty(prop)?.SetValue(vm, !currentValue);

        if (btn.Classes.Contains("active"))
            btn.Classes.Remove("active");
        else
            btn.Classes.Add("active");
    }

    private void SyncDayButtons(ConfigViewModel vm)
    {
        foreach (var kvp in _dayButtons)
        {
            var isActive = (bool?)vm.GetType().GetProperty(kvp.Value)?.GetValue(vm) ?? false;
            if (isActive)
                kvp.Key.Classes.Add("active");
            else
                kvp.Key.Classes.Remove("active");
        }
    }
}