using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using PlanificApp.Models;
using planificApp.Helpers;
using planificApp.ViewModels;

namespace planificApp.Views;

public partial class InboxView : UserControl
{
    private Border? _selectedTaskBorder;
    private DispatcherTimer? _mensajeTimer;

    public InboxView()
    {
        InitializeComponent();
        Loaded += InboxView_Loaded;
        AddTaskInput.KeyDown += AddTaskInput_KeyDown;
    }

    private async void AddTaskInput_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (DataContext is not InboxViewModel vm) return;
        if (string.IsNullOrWhiteSpace(vm.QuickAddNombre)) return;

        await vm.QuickAddCommand.ExecuteAsync(null);
        AddTaskInput.Focus();
    }

    private async void InboxView_Loaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is InboxViewModel vm)
            await vm.CargarTareasAsync();
    }

    private async void NewTaskButton_Click(object? sender, RoutedEventArgs e)
    {
        var result = await DialogHelper.ShowNewTaskDialog(this);
        if (result && DataContext is InboxViewModel vm)
            await vm.CargarTareasAsync();
    }

    private void RootPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not InboxViewModel vm || vm.TareaSeleccionada == null) return;

        if (e.Source is Control ctrl)
        {
            var current = ctrl;
            while (current != null)
            {
                if (current is Border b && b.Classes.Contains("task-item")) return;
                if (current is Border b2 && b2.Name == "DetailPanelBorder") return;
                if (current is Button) return;
                if (current is CalendarDatePicker or TimePicker or ComboBox or TextBox) return;
                current = current.Parent as Control;
            }
        }

        vm.TareaSeleccionada = null;
        HideDetail();
        ClearSelection();
    }

    private void TaskItem_Tapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Border border || border.Tag is not Tarea tarea) return;
        if (DataContext is not InboxViewModel vm) return;

        ClearSelection();
        _selectedTaskBorder = border;
        border.Classes.Add("selected");

        vm.SeleccionarTareaCommand.Execute(tarea);
        ShowDetail(vm);
    }

    private void ToggleCompletadas_Tapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is not InboxViewModel vm) return;
        vm.ToggleCompletadasCommand.Execute(null);
    }

    private void ClearSelection()
    {
        if (_selectedTaskBorder != null)
        {
            _selectedTaskBorder.Classes.Remove("selected");
            _selectedTaskBorder = null;
        }
    }

    private async void TaskCheck_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not Tarea tarea) return;
        if (DataContext is not InboxViewModel vm) return;

        e.Handled = true;
        await vm.ToggleTareaCommand.ExecuteAsync(tarea);
    }

    private async void TaskDelete_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not Tarea tarea) return;
        if (DataContext is not InboxViewModel vm) return;

        e.Handled = true;
        await vm.EliminarTareaCommand.ExecuteAsync(tarea);
    }

    private async void DeleteTask_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not InboxViewModel vm || vm.TareaSeleccionada == null) return;

        await vm.EliminarTareaCommand.ExecuteAsync(vm.TareaSeleccionada);
        HideDetail();
        ClearSelection();
    }

    private void ClearFecInicio_Click(object? sender, RoutedEventArgs e)
    {
        DetailFecInicio.SelectedDate = null;
    }

    private void ClearFecLimite_Click(object? sender, RoutedEventArgs e)
    {
        DetailFecLimite.SelectedDate = null;
    }

    private void ClearHoraInicio_Click(object? sender, RoutedEventArgs e)
    {
        DetailHoraInicio.SelectedTime = null;
    }

    private void ClearHoraFin_Click(object? sender, RoutedEventArgs e)
    {
        DetailHoraFin.SelectedTime = null;
    }

    private void ShowDetail(InboxViewModel vm)
    {
        DetailPlaceholder.IsVisible = false;
        DetailContent.IsVisible = true;

        DetailNombre.Text = vm.DetalleNombre;
        DetailFecInicio.SelectedDate = vm.TareaSeleccionada?.FecInicio;
        DetailFecLimite.SelectedDate = vm.TareaSeleccionada?.FecLimite;
        DetailHoraInicio.SelectedTime = vm.TareaSeleccionada?.HoraInicio;
        DetailHoraFin.SelectedTime = vm.TareaSeleccionada?.HoraFin;
        DetailUbicacion.SelectedIndex = string.IsNullOrEmpty(vm.TareaSeleccionada?.Ubicacion) ? 0 : DetalleTareaHelper.UbicacionToIndex(vm.TareaSeleccionada.Ubicacion);
        DetailPrioridad.SelectedIndex = vm.DetallePrioridad - 1;
        DetailTiempoEstimado.SelectedIndex = DetalleTareaHelper.TiempoEstimadoToIndex(vm.DetalleTiempoEstimado);
        DetalleTareaHelper.PopulateAreaComboBox(DetailAreaInteres, vm.AreasInteres, vm.TareaSeleccionada?.IdAreaInteres);
        DetailEstado.Text = vm.DetalleEstado;
        DetailMensaje.IsVisible = false;
        DetailTipoActividadFisica.SelectedIndex = DetalleTareaHelper.TipoActividadFisicaToIndex(vm.DetalleTipoActividadFisica);
        DetailTipoActividadMental.SelectedIndex = DetalleTareaHelper.TipoActividadMentalToIndex(vm.DetalleTipoActividadMental);
    }

    private void HideDetail()
    {
        DetailPlaceholder.IsVisible = true;
        DetailContent.IsVisible = false;
    }

    private async void SaveDetail_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not InboxViewModel vm || vm.TareaSeleccionada == null) return;

        vm.DetalleNombre = DetailNombre.Text;
        vm.DetalleFecInicio = DetailFecInicio.SelectedDate;
        vm.DetalleFecLimite = DetailFecLimite.SelectedDate;
        vm.DetalleHoraInicio = DetailHoraInicio.SelectedTime;
        vm.DetalleHoraFin = DetailHoraFin.SelectedTime;
        vm.DetallePrioridad = DetailPrioridad.SelectedIndex + 1;
        vm.DetalleTiempoEstimado = DetalleTareaHelper.TiempoEstimadoFromIndex(DetailTiempoEstimado.SelectedIndex);
        vm.DetalleUbicacion = DetailUbicacion.SelectedIndex <= 0 ? null : DetalleTareaHelper.UbicacionFromIndex(DetailUbicacion.SelectedIndex);
        vm.DetalleIdAreaInteres = DetalleTareaHelper.GetSelectedAreaId(DetailAreaInteres);
        vm.DetalleTipoActividadFisica = DetalleTareaHelper.TipoActividadFisicaFromIndex(DetailTipoActividadFisica.SelectedIndex);
        vm.DetalleTipoActividadMental = DetalleTareaHelper.TipoActividadMentalFromIndex(DetailTipoActividadMental.SelectedIndex);

        await vm.GuardarDetalleCommand.ExecuteAsync(null);

        if (vm.DetalleMensaje == "El nombre no puede estar vacío.")
        {
            DetailNombre.Text = vm.DetalleNombre;
            DetailMensaje.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#f87171"));
        }
        else
        {
            DetailMensaje.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#6ee7b7"));
        }

        DetailMensaje.Text = vm.DetalleMensaje;
        DetailMensaje.Opacity = 1;
        DetailMensaje.IsVisible = !string.IsNullOrEmpty(vm.DetalleMensaje);

        if (!string.IsNullOrEmpty(vm.DetalleMensaje))
        {
            _mensajeTimer?.Stop();
            _mensajeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _mensajeTimer.Tick += (_, _) =>
            {
                FadeOutMensaje();
                _mensajeTimer.Stop();
            };
            _mensajeTimer.Start();
        }
    }

    private async void FadeOutMensaje()
    {
        var duration = TimeSpan.FromMilliseconds(400);
        var steps = 20;
        var interval = duration.TotalMilliseconds / steps;

        for (int i = steps; i >= 0; i--)
        {
            DetailMensaje.Opacity = (double)i / steps;
            await Task.Delay((int)interval);
        }

        DetailMensaje.IsVisible = false;
        DetailMensaje.Opacity = 1;
    }
}