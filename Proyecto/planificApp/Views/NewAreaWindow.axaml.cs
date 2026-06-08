using Avalonia.Controls;
using Avalonia.Interactivity;
using PlanificApp.Models;
using PlanificApp.Models.Enums;
using planificApp.Helpers;
using planificApp.ViewModels;

namespace planificApp.Views;

public partial class NewAreaWindow : Window
{
    public bool Result { get; private set; }

    public NewAreaWindow()
    {
        InitializeComponent();
    }

    public void SetEditMode(AreaInteres? area)
    {
        if (area == null || DataContext is not NewAreaViewModel vm) return;

        vm.CargarParaEdicion(area);

        CmbPrioridadArea.SelectedIndex = area.Prioridad switch
        {
            PrioridadAreaInteres.Baja => 0,
            PrioridadAreaInteres.Media => 1,
            PrioridadAreaInteres.Alta => 2,
            _ => 0
        };

        CmbUbicacionPred.SelectedIndex = area.UbicacionPred switch
        {
            "Casa" => 1,
            "Trabajo" => 2,
            "Universidad" => 3,
            "Gimnasio" => 4,
            "Supermercado" => 5,
            "Otro" => 6,
            _ => 0
        };

        CmbTransporte.SelectedIndex = area.MetodoTransportePred switch
        {
            MetodoTransporte.Pie => 1,
            MetodoTransporte.Bicicleta => 2,
            MetodoTransporte.Automovil => 3,
            MetodoTransporte.TransportePublico => 4,
            _ => 0
        };

        CmbTipoActividadFisicaPred.SelectedIndex = DetalleTareaHelper.TipoActividadFisicaToIndex(area.TipoActividadFisicaPred);
        CmbTipoActividadMentalPred.SelectedIndex = DetalleTareaHelper.TipoActividadMentalToIndex(area.TipoActividadMentalPred);
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
            1 => MetodoTransporte.Pie,
            2 => MetodoTransporte.Bicicleta,
            3 => MetodoTransporte.Automovil,
            4 => MetodoTransporte.TransportePublico,
            _ => null
        };

        vm.TipoActividadFisicaPred = DetalleTareaHelper.TipoActividadFisicaFromIndex(CmbTipoActividadFisicaPred.SelectedIndex);
        vm.TipoActividadMentalPred = DetalleTareaHelper.TipoActividadMentalFromIndex(CmbTipoActividadMentalPred.SelectedIndex);
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