using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using planificApp.Helpers;
using planificApp.ViewModels;

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
    }

    private async void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not NewTaskViewModel vm) return;

        // Transferir valores de controles al ViewModel
        vm.FecInicio = PickerFecInicio.SelectedDate;
        vm.FecLimite = PickerFecLimite.SelectedDate;
        vm.HoraInicio = PickerHoraInicio.SelectedTime;
        vm.HoraFin = PickerHoraFin.SelectedTime;
        vm.IdAreaInteres = DetalleTareaHelper.GetSelectedAreaId(CmbAreaInteres);
    
        // Ubicacion - mapear el índice del ComboBox
        vm.Ubicacion = CmbUbicacion.SelectedIndex switch
        {
            1 => "Casa",
            2 => "Trabajo",
            3 => "Universidad",
            4 => "Gimnasio",
            5 => "Supermercado",
            6 => "Otro",
            _ => null
        };
    
        // Prioridad - mapear índice (0-based) a valor
        vm.Prioridad = CmbPrioridad.SelectedIndex + 1;
    
        // Tiempo estimado - mapear índice a minutos
        vm.TiempoEstimado = CmbTiempoEstimado.SelectedIndex switch
        {
            1 => 5,
            2 => 10,
            3 => 15,
            4 => 30,
            5 => 45,
            6 => 60,
            7 => 90,
            8 => 120,
            9 => 180,
            10 => 240,
            _ => 0
        };

        // Recordatorio
        vm.Recordatorio = PickerRecordatorio.SelectedDate;

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
        PickerFecInicio.SelectedDate = null;
        if (DataContext is NewTaskViewModel vm)
            vm.FecInicio = null;
    }

    private void ClearFecLimite_Click(object? sender, RoutedEventArgs e)
    {
        PickerFecLimite.SelectedDate = null;
        if (DataContext is NewTaskViewModel vm)
            vm.FecLimite = null;
    }

    private void ClearHoraInicio_Click(object? sender, RoutedEventArgs e)
    {
        PickerHoraInicio.SelectedTime = null;
        if (DataContext is NewTaskViewModel vm)
            vm.HoraInicio = null;
    }

    private void ClearHoraFin_Click(object? sender, RoutedEventArgs e)
    {
        PickerHoraFin.SelectedTime = null;
        if (DataContext is NewTaskViewModel vm)
            vm.HoraFin = null;
    }
}