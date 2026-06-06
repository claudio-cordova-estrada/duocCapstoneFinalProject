using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using planificApp.ViewModels;

namespace planificApp.Views
{
    public partial class UbicacionesView : UserControl
    {
        private MemoryLayer? _capaPuntos;

        public UbicacionesView()
        {
            InitializeComponent();
            InicializarMapa();
        }

        private void InicializarMapa()
        {
            // 1. Cargamos la capa base gratuita
            MiMapa.Map?.Layers.Add(OpenStreetMap.CreateTileLayer());

            // 2. Limpiamos los textos molestos (FPS, INFO) de las esquinas del mapa
            MiMapa.Map?.Widgets.Clear();

            // 3. Centramos el mapa
            var (x, y) = SphericalMercator.FromLonLat(-73.0497, -36.8261);
            var posicionInicial = new MPoint(x, y);
            MiMapa.Map?.Navigator?.CenterOnAndZoomTo(posicionInicial, 13);

            // 4. Creamos una capa transparente para colocar los puntos encima
            _capaPuntos = new MemoryLayer
            {
                Name = "Capa de Ubicaciones",
                Style = null // Anulamos el estilo general para pintar cada punto de su propio color
            };
            MiMapa.Map?.Layers.Add(_capaPuntos);
        }

        // Este evento se dispara cuando la vista se conecta con el ViewModel
        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            if (DataContext is UbicacionesViewModel viewModel)
            {
                // Nos suscribimos al "aviso". Cuando Google convierta las direcciones en coordenadas, se llamará a DibujarPines
                viewModel.MapaDebeActualizarse = () => DibujarPines(viewModel.ListaUbicaciones);
            }
        }

        private void DibujarPines(IEnumerable<UbicacionVisual> ubicaciones)
        {
            var features = new List<IFeature>();

            foreach (var ubi in ubicaciones)
            {
                // Transformamos la Latitud y Longitud exacta al formato que exige el mapa
                var (x, y) = SphericalMercator.FromLonLat(ubi.Longitud, ubi.Latitud);
                var punto = new PointFeature(new MPoint(x, y));

                // Convertimos tu color Hexadecimal al color que entiende la librería
                var colorMap = Mapsui.Styles.Color.FromString(ubi.ColorHex ?? "#000000");

                // Dibujamos el círculo del punto
                punto.Styles.Add(new SymbolStyle
                {
                    SymbolType = SymbolType.Ellipse,
                    SymbolScale = 0.6,
                    Fill = new Brush(colorMap),
                    Outline = new Pen(Mapsui.Styles.Color.White, 3)
                });

                // Dibujamos el texto flotante con el nombre (Casa, Trabajo, Iglesia)
                punto.Styles.Add(new LabelStyle
                {
                    Text = ubi.Nombre,
                    BackColor = new Brush(Mapsui.Styles.Color.Transparent),
                    ForeColor = Mapsui.Styles.Color.Black,
                    Halo = new Pen(Mapsui.Styles.Color.White, 2),
                    Offset = new Offset(0, -16)
                });

                features.Add(punto);
            }

            // Subimos los puntos a nuestra capa
            if (_capaPuntos != null)
            {
                _capaPuntos.Features = features;
            }

            // Forzamos al mapa a repintarse para que los puntos aparezcan de inmediato
            MiMapa.RefreshGraphics();
        }
    }
}