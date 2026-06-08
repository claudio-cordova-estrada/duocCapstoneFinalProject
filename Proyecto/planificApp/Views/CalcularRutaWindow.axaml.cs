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

        // Constructor para el diseñador visual
        public CalcularRutaWindow()
        {
            InitializeComponent();
            _geoService = null!;
        }

        // Constructor real que recibe tus servicios y tu lista de ubicaciones
        public CalcularRutaWindow(IGeoService geoService, ObservableCollection<UbicacionVisual> ubicaciones)
        {
            InitializeComponent();
            _geoService = geoService;

            // Llenamos los Dropdowns con tus ubicaciones guardadas
            ComboOrigen.ItemsSource = ubicaciones;
            ComboDestino.ItemsSource = ubicaciones;

            BtnCalcular.Click += BtnCalcular_Click;
        }

        public async void BtnCalcular_Click(object? sender, RoutedEventArgs e)
        {
            if (ComboOrigen.SelectedItem is UbicacionVisual origen &&
                ComboDestino.SelectedItem is UbicacionVisual destino &&
                ComboTransporte.SelectedItem is ComboBoxItem transporteItem)
            {
                string transporte = transporteItem.Content?.ToString() ?? "Auto";
                TxtResultado.Text = "Calculando...";

                // Usamos el nuevo método
                var (tiempo, ruta) = await _geoService.CalcularRutaConTrazadoAsync(
    origen.Latitud, origen.Longitud,
    destino.Latitud, destino.Longitud,
    transporte);

                TxtResultado.Text = tiempo;

                // Enviamos el trazado para que se dibuje en la ventana principal
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