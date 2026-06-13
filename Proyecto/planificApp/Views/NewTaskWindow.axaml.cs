using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using planificApp.Helpers;
using planificApp.ViewModels;
using PlanificApp.Models;

namespace planificApp.Views;

public partial class NewTaskWindow : Window
{
    public bool Result { get; private set; }
    public string? PreSelectedAreaId { get; set; }

    public NewTaskWindow()
    {
        InitializeComponent();
        Loaded += NewTaskWindow_Loaded;
    }

    private async void NewTaskWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not NewTaskViewModel vm) return;

        await vm.WaitForAreasAsync();
        DetalleTareaHelper.PopulateAreaComboBox(CmbAreaInteres, vm.AreasInteres, PreSelectedAreaId);

        if (PreSelectedAreaId != null)
        {
            var area = vm.AreasInteres.FirstOrDefault(a => a.IdAreaInteres == PreSelectedAreaId);
            if (area != null)
            {
                ApplyAreaDefaults(area, vm);
            }
        }
    }

    private void ApplyAreaDefaults(AreaInteres area, NewTaskViewModel vm)
    {
        // Aplicamos los valores default directo al ViewModel (para que se reflejen en los ComboBox)
        if (area.UbicacionPred != null && vm.MisUbicaciones.Contains(area.UbicacionPred))
            vm.UbicacionDisplay = area.UbicacionPred;

        if (area.TipoActividadFisicaPred != null)
            vm.ActividadFisicaDisplay = area.TipoActividadFisicaPred;

        if (area.TipoActividadMentalPred != null)
            vm.ActividadMentalDisplay = area.TipoActividadMentalPred;
    }

    private async void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not NewTaskViewModel vm) return;

        // Lo único que seguimos seteando manualmente es el Área de Interés, porque usa un ComboBox de objetos
        vm.IdAreaInteres = DetalleTareaHelper.GetSelectedAreaId(CmbAreaInteres);

        // Disparamos el comando del ViewModel (todo lo demás ya está enlazado por Binding)
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

    private void ClearFecInicio_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is NewTaskViewModel vm) vm.FecInicio = null;
    }

    private void ClearFecLimite_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is NewTaskViewModel vm) vm.FecLimite = null;
    }

    private void ClearHoraInicio_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is NewTaskViewModel vm) vm.HoraInicio = null;
    }

    private void ClearHoraFin_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is NewTaskViewModel vm) vm.HoraFin = null;
    }
}