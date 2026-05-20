using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using planificApp.ViewModels;

namespace planificApp.Views;

public partial class NewTaskWindow : Window
{
    public bool Result { get; private set; }

    public NewTaskWindow()
    {
        InitializeComponent();

        PickerFecInicio.SelectedDateChanged += (_, _) =>
        {
            if (DataContext is NewTaskViewModel vm && PickerFecInicio.SelectedDate.HasValue)
                vm.FecInicio = PickerFecInicio.SelectedDate.Value;
        };

        PickerFecLimite.SelectedDateChanged += (_, _) =>
        {
            if (DataContext is NewTaskViewModel vm && PickerFecLimite.SelectedDate.HasValue)
                vm.FecLimite = PickerFecLimite.SelectedDate.Value;
        };

        PickerHoraInicio.SelectedTimeChanged += (_, e) =>
        {
            if (DataContext is NewTaskViewModel vm)
                vm.HoraInicio = e.NewTime;
        };

        PickerHoraFin.SelectedTimeChanged += (_, e) =>
        {
            if (DataContext is NewTaskViewModel vm)
                vm.HoraFin = e.NewTime;
        };

        CmbPrioridad.SelectionChanged += (_, _) =>
        {
            if (DataContext is NewTaskViewModel vm)
                vm.Prioridad = CmbPrioridad.SelectedIndex + 1;
        };

        CmbUbicacion.SelectionChanged += (_, _) =>
        {
            if (DataContext is NewTaskViewModel vm)
                vm.Ubicacion = CmbUbicacion.SelectedIndex <= 0 ? null : UbicacionFromIndex(CmbUbicacion.SelectedIndex);
        };

        CmbTiempoEstimado.SelectionChanged += (_, _) =>
        {
            if (DataContext is NewTaskViewModel vm)
                vm.TiempoEstimado = TiempoEstimadoFromIndex(CmbTiempoEstimado.SelectedIndex);
        };

        PickerRecordatorio.SelectedDateChanged += (_, _) =>
        {
            if (DataContext is NewTaskViewModel vm && PickerRecordatorio.SelectedDate.HasValue)
                vm.Recordatorio = PickerRecordatorio.SelectedDate.Value;
        };
    }

    private async void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not NewTaskViewModel vm) return;

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

    private static string? UbicacionFromIndex(int index) => index switch
    {
        1 => "Casa", 2 => "Trabajo", 3 => "Universidad",
        4 => "Gimnasio", 5 => "Supermercado", 6 => "Otro",
        _ => null
    };

    private static int TiempoEstimadoFromIndex(int index) => index switch
    {
        1 => 5, 2 => 10, 3 => 15, 4 => 30, 5 => 45,
        6 => 60, 7 => 90, 8 => 120, 9 => 180, 10 => 240,
        _ => 0
    };
}