using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using PlanificApp.Models;
using planificApp.ViewModels;

namespace planificApp.Views;

public partial class CondicionesGeneracionWindow : Window
{
    public CondicionesGeneracion? Condiciones { get; private set; }
    public bool ResultadoConfirmado { get; private set; }

    public CondicionesGeneracionWindow()
    {
        InitializeComponent();
    }

    public async void SetFechaLunes(DateTime fechaLunes)
    {
        if (DataContext is CondicionesGeneracionViewModel vm)
        {
            await vm.InicializarAsync(fechaLunes);
        }
    }

    private void GenerarButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not CondicionesGeneracionViewModel vm) return;

        vm.ConfirmarCommand.Execute(null);
        Condiciones = vm.ObtenerCondiciones();
        ResultadoConfirmado = true;
        Close(true);
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        ResultadoConfirmado = false;
        Condiciones = null;
        Close(false);
    }

    private void AreaConsiderar_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (sender is Border border && border.DataContext is AreaInteres area)
        {
            if (DataContext is CondicionesGeneracionViewModel vm)
                vm.MoverAreaANoConsiderarCommand.Execute(area);
        }
    }

    private void AreaNoConsiderar_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (sender is Border border && border.DataContext is AreaInteres area)
        {
            if (DataContext is CondicionesGeneracionViewModel vm)
                vm.MoverAreaAConsiderarCommand.Execute(area);
        }
    }
}