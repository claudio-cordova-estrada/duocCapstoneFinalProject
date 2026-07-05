using Avalonia.Controls;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using planificApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Interactivity;

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

            // 3. Centramos el mapa inicialmente en Concepción/Hualpén
            var (x, y) = SphericalMercator.FromLonLat(-73.0497, -36.8261);
            var posicionInicial = new MPoint(x, y);
            MiMapa.Map?.Navigator?.CenterOnAndZoomTo(posicionInicial, 13);

            // 4. Creamos una capa transparente para colocar los puntos encima
            _capaPuntos = new MemoryLayer
            {
                Name = "Capa de Ubicaciones",
                Style = null
            };
            MiMapa.Map?.Layers.Add(_capaPuntos);
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            if (DataContext is UbicacionesViewModel vm)
            {
                vm.MapaDebeActualizarse = () => DibujarPines(vm.ListaUbicaciones);

                vm.EnfocarEnUbicacion = (latitud, longitud) =>
                {
                    var (x, y) = SphericalMercator.FromLonLat(longitud, latitud);
                    MiMapa.Map?.Navigator?.CenterOnAndZoomTo(new MPoint(x, y), MiMapa.Map.Navigator.Resolutions[15]);
                };

                // Centro inicial en la comuna del usuario (cuando aún no tiene ubicaciones guardadas).
                vm.CentrarEnUbicacionInicial = (latitud, longitud) =>
                {
                    var (x, y) = SphericalMercator.FromLonLat(longitud, latitud);
                    MiMapa.Map?.Navigator?.CenterOnAndZoomTo(new MPoint(x, y), 13);
                };

                vm.TrazarRutaEnMapa = DibujarRuta;
                vm.LimpiarRutaDelMapa = LimpiarRuta;

                if (MiMapa.Map != null)
                {
                    // Desuscribimos antes de suscribir para evitar disparos dobles
                    MiMapa.Map.Info -= MiMapa_Info;
                    MiMapa.Map.Info += MiMapa_Info;
                }

                DibujarPines(vm.ListaUbicaciones);

                vm.BorrarPinTemporalDelMapa = () =>
                {
                    if (MiMapa.Map == null) return;
                    var capaTemporal = MiMapa.Map.Layers.FirstOrDefault(l => l.Name == "CapaPinTemporal");
                    if (capaTemporal != null)
                    {
                        MiMapa.Map.Layers.Remove(capaTemporal);
                        MiMapa.Refresh();
                    }
                };
            }
        }

        private void DibujarRuta(List<(double Latitud, double Longitud)> puntosRuta)
        {
            if (MiMapa.Map == null) return;

            // 1. SIEMPRE eliminamos la capa de ruta vieja primero
            var capaVieja = MiMapa.Map.Layers.FirstOrDefault(l => l.Name == "CapaRuta");
            if (capaVieja != null)
            {
                MiMapa.Map.Layers.Remove(capaVieja);
            }

            // 2. Si la lista viene vacía (Botón Limpiar), refrescamos el mapa y detenemos la función aquí
            if (puntosRuta == null || puntosRuta.Count == 0)
            {
                MiMapa.Refresh();
                return;
            }

            // 3. Si hay puntos, creamos la nueva línea y la dibujamos
            var features = new List<IFeature>();
            var lineFeature = new MemoryLayer { Name = "CapaRuta" };

            var rutaGeometrica = new Mapsui.Nts.GeometryFeature
            {
                Geometry = new NetTopologySuite.Geometries.LineString(
                    puntosRuta.Select(p => {
                        var (x, y) = SphericalMercator.FromLonLat(p.Longitud, p.Latitud);
                        return new NetTopologySuite.Geometries.Coordinate(x, y);
                    }).ToArray())
            };

            rutaGeometrica.Styles.Add(new VectorStyle
            {
                Line = new Pen(Mapsui.Styles.Color.FromArgb(180, 0, 100, 255), 5)
            });

            features.Add(rutaGeometrica);
            lineFeature.Features = features;

            MiMapa.Map.Layers.Add(lineFeature);
            MiMapa.Refresh();
        }

        private void LimpiarRuta()
        {
            if (MiMapa.Map == null) return;
            var capaRuta = MiMapa.Map.Layers.FirstOrDefault(l => l.Name == "CapaRuta");
            if (capaRuta != null)
            {
                MiMapa.Map.Layers.Remove(capaRuta);
                MiMapa.Refresh();
            }
        }

        private async void MiMapa_Info(object? sender, MapInfoEventArgs e)
        {
            if (DataContext is UbicacionesViewModel vm)
            {
                if (vm.ModoSeleccionActivo)
                {
                    if (e.WorldPosition != null)
                    {
                        e.Handled = true;
                        vm.ModoSeleccionActivo = false;

                        var lonLat = SphericalMercator.ToLonLat(e.WorldPosition.X, e.WorldPosition.Y);

                        DibujarPinTemporal(lonLat.lat, lonLat.lon, "#6366f1");
                        MiMapa.Map?.Navigator?.CenterOn(e.WorldPosition);
                        Avalonia.Controls.Primitives.FlyoutBase.ShowAttachedFlyout(MiMapa);

                        vm.UbicacionSeleccionada = new UbicacionVisual
                        {
                            Nombre = "Buscando dirección...",
                            AreaInteres = "No aplica",
                            ColorHex = "#6366f1",
                            Latitud = lonLat.lat,
                            Longitud = lonLat.lon
                        };

                        string direccion = await vm.ServicioGeo.ObtenerDireccionDesdeCoordenadasAsync(lonLat.lat, lonLat.lon);

                        DibujarPinTemporal(lonLat.lat, lonLat.lon, "#10b981");

                        vm.UbicacionSeleccionada = new UbicacionVisual
                        {
                            Nombre = "Punto seleccionado",
                            AreaInteres = "No aplica",
                            DireccionExacta = direccion,
                            EsTemporal = true,
                            ColorHex = "#10b981",
                            TransportePreferido = "-",
                            Latitud = lonLat.lat,
                            Longitud = lonLat.lon
                        };
                    }
                }
                else if (vm.UbicacionSeleccionada != null)
                {
                    vm.UnfocusCommand.Execute(null);
                }
            }
        }

        private void DibujarPines(IEnumerable<UbicacionVisual> ubicaciones)
        {
            if (MiMapa.Map == null) return;

            var capaVieja = MiMapa.Map.Layers.FirstOrDefault(l => l.Name == "CapaPines");
            if (capaVieja != null) MiMapa.Map.Layers.Remove(capaVieja);

            var capaTemporal = MiMapa.Map.Layers.FirstOrDefault(l => l.Name == "CapaPinTemporal");
            if (capaTemporal != null) MiMapa.Map.Layers.Remove(capaTemporal);

            var features = new List<IFeature>();

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

            var capaVieja = MiMapa.Map.Layers.FirstOrDefault(l => l.Name == "CapaPinTemporal");
            if (capaVieja != null)
            {
                MiMapa.Map.Layers.Remove(capaVieja);
            }

            var features = new List<IFeature>();
            var (x, y) = SphericalMercator.FromLonLat(longitud, latitud);
            var punto = new PointFeature(new MPoint(x, y));

            var colorAvalonia = Avalonia.Media.Color.Parse(colorHex);
            var colorMapsui = Mapsui.Styles.Color.FromArgb(colorAvalonia.A, colorAvalonia.R, colorAvalonia.G, colorAvalonia.B);

            punto.Styles.Add(new SymbolStyle
            {
                Fill = new Brush(colorMapsui),
                SymbolScale = 0.8,
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
        
        private void BtnCentrarMapa_Click(object? sender, RoutedEventArgs e)
        {
            if (MiMapa.Map == null) return;

            if (DataContext is UbicacionesViewModel vm && vm.ListaUbicaciones.Any())
            {
                // Si el usuario tiene ubicaciones guardadas, calculamos el cuadro que las encierra a todas
                var capaPines = MiMapa.Map.Layers.FirstOrDefault(l => l.Name == "CapaPines");

                if (capaPines?.Extent != null)
                {
                    // Si solo hay 1 pin (el Extent width y height es 0), hacemos zoom estándar
                    if (capaPines.Extent.Width == 0 && capaPines.Extent.Height == 0)
                    {
                        MiMapa.Map.Navigator?.CenterOnAndZoomTo(capaPines.Extent.Centroid, MiMapa.Map.Navigator.Resolutions[14]);
                    }
                    else
                    {
                        // Si hay varios, usamos la magia de Mapsui para encuadrar todo expandiendo un 15% los bordes para que no queden cortados
                        var mRect = capaPines.Extent.Grow(capaPines.Extent.Width * 0.15);
                        MiMapa.Map.Navigator?.ZoomToBox(mRect);
                    }
                }
            }
            else
            {
                // Si no hay pines, centramos en la ubicación por defecto (Concepción/Hualpén) con un zoom general
                var (x, y) = SphericalMercator.FromLonLat(-73.0497, -36.8261);
                MiMapa.Map.Navigator?.CenterOnAndZoomTo(new MPoint(x, y), MiMapa.Map.Navigator.Resolutions[11]);
            }
        }
    }
}