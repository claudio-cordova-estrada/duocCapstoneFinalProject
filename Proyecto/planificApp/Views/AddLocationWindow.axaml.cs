using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using PlanificApp.Models.Services.Interfaces;
using planificApp.ViewModels;

namespace planificApp.Views;

// DTO que la ventana devuelve al cerrarse (lo consume DialogService).
public class LocationFormData
{
    public string Nombre { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string AreaInteres { get; set; } = string.Empty;
    public string ColorHex { get; set; } = string.Empty;
    public string Transporte { get; set; } = string.Empty;
}

public partial class AddLocationWindow : Window
{
    private readonly AddLocationWindowViewModel _vm;

    // Constructor para el diseñador
    public AddLocationWindow()
    {
        InitializeComponent();
        _vm = new AddLocationWindowViewModel();
        DataContext = _vm;
    }

    // Constructor real
    public AddLocationWindow(IGeoService geoService)
    {
        InitializeComponent();
        _vm = new AddLocationWindowViewModel(geoService);
        DataContext = _vm;

        _vm.CerrarSolicitado += OnCerrarSolicitado;
        _vm.PropertyChanged += Vm_PropertyChanged;
        LocationInput.Populating += OnLocationInputPopulating;

        ActualizarSeleccionVisual();
    }

    // API pública usada por DialogService para el modo edición.
    public void SetEditMode(string name, string direccion, string area, string color, string transport)
    {
        _vm.ConfigurarEdicion(name, direccion, area, color, transport);
        ActualizarSeleccionVisual();
    }

    private void OnCerrarSolicitado(LocationFormData? resultado) => Close(resultado);

    private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(AddLocationWindowViewModel.SelectedColor)
            or nameof(AddLocationWindowViewModel.SelectedTransport))
        {
            ActualizarSeleccionVisual();
        }
    }

    private async void OnLocationInputPopulating(object? sender, PopulatingEventArgs e)
    {
        // async void: cualquier excepción que escape de aquí cae la app entera
        // (no hay manejador global). Por eso TODO va dentro de un try/catch.
        try
        {
            var text = LocationInput.Text;
            if (string.IsNullOrWhiteSpace(text) || text.Length < 3) return;
            e.Cancel = true;
            await _vm.BuscarSugerenciasAsync(text);
            LocationInput.PopulateComplete();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ADD_LOCATION] Error en autocompletado: {ex.Message}");
        }
    }

    private void ColorOption_Tapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border b && b.Tag is string color)
            _vm.SeleccionarColorCommand.Execute(color);
    }

    private void TransportOption_Tapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border b && b.Tag is string transport)
            _vm.SeleccionarTransporteCommand.Execute(transport);
    }

    // Resuelve un brush del tema actual (claro/oscuro); usa el fallback si no existe.
    private static IBrush ThemeBrush(string key, string fallback)
    {
        var app = Avalonia.Application.Current;
        if (app != null && app.TryGetResource(key, app.ActualThemeVariant, out var res) && res is IBrush b)
            return b;
        return new SolidColorBrush(Color.Parse(fallback));
    }

    // Realce visual de los recuadros de color/transporte según la selección del VM.
    // Es manipulación directa de Borders concretos → propio de la vista.
    private void ActualizarSeleccionVisual()
    {
        foreach (var child in ColorGrid.Children)
        {
            if (child is Border b)
            {
                bool sel = b.Tag?.ToString() == _vm.SelectedColor;
                // Anillo de selección con el color de texto del tema (contrasta en claro y oscuro).
                if (sel) { b.BorderThickness = new Thickness(2); b.BorderBrush = ThemeBrush("TextPrimary", "#ffffff"); b.Width = 26; b.Height = 26; }
                else { b.BorderThickness = new Thickness(0); b.Width = 22; b.Height = 22; }
            }
        }

        foreach (var child in TransportGrid.Children)
        {
            if (child is Border b)
            {
                bool sel = b.Tag?.ToString() == _vm.SelectedTransport;
                if (sel) { b.Background = ThemeBrush("AccentBackgroundLight", "#1e1a2e"); b.BorderBrush = ThemeBrush("AccentBorder", "#3d3060"); if (b.Child is TextBlock tb) tb.Foreground = ThemeBrush("AccentColor", "#a78bfa"); }
                else { b.Background = ThemeBrush("PillBackground", "#1a1a1a"); b.BorderBrush = ThemeBrush("PillBorder", "#222222"); if (b.Child is TextBlock tb) tb.Foreground = ThemeBrush("TextDisabled", "#555555"); }
            }
        }
    }
}
