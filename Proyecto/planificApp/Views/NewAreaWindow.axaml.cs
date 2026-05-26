using Avalonia.Controls;
using Avalonia.Interactivity;
using planificApp.ViewModels;

namespace planificApp.Views;

public partial class NewAreaWindow : Window
{
    public bool Result { get; private set; }

    public NewAreaWindow()
    {
        InitializeComponent();
    }

    private async void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not NewAreaViewModel vm) return;

        if (CmbUbicacionPred.SelectedIndex <= 0)
            vm.UbicacionPred = null;
        else
            vm.UbicacionPred = CmbUbicacionPred.SelectedIndex switch
            {
                1 => "Casa", 2 => "Trabajo", 3 => "Universidad",
                4 => "Gimnasio", 5 => "Supermercado", 6 => "Otro",
                _ => null
            };

        vm.MetodoTransportePred = CmbTransporte.SelectedIndex switch
        {
            0 => null,
            1 => PlanificApp.Models.Enums.MetodoTransporte.Pie,
            2 => PlanificApp.Models.Enums.MetodoTransporte.Bicicleta,
            3 => PlanificApp.Models.Enums.MetodoTransporte.Automovil,
            4 => PlanificApp.Models.Enums.MetodoTransporte.TransportePublico,
            _ => null
        };

        vm.Prioridad = CmbPrioridadArea.SelectedIndex + 1;

        await vm.GuardarCommand.ExecuteAsync(null);

        if (vm.GuardadoExitoso)
        {
            Result = true;
            Close(true);
        }
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Result = false;
        Close(false);
    }
}