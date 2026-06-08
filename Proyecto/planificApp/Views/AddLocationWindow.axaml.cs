using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using PlanificApp.Models.Services.Interfaces;

namespace planificApp.Views;

// Clase para enviar los datos de vuelta
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
    private readonly IGeoService _geoService;
    public ObservableCollection<string> Sugerencias { get; set; } = new();
    private string _selectedColor = "#a78bfa";
    private string _selectedTransport = "Metro";

    // Constructor para el diseñador
    public AddLocationWindow() { InitializeComponent();
                            _geoService = null!;
    }

    // Constructor real
    public AddLocationWindow(IGeoService geoService) : this()
    {
        _geoService = geoService;
        LocationInput.ItemsSource = Sugerencias;
        LocationInput.Populating += OnLocationInputPopulating;
        SelectColor(_selectedColor);
        SelectTransport(_selectedTransport);
    }

    // --- MÉTODOS DE LA VENTANA ---
    public void SetAreasDeInteres(IEnumerable<string> areas)
    {
        AreaSelect.ItemsSource = areas;
        if (AreaSelect.ItemCount > 0) AreaSelect.SelectedIndex = 0;
    }

    public void SetEditMode(string name, string direccion, string area, string color, string transport)
    {
        Title = "Editar ubicación";
        NameInput.Text = name;
        LocationInput.Text = direccion;
        DeleteButton.IsVisible = true;
        _selectedColor = color;
        _selectedTransport = transport;
        AreaSelect.SelectedItem = area;
        SelectColor(_selectedColor);
        SelectTransport(_selectedTransport);
    }

    private async void OnLocationInputPopulating(object? sender, PopulatingEventArgs e)
    {
        var text = LocationInput.Text;
        if (string.IsNullOrWhiteSpace(text) || text.Length < 3) return;
        e.Cancel = true;
        var resultados = await _geoService.GetPredictionsAsync(text);
        Sugerencias.Clear();
        foreach (var item in resultados) Sugerencias.Add(item);
        LocationInput.PopulateComplete();
    }

    private void ColorOption_Tapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border b && b.Tag is string color) { _selectedColor = color; SelectColor(color); }
    }

    private void SelectColor(string color)
    {
        foreach (var child in ColorGrid.Children)
        {
            if (child is Border b)
            {
                if (b.Tag?.ToString() == color) { b.BorderThickness = new Thickness(2); b.BorderBrush = new SolidColorBrush(Colors.White); b.Width = 26; b.Height = 26; }
                else { b.BorderThickness = new Thickness(0); b.Width = 22; b.Height = 22; }
            }
        }
    }

    private void TransportOption_Tapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border b && b.Tag is string transport) { _selectedTransport = transport; SelectTransport(transport); }
    }

    private void SelectTransport(string transport)
    {
        foreach (var child in TransportGrid.Children)
        {
            if (child is Border b)
            {
                bool isSelected = b.Tag?.ToString() == transport;
                if (isSelected) { b.Background = new SolidColorBrush(Color.Parse("#1e1a2e")); b.BorderBrush = new SolidColorBrush(Color.Parse("#3d3060")); if (b.Child is TextBlock tb) tb.Foreground = new SolidColorBrush(Color.Parse("#a78bfa")); }
                else { b.Background = new SolidColorBrush(Color.Parse("#1a1a1a")); b.BorderBrush = new SolidColorBrush(Color.Parse("#222222")); if (b.Child is TextBlock tb) tb.Foreground = new SolidColorBrush(Color.Parse("#555555")); }
            }
        }
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e) { Close(null); }
    private void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        var resultado = new LocationFormData { Nombre = NameInput.Text ?? "Nueva Ubicación", Direccion = LocationInput.Text ?? "", AreaInteres = AreaSelect.SelectedItem?.ToString() ?? "General", ColorHex = _selectedColor, Transporte = _selectedTransport };
        Close(resultado);
    }
    private void DeleteButton_Click(object? sender, RoutedEventArgs e) { Close(null); }
}