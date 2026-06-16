using PlanificApp.Models;
using PlanificApp.Models.Services.Interfaces;
using PlanificApp.Models.Repositories.Interfaces;
using PlanificApp.Models.Services;
using planificApp.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace planificApp.ViewModels
{
    public class UbicacionesViewModel : PageViewModel
    {
        private readonly IGeoService _geoService;
        private readonly IUbicacionRepository _ubicacionRepo;
        private readonly ISesionService _sesionService;
        private readonly IDialogService _dialogService;
        private readonly IAreaInteresRepository _areaRepo;

        public ObservableCollection<UbicacionVisual> ListaUbicaciones { get; set; } = new();

        private UbicacionVisual? _ubicacionSeleccionada;
        public UbicacionVisual? UbicacionSeleccionada
        {
            get => _ubicacionSeleccionada;
            set
            {
                if (_ubicacionSeleccionada != value)
                {
                    _ubicacionSeleccionada = value;
                    OnPropertyChanged(nameof(UbicacionSeleccionada));

                    if (_ubicacionSeleccionada != null)
                    {
                        EnfocarEnUbicacion?.Invoke(_ubicacionSeleccionada.Latitud, _ubicacionSeleccionada.Longitud);
                    }
                }
            }
        }

        // ==========================================
        // PROPIEDADES PARA EL MÉTODO DE TRANSPORTE
        // ==========================================

        private string _modoTransporteSeleccionado = "Auto";
        public string ModoTransporteSeleccionado
        {
            get => _modoTransporteSeleccionado;
            set { _modoTransporteSeleccionado = value; OnPropertyChanged(nameof(ModoTransporteSeleccionado)); }
        }
        // ==========================================

        public ObservableCollection<string> MisAreasDeInteres { get; set; } = new();

        public ICommand AgregarUbicacionCommand { get; set; }
        public ICommand EditarUbicacionCommand { get; set; }
        public ICommand EliminarUbicacionCommand { get; set; }
        public ICommand AbrirCalculadoraRutaCommand { get; set; }
        public ICommand GuardarUbicacionTemporalCommand { get; set; }
        public ICommand DescartarUbicacionTemporalCommand { get; set; }
        public ICommand UnfocusCommand { get; set; }

        public IGeoService ServicioGeo => _geoService;
        public Action? MapaDebeActualizarse { get; set; }
        public Action<double, double>? EnfocarEnUbicacion { get; set; }
        public Action? BorrarPinTemporalDelMapa { get; set; }

        public static string? UbicacionSeleccionadaIdTemporal { get; set; }

        private bool _modoSeleccionActivo;
        public bool ModoSeleccionActivo
        {
            get => _modoSeleccionActivo;
            set
            {
                if (_modoSeleccionActivo != value)
                {
                    _modoSeleccionActivo = value;
                    OnPropertyChanged(nameof(ModoSeleccionActivo));
                }
            }
        }

        public Action<List<(double Latitud, double Longitud)>>? TrazarRutaEnMapa { get; set; }

        // ==========================================
        // PROPIEDADES PARA EL PANEL DE RUTAS FLOTANTE
        // ==========================================
        private bool _panelRutaVisible;
        public bool PanelRutaVisible
        {
            get => _panelRutaVisible;
            set { _panelRutaVisible = value; OnPropertyChanged(nameof(PanelRutaVisible)); }
        }

        private UbicacionVisual? _origenRutaSeleccionado;
        public UbicacionVisual? OrigenRutaSeleccionado
        {
            get => _origenRutaSeleccionado;
            set { _origenRutaSeleccionado = value; OnPropertyChanged(nameof(OrigenRutaSeleccionado)); }
        }

        private UbicacionVisual? _destinoRutaSeleccionado;
        public UbicacionVisual? DestinoRutaSeleccionado
        {
            get => _destinoRutaSeleccionado;
            set { _destinoRutaSeleccionado = value; OnPropertyChanged(nameof(DestinoRutaSeleccionado)); }
        }

        private string _infoRutaCalculada = string.Empty;
        public string InfoRutaCalculada
        {
            get => _infoRutaCalculada;
            set { _infoRutaCalculada = value; OnPropertyChanged(nameof(InfoRutaCalculada)); }
        }

        public ObservableCollection<string> ModosDeTransporteRuta { get; } = new() { "Auto", "Transporte Público", "Bicicleta", "Caminando" };
        public ICommand TogglePanelRutaCommand { get; set; }
        public ICommand CalcularRutaIntegradaCommand { get; set; }
        // ==========================================

        public UbicacionesViewModel(IGeoService geoService, IUbicacionRepository ubicacionRepo, ISesionService sesionService, IDialogService dialogService, IAreaInteresRepository areaRepo)
        {
            _geoService = geoService;
            _ubicacionRepo = ubicacionRepo;
            _sesionService = sesionService;
            _dialogService = dialogService;
            _areaRepo = areaRepo;

            EliminarUbicacionCommand = new RelayCommand<UbicacionVisual>(EliminarUbicacion);
            EditarUbicacionCommand = new RelayCommand<UbicacionVisual>(EditarUbicacion);
            AgregarUbicacionCommand = new RelayCommand<object>(AgregarUbicacion);
            AbrirCalculadoraRutaCommand = new RelayCommand<object>(AbrirCalculadoraRuta);
            GuardarUbicacionTemporalCommand = new RelayCommand<object>(GuardarUbicacionTemporal);
            DescartarUbicacionTemporalCommand = new RelayCommand<object>(DescartarUbicacionTemporal);
            UnfocusCommand = new RelayCommand<object>(UnfocusLocation);

            // Comandos del panel de rutas
            TogglePanelRutaCommand = new RelayCommand<object>(_ => PanelRutaVisible = !PanelRutaVisible);
            CalcularRutaIntegradaCommand = new RelayCommand<object>(EjecutarCalculoRuta);

            _ = CargarUbicacionesRealesAsync();
            _ = CargarAreasDeInteresRealesAsync();
        }

        private async void EjecutarCalculoRuta(object parametro)
        {
            if (OrigenRutaSeleccionado == null || DestinoRutaSeleccionado == null)
            {
                InfoRutaCalculada = "Selecciona origen y destino.";
                return;
            }

            InfoRutaCalculada = "Calculando ruta...";

            // 1. Traducir el modo de transporte de tu variable al inglés de Google
            string modoGoogle = "DRIVE"; // Valor por defecto (Auto)

            if (ModoTransporteSeleccionado != null)
            {
                string seleccionStr = ModoTransporteSeleccionado.ToString() ?? "";

                if (seleccionStr.Contains("Caminando")) modoGoogle = "WALK";
                else if (seleccionStr.Contains("Bicicleta")) modoGoogle = "BICYCLE";
                else if (seleccionStr.Contains("Público") || seleccionStr.Contains("Publico")) modoGoogle = "TRANSIT";
                else if (seleccionStr.Contains("Auto")) modoGoogle = "DRIVE";
            }

            try
            {
                // 2. AHORA SÍ PASAMOS LOS 5 PARÁMETROS (incluyendo modoGoogle al final)
                var respuesta = await _geoService.CalcularRutaGoogleAsync(
                    OrigenRutaSeleccionado.Latitud, OrigenRutaSeleccionado.Longitud,
                    DestinoRutaSeleccionado.Latitud, DestinoRutaSeleccionado.Longitud,
                    modoGoogle); // <--- ¡Aquí está el 5to parámetro que pedía el error!

                if (respuesta != null)
                {
                    InfoRutaCalculada = $"Tiempo: {respuesta.Value.Tiempo}  |  Distancia: {respuesta.Value.Distancia}";

                    var puntosRuta = DecodificarPolyline(respuesta.Value.Polyline);
                    TrazarRutaEnMapa?.Invoke(puntosRuta);
                }
                else
                {
                    InfoRutaCalculada = "No se pudo trazar una ruta entre estos puntos.";
                }
            }
            catch (Exception)
            {
                InfoRutaCalculada = "Error al calcular la ruta.";
            }
        }

        // Utilidad para convertir el texto de Google en puntos dibujables
        public static List<(double Latitud, double Longitud)> DecodificarPolyline(string polylineEnconded)
        {
            if (string.IsNullOrEmpty(polylineEnconded))
                return new List<(double, double)>();

            var polylineChars = polylineEnconded.ToCharArray();
            int index = 0, currentLat = 0, currentLng = 0;
            var puntos = new List<(double, double)>();

            while (index < polylineChars.Length)
            {
                int sum = 0, shifter = 0, b;
                do
                {
                    b = polylineChars[index++] - 63;
                    sum |= (b & 31) << shifter;
                    shifter += 5;
                } while (b >= 32);
                int dlat = (sum & 1) == 1 ? ~(sum >> 1) : (sum >> 1);
                currentLat += dlat;

                sum = 0; shifter = 0;
                do
                {
                    b = polylineChars[index++] - 63;
                    sum |= (b & 31) << shifter;
                    shifter += 5;
                } while (b >= 32);
                int dlng = (sum & 1) == 1 ? ~(sum >> 1) : (sum >> 1);
                currentLng += dlng;

                puntos.Add((currentLat / 100000.0, currentLng / 100000.0));
            }
            return puntos;
        }

        private async Task CargarAreasDeInteresRealesAsync()
        {
            if (_sesionService.UsuarioActual == null || string.IsNullOrEmpty(_sesionService.UsuarioActual.IdUsuario)) return;

            try
            {
                var areasDb = await _areaRepo.ObtenerAreasPorUsuario(_sesionService.UsuarioActual.IdUsuario);

                MisAreasDeInteres.Clear();
                MisAreasDeInteres.Add("General");

                foreach (var area in areasDb)
                {
                    if (!string.IsNullOrWhiteSpace(area.Nombre) && area.Nombre != "General")
                    {
                        MisAreasDeInteres.Add(area.Nombre);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar áreas de interés: {ex.Message}");
            }
        }

        private async Task CargarUbicacionesRealesAsync()
        {
            if (_sesionService.UsuarioActual == null) return;
            var ubicacionesDb = await _ubicacionRepo.ObtenerUbicacionesPorUsuario(_sesionService.UsuarioActual.IdUsuario);
            ListaUbicaciones.Clear();
            foreach (var ubi in ubicacionesDb)
            {
                ListaUbicaciones.Add(new UbicacionVisual
                {
                    IdUbicacion = ubi.IdUbicacion,
                    Nombre = ubi.Nombre,
                    AreaInteres = ubi.AreaInteres,
                    DireccionExacta = ubi.DireccionExacta,
                    ColorHex = ubi.ColorHex,
                    TransportePreferido = ubi.TransportePreferido,
                    Latitud = ubi.Latitud,
                    Longitud = ubi.Longitud
                });
            }

            MapaDebeActualizarse?.Invoke();

            if (!string.IsNullOrEmpty(UbicacionSeleccionadaIdTemporal))
            {
                var ubiTarget = ListaUbicaciones.FirstOrDefault(u => u.IdUbicacion == UbicacionSeleccionadaIdTemporal);
                if (ubiTarget != null)
                {
                    UbicacionSeleccionada = ubiTarget;
                }
                UbicacionSeleccionadaIdTemporal = null;
            }
        }

        private async void AgregarUbicacion(object parametro)
        {
            var resultado = await _dialogService.ShowAddLocationDialog(_geoService, MisAreasDeInteres);
            if (resultado != null && !string.IsNullOrWhiteSpace(resultado.Direccion))
            {
                var geo = await _geoService.ValidarDireccionAsync(resultado.Nombre, resultado.Direccion);
                if (geo != null && _sesionService.UsuarioActual != null)
                {
                    var nuevaUbicacionDb = new UbicacionGuardada
                    {
                        IdUsuario = _sesionService.UsuarioActual.IdUsuario,
                        Nombre = resultado.Nombre,
                        AreaInteres = resultado.AreaInteres,
                        DireccionExacta = resultado.Direccion,
                        ColorHex = resultado.ColorHex,
                        TransportePreferido = resultado.Transporte,
                        Latitud = geo.Latitud,
                        Longitud = geo.Longitud
                    };
                    await _ubicacionRepo.CrearUbicacion(nuevaUbicacionDb);
                    ListaUbicaciones.Add(new UbicacionVisual
                    {
                        IdUbicacion = nuevaUbicacionDb.IdUbicacion,
                        Nombre = nuevaUbicacionDb.Nombre,
                        AreaInteres = nuevaUbicacionDb.AreaInteres,
                        DireccionExacta = nuevaUbicacionDb.DireccionExacta,
                        ColorHex = nuevaUbicacionDb.ColorHex,
                        TransportePreferido = nuevaUbicacionDb.TransportePreferido,
                        Latitud = nuevaUbicacionDb.Latitud,
                        Longitud = nuevaUbicacionDb.Longitud
                    });
                    MapaDebeActualizarse?.Invoke();
                }
            }
        }

        private async void EditarUbicacion(UbicacionVisual ubicacionAEditar)
        {
            if (ubicacionAEditar == null) return;
            var resultado = await _dialogService.ShowEditLocationDialog(
                _geoService, MisAreasDeInteres,
                ubicacionAEditar.Nombre!, ubicacionAEditar.DireccionExacta!,
                ubicacionAEditar.AreaInteres ?? "General", ubicacionAEditar.ColorHex!, ubicacionAEditar.TransportePreferido!);

            if (resultado != null && _sesionService.UsuarioActual != null)
            {
                var ubicacionDb = new UbicacionGuardada
                {
                    IdUbicacion = ubicacionAEditar.IdUbicacion!,
                    IdUsuario = _sesionService.UsuarioActual.IdUsuario,
                    Nombre = resultado.Nombre,
                    AreaInteres = resultado.AreaInteres,
                    DireccionExacta = resultado.Direccion,
                    ColorHex = resultado.ColorHex,
                    TransportePreferido = resultado.Transporte,
                    Latitud = ubicacionAEditar.Latitud,
                    Longitud = ubicacionAEditar.Longitud
                };

                await _ubicacionRepo.ActualizarUbicacion(ubicacionAEditar.IdUbicacion!, ubicacionDb);

                int index = ListaUbicaciones.IndexOf(ubicacionAEditar);
                if (index >= 0)
                {
                    var actualizada = new UbicacionVisual
                    {
                        IdUbicacion = ubicacionDb.IdUbicacion,
                        Nombre = ubicacionDb.Nombre,
                        AreaInteres = ubicacionDb.AreaInteres,
                        DireccionExacta = ubicacionDb.DireccionExacta,
                        ColorHex = ubicacionDb.ColorHex,
                        TransportePreferido = ubicacionDb.TransportePreferido,
                        Latitud = ubicacionDb.Latitud,
                        Longitud = ubicacionDb.Longitud,
                        EsTemporal = false
                    };

                    ListaUbicaciones[index] = actualizada;

                    if (UbicacionSeleccionada == ubicacionAEditar)
                        UbicacionSeleccionada = actualizada;
                }

                MapaDebeActualizarse?.Invoke();
            }
        }

        private async void EliminarUbicacion(UbicacionVisual ubicacionAEliminar)
        {
            if (ubicacionAEliminar == null || ubicacionAEliminar.IdUbicacion == null) return;
            await _ubicacionRepo.EliminarUbicacion(ubicacionAEliminar.IdUbicacion);
            ListaUbicaciones.Remove(ubicacionAEliminar);
            MapaDebeActualizarse?.Invoke();
        }

        private async void AbrirCalculadoraRuta(object parametro)
        {
            await _dialogService.ShowRouteCalculatorDialog(_geoService, ListaUbicaciones);
        }

        private async void GuardarUbicacionTemporal(object parametro)
        {
            if (UbicacionSeleccionada == null || !UbicacionSeleccionada.EsTemporal) return;

            var resultado = await _dialogService.ShowEditLocationDialog(
                _geoService, MisAreasDeInteres,
                "", UbicacionSeleccionada.DireccionExacta ?? "", "General", "#10b981", "Auto");

            if (resultado != null && !string.IsNullOrWhiteSpace(resultado.Direccion))
            {
                if (_sesionService.UsuarioActual != null)
                {
                    var nuevaUbicacionDb = new UbicacionGuardada
                    {
                        IdUsuario = _sesionService.UsuarioActual.IdUsuario,
                        Nombre = resultado.Nombre,
                        AreaInteres = resultado.AreaInteres,
                        DireccionExacta = resultado.Direccion,
                        ColorHex = resultado.ColorHex,
                        TransportePreferido = resultado.Transporte,
                        Latitud = UbicacionSeleccionada.Latitud,
                        Longitud = UbicacionSeleccionada.Longitud
                    };

                    await _ubicacionRepo.CrearUbicacion(nuevaUbicacionDb);

                    var nuevaVisual = new UbicacionVisual
                    {
                        IdUbicacion = nuevaUbicacionDb.IdUbicacion,
                        Nombre = nuevaUbicacionDb.Nombre,
                        AreaInteres = nuevaUbicacionDb.AreaInteres,
                        DireccionExacta = nuevaUbicacionDb.DireccionExacta,
                        ColorHex = nuevaUbicacionDb.ColorHex,
                        TransportePreferido = nuevaUbicacionDb.TransportePreferido,
                        Latitud = nuevaUbicacionDb.Latitud,
                        Longitud = nuevaUbicacionDb.Longitud,
                        EsTemporal = false
                    };

                    var temporal = ListaUbicaciones.FirstOrDefault(u => u.EsTemporal);
                    if (temporal != null) ListaUbicaciones.Remove(temporal);

                    ListaUbicaciones.Add(nuevaVisual);
                    UbicacionSeleccionada = nuevaVisual;

                    BorrarPinTemporalDelMapa?.Invoke();
                    MapaDebeActualizarse?.Invoke();
                }
            }
        }

        private void DescartarUbicacionTemporal(object parametro)
        {
            UbicacionSeleccionada = null;
            BorrarPinTemporalDelMapa?.Invoke();
        }

        private void UnfocusLocation(object? parameter)
        {
            UbicacionSeleccionada = null;
            BorrarPinTemporalDelMapa?.Invoke();
        }
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        public RelayCommand(Action<T> execute) => _execute = execute;
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _execute((T)parameter!);
        public event EventHandler? CanExecuteChanged;
    }

    public class UbicacionVisual
    {
        public string? IdUbicacion { get; set; }
        public string? Nombre { get; set; }
        public string? AreaInteres { get; set; }
        public string? DireccionExacta { get; set; }
        public bool EsTemporal { get; set; }
        public string? ColorHex { get; set; }
        public string? UltimaVisitaFormateada { get; set; }
        public string? TransportePreferido { get; set; }
        public double Latitud { get; set; }
        public double Longitud { get; set; }
    }

}
#pragma warning restore CS0067