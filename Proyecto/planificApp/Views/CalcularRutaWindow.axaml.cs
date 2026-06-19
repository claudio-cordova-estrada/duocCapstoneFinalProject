using System;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using planificApp.ViewModels;
using PlanificApp.Models.Services.Interfaces;

namespace planificApp.Views
{
    public partial class CalcularRutaWindow : Window
    {
        private readonly IGeoService _geoService;

        public CalcularRutaWindow()
        {
            InitializeComponent();
            _geoService = null!;
        }

        public CalcularRutaWindow(IGeoService geoService, ObservableCollection<UbicacionVisual> ubicaciones)
        {
            InitializeComponent();
            _geoService = geoService;

            ComboOrigen.ItemsSource = ubicaciones;
            ComboDestino.ItemsSource = ubicaciones;

            ComboDestino.SelectionChanged += ComboDestino_SelectionChanged;
            Closed += CalcularRutaWindow_Closed;
        }

        private void ComboDestino_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (ComboDestino.SelectedItem is UbicacionVisual destino)
            {
                TxtTransporte.Text = destino.TransportePreferido ?? "Auto";
            }
            else
            {
                TxtTransporte.Text = "Selecciona un destino";
            }
        }

        private void CalcularRutaWindow_Closed(object? sender, EventArgs e)
        {
            if (Owner?.DataContext is UbicacionesViewModel vm)
            {
                vm.LimpiarRutaDelMapa?.Invoke();
            }
        }

        public async void BtnCalcular_Click(object? sender, RoutedEventArgs e)
        {
            if (ComboOrigen.SelectedItem is UbicacionVisual origen &&
                ComboDestino.SelectedItem is UbicacionVisual destino)
            {
                string transporte = destino.TransportePreferido ?? "Auto";
                TxtResultado.Text = "Calculando...";

                string transporteGeo = transporte switch
                {
                    "A pie" => "A pie",
                    "Bus" => "Bus",
                    _ => "Auto"
                };

                var (tiempo, ruta) = await _geoService.CalcularRutaConTrazadoAsync(
                    origen.Latitud, origen.Longitud,
                    destino.Latitud, destino.Longitud,
                    transporteGeo);

                TxtResultado.Text = tiempo;

                if (Owner?.DataContext is UbicacionesViewModel vm)
                {
                    vm.TrazarRutaEnMapa?.Invoke(ruta);
                }
            }
            else
            {
                TxtResultado.Text = "Seleccione puntos";
            }
        }
    }
}