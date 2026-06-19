using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using PlanificApp.Models;
using PlanificApp.Models.Enums;
using planificApp.Helpers;
using planificApp.ViewModels;

namespace planificApp.Views;

public partial class NewAreaWindow : Window
{
    public bool Result { get; private set; }
    private string _selectedColor = "#a78bfa";

    public NewAreaWindow()
    {
        InitializeComponent();

        this.DataContextChanged += async (sender, e) =>
        {
            if (DataContext is NewAreaViewModel vm)
            {
                await vm.CargarUbicacionesAsync();
                SelectColor(vm.ColorHex);
            }
        };
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

        CmbTransporte.SelectedIndex = area.MetodoTransportePred switch
        {
            MetodoTransporte.Caminar => 1,
            MetodoTransporte.Auto => 2,
            MetodoTransporte.Bus => 3,
            _ => 0
        };

        CmbTipoActividadFisicaPred.SelectedIndex = DetalleTareaHelper.TipoActividadFisicaToIndex(area.TipoActividadFisicaPred);
        CmbTipoActividadMentalPred.SelectedIndex = DetalleTareaHelper.TipoActividadMentalToIndex(area.TipoActividadMentalPred);

        _selectedColor = area.ColorHex;
        SelectColor(_selectedColor);
    }

    private void ColorOption_Tapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border b && b.Tag is string color)
        {
            _selectedColor = color;
            if (DataContext is NewAreaViewModel vm)
                vm.ColorHex = color;
            SelectColor(color);
        }
    }

    private void SelectColor(string color)
    {
        if (this.FindControl<StackPanel>("ColorGrid") is not StackPanel colorGrid) return;

        foreach (var child in colorGrid.Children)
        {
            if (child is Border b)
            {
                if (b.Tag?.ToString() == color)
                {
                    b.BorderThickness = new Thickness(2);
                    b.BorderBrush = new SolidColorBrush(Colors.White);
                    b.Width = 26;
                    b.Height = 26;
                }
                else
                {
                    b.BorderThickness = new Thickness(0);
                    b.Width = 22;
                    b.Height = 22;
                }
            }
        }
    }

    private async void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not NewAreaViewModel vm) return;

        vm.MetodoTransportePred = CmbTransporte.SelectedIndex switch
        {
            0 => null,
            1 => MetodoTransporte.Caminar,
            2 => MetodoTransporte.Auto,
            3 => MetodoTransporte.Bus,
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