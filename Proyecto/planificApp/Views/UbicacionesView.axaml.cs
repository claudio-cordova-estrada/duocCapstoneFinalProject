using Avalonia.Controls;
using Mapsui;
using Mapsui.Layers;
using Avalonia.Input;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using planificApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace planificApp.Views
{
    public partial class UbicacionesView : UserControl
    {
        private MemoryLayer? _capaPuntos;

        public UbicacionesView()
        {
            InitializeComponent();
            InicializarMapa();

            if (MiMapa != null)
            {
                MiMapa.PointerWheelChanged += (sender, e) =>
                {
                    e.Handled = true;
                };
            }
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

            if (DataContext is UbicacionesViewModel vm)
            {
                vm.MapaDebeActualizarse = () => DibujarPines(vm.ListaUbicaciones);

                vm.EnfocarEnUbicacion = (latitud, longitud) =>
                {
                    var (x, y) = SphericalMercator.FromLonLat(longitud, latitud);
                    MiMapa.Map?.Navigator?.CenterOn(new MPoint(x, y));
                };

                // NUEVO: Suscribirse a la ruta
                vm.TrazarRutaEnMapa = DibujarRuta;

                // NUEVO: Activar evento de clics en el mapa
                if (MiMapa.Map != null)
                {
                    MiMapa.Map.Info += MiMapa_Info;
                }

                DibujarPines(vm.ListaUbicaciones);

                vm.BorrarPinTemporalDelMapa = () =>
                {
                    if (MiMapa.Map == null) return;

                    // Buscamos la capa temporal y la destruimos
                    var capaTemporal = MiMapa.Map.Layers.FirstOrDefault(l => l.Name == "CapaPinTemporal");
                    if (capaTemporal != null)
                    {
                        MiMapa.Map.Layers.Remove(capaTemporal);
                        MiMapa.Refresh();
                    }
                };
            }
        }

        // NUEVO MÉTODO: Dibuja la línea azul en el mapa
        private void DibujarRuta(List<(double Latitud, double Longitud)> puntosRuta)
        {
            if (MiMapa.Map == null || puntosRuta.Count == 0) return;

            // Borrar ruta anterior
            var capaVieja = MiMapa.Map.Layers.FirstOrDefault(l => l.Name == "CapaRuta");
            if (capaVieja != null) MiMapa.Map.Layers.Remove(capaVieja);

            // Mapsui v4 usa puntos individuales para dibujar líneas si no tienes NTS
            var features = new List<IFeature>();
            var lineFeature = new MemoryLayer { Name = "CapaRuta" };

            // Crear la línea uniendo los puntos
            var rutaGeometrica = new Mapsui.Nts.GeometryFeature
            {
                Geometry = new NetTopologySuite.Geometries.LineString(
                    puntosRuta.Select(p => {
                        var (x, y) = SphericalMercator.FromLonLat(p.Longitud, p.Latitud);
                        return new NetTopologySuite.Geometries.Coordinate(x, y);
                    }).ToArray())
            };

            // Estilo: Línea Azul Semitransparente, grosor 5
            rutaGeometrica.Styles.Add(new VectorStyle
            {
                Line = new Pen(Mapsui.Styles.Color.FromArgb(180, 0, 100, 255), 5)
            });

            features.Add(rutaGeometrica);
            lineFeature.Features = features;

            MiMapa.Map.Layers.Add(lineFeature);
            MiMapa.Refresh();
        }

        // Detecta clics en el mapa
        private async void MiMapa_Info(object? sender, MapInfoEventArgs e)
        {
            if (DataContext is UbicacionesViewModel vm && vm.ModoSeleccionActivo)
            {
                if (e.WorldPosition != null)
                {
                    vm.ModoSeleccionActivo = false; // El botón se desactiva solo

                    var lonLat = SphericalMercator.ToLonLat(e.WorldPosition.X, e.WorldPosition.Y);

                    // 1. Dibujamos pin morado de carga
                    DibujarPinTemporal(lonLat.lat, lonLat.lon, "#6366f1");

                    // 2. Centramos el mapa
                    MiMapa.Map?.Navigator?.CenterOn(e.WorldPosition);

                    // 3. Mostramos menú flotante si está configurado
                    Avalonia.Controls.Primitives.FlyoutBase.ShowAttachedFlyout(MiMapa);

                    // 4. Efecto de "Cargando..."
                    vm.UbicacionSeleccionada = new UbicacionVisual
                    {
                        Nombre = "Buscando dirección...",
                        AreaInteres = "Buscando...",
                        ColorHex = "#6366f1",
                        Latitud = lonLat.lat,
                        Longitud = lonLat.lon
                    };

                    // 5. Obtener la dirección real desde Google
                    string direccion = await vm.ServicioGeo.ObtenerDireccionDesdeCoordenadasAsync(lonLat.lat, lonLat.lon);

                    // 6. Cambiamos pin a verde de éxito
                    DibujarPinTemporal(lonLat.lat, lonLat.lon, "#10b981");

                    // 7. ACTUALIZACIÓN FINAL: ¡Aquí encendemos el botón!
                    vm.UbicacionSeleccionada = new UbicacionVisual
                    {
                        Nombre = "Punto seleccionado",
                        AreaInteres = direccion,

                        // ESTAS DOS LÍNEAS SON LA CLAVE QUE FALTABA:
                        DireccionExacta = direccion,   // Guarda la calle para el formulario
                        EsTemporal = true,             // ¡Esto hace aparecer el botón verde!

                        ColorHex = "#10b981",
                        UltimaVisitaFormateada = "Ubicación temporal",
                        TransportePreferido = "-",
                        Latitud = lonLat.lat,
                        Longitud = lonLat.lon
                    };
                }
            }
        }

        private void DibujarPines(IEnumerable<UbicacionVisual> ubicaciones)
        {
            if (MiMapa.Map == null) return;

            // Paso A: Buscamos y borramos la capa de pines anterior (Sin usar LINQ)
            Mapsui.Layers.ILayer? capaVieja = null;
            foreach (var layer in MiMapa.Map.Layers)
            {
                if (layer.Name == "CapaPines")
                {
                    capaVieja = layer;
                    break; // Encontramos la capa, dejamos de buscar
                }
            }

            // Si la encontramos, la borramos del mapa
            if (capaVieja != null)
            {
                MiMapa.Map.Layers.Remove(capaVieja);
            }

            var features = new List<IFeature>();

            // Paso B: Creamos un punto por cada ubicación guardada
            foreach (var ubi in ubicaciones)
            {
                var (x, y) = SphericalMercator.FromLonLat(ubi.Longitud, ubi.Latitud);
                var punto = new PointFeature(new MPoint(x, y));

                var colorAvalonia = Avalonia.Media.Color.Parse(ubi.ColorHex ?? "#a78bfa");
                var colorMapsui = Mapsui.Styles.Color.FromArgb(colorAvalonia.A, colorAvalonia.R, colorAvalonia.G, colorAvalonia.B);

                punto.Styles.Add(new SymbolStyle
                {
                    Fill = new Brush(colorMapsui),
                    SymbolScale = 0.6,
                    Outline = new Pen(Mapsui.Styles.Color.White, 2)
                });

                features.Add(punto);
            }

            // Paso C: Metemos todos los puntos en una nueva capa
            var capaPines = new MemoryLayer
            {
                Name = "CapaPines",
                Features = features,
                Style = null
            };

            MiMapa.Map.Layers.Add(capaPines);
            MiMapa.Refresh();
        }
        private void DibujarPinTemporal(double latitud, double longitud, string colorHex)
        {
            if (MiMapa.Map == null) return;

            // Borramos el pin temporal anterior si existe para no duplicarlo
            var capaVieja = MiMapa.Map.Layers.FirstOrDefault(l => l.Name == "CapaPinTemporal");
            if (capaVieja != null)
            {
                MiMapa.Map.Layers.Remove(capaVieja);
            }

            var features = new List<IFeature>();
            var (x, y) = SphericalMercator.FromLonLat(longitud, latitud);
            var punto = new PointFeature(new MPoint(x, y));

            // Convertimos el color hexadecimal a color de Mapsui
            var colorAvalonia = Avalonia.Media.Color.Parse(colorHex);
            var colorMapsui = Mapsui.Styles.Color.FromArgb(colorAvalonia.A, colorAvalonia.R, colorAvalonia.G, colorAvalonia.B);

            punto.Styles.Add(new SymbolStyle
            {
                Fill = new Brush(colorMapsui),
                SymbolScale = 0.8, // Un poco más grande para destacar que es una selección
                Outline = new Pen(Mapsui.Styles.Color.White, 2)
            });

            features.Add(punto);

            var capaTemporal = new MemoryLayer
            {
                Name = "CapaPinTemporal",
                Features = features,
                Style = null
            };

            MiMapa.Map.Layers.Add(capaTemporal);
            MiMapa.Refresh();
        }
    }
}