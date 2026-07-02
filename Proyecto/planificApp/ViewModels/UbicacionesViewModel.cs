using planificApp.Services;
using PlanificApp.Models;
using PlanificApp.Models.Repositories.Interfaces;
using PlanificApp.Models.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.ComponentModel;
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

        // Áreas del usuario, cacheadas para resolver qué áreas usan cada ubicación (vía UbicacionPred).
        private List<AreaInteres> _areasUsuario = new();

        private UbicacionVisual? _ubicacionSeleccionada;
        public UbicacionVisual? UbicacionSeleccionada
        {
            get => _ubicacionSeleccionada;
            set
            {
                if (_ubicacionSeleccionada != null) _ubicacionSeleccionada.IsSelected = false;
                _ubicacionSeleccionada = value;
                OnPropertyChanged(nameof(UbicacionSeleccionada));
                if (_ubicacionSeleccionada != null)
                {
                    _ubicacionSeleccionada.IsSelected = true;
                    EnfocarEnUbicacion?.Invoke(_ubicacionSeleccionada.Latitud, _ubicacionSeleccionada.Longitud);
                }
            }
        }

        public ObservableCollection<string> ModosDeTransporteRuta { get; } = new() { "Auto", "Transporte Público", "Bicicleta", "Caminando" };

        private string _modoTransporteSeleccionado = "Auto";
        public string ModoTransporteSeleccionado
        {
            get => _modoTransporteSeleccionado;
            set { _modoTransporteSeleccionado = value; OnPropertyChanged(nameof(ModoTransporteSeleccionado)); }
        }

        private string _transporteRutaLabel = string.Empty;
        public string TransporteRutaLabel
        {
            get => _transporteRutaLabel;
            set { _transporteRutaLabel = value; OnPropertyChanged(nameof(TransporteRutaLabel)); }
        }

        public ICommand SeleccionarUbicacionCommand { get; set; }
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

        // Centro inicial del mapa cuando el usuario aún no tiene ubicaciones guardadas
        // (se resuelve geocodificando su comuna del registro). La View decide el zoom.
        public Action<double, double>? CentrarEnUbicacionInicial { get; set; }

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
        public Action? LimpiarRutaDelMapa { get; set; }

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
            set
            {
                _origenRutaSeleccionado = value;
                OnPropertyChanged(nameof(OrigenRutaSeleccionado));
                ActualizarTransporteRuta();
            }
        }

        private UbicacionVisual? _destinoRutaSeleccionado;
        public UbicacionVisual? DestinoRutaSeleccionado
        {
            get => _destinoRutaSeleccionado;
            set
            {
                _destinoRutaSeleccionado = value;
                OnPropertyChanged(nameof(DestinoRutaSeleccionado));
                ActualizarTransporteRuta();
            }
        }

        private string _infoRutaCalculada = string.Empty;
        public string InfoRutaCalculada
        {
            get => _infoRutaCalculada;
            set { _infoRutaCalculada = value; OnPropertyChanged(nameof(InfoRutaCalculada)); }
        }

        public ICommand TogglePanelRutaCommand { get; set; }
        public ICommand CalcularRutaIntegradaCommand { get; set; }
        public ICommand LimpiarRutaCommand { get; set; }

        private void ActualizarTransporteRuta()
        {
            TransporteRutaLabel = DestinoRutaSeleccionado?.TransportePreferido
                ?? OrigenRutaSeleccionado?.TransportePreferido
                ?? "Auto";
        }

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
            SeleccionarUbicacionCommand = new RelayCommand<UbicacionVisual>(u => UbicacionSeleccionada = u);
            UnfocusCommand = new RelayCommand<object>(UnfocusLocation);

            TogglePanelRutaCommand = new RelayCommand<object>(_ =>
            {
                PanelRutaVisible = !PanelRutaVisible;
                if (!PanelRutaVisible)
                {
                    InfoRutaCalculada = string.Empty;
                    OrigenRutaSeleccionado = null;
                    DestinoRutaSeleccionado = null;
                    LimpiarRutaDelMapa?.Invoke();
                }
            });
            CalcularRutaIntegradaCommand = new RelayCommand<object>(EjecutarCalculoRuta);
            LimpiarRutaCommand = new RelayCommand<object>(LimpiarRuta);

            _ = CargarUbicacionesRealesAsync();
        }

        private async void EjecutarCalculoRuta(object parametro)
        {
            if (OrigenRutaSeleccionado == null || DestinoRutaSeleccionado == null)
            {
                InfoRutaCalculada = "Selecciona origen y destino.";
                return;
            }

            InfoRutaCalculada = "Calculando ruta...";

            string transporte = DestinoRutaSeleccionado.TransportePreferido
                ?? OrigenRutaSeleccionado.TransportePreferido
                ?? "Auto";
            string modoGoogle = transporte switch
            {
                "A pie" or "Caminando" => "WALK",
                "Bus" or "Transporte Público" => "TRANSIT",
                "Bicicleta" => "WALK",
                _ => "DRIVE"
            };

            try
            {
                var respuesta = await _geoService.CalcularRutaGoogleAsync(
                    OrigenRutaSeleccionado.Latitud, OrigenRutaSeleccionado.Longitud,
                    DestinoRutaSeleccionado.Latitud, DestinoRutaSeleccionado.Longitud,
                    modoGoogle);

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

        private void LimpiarRuta(object parametro)
        {
            InfoRutaCalculada = string.Empty;
            LimpiarRutaDelMapa?.Invoke();
        }

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

        // Para cada ubicación, calcula qué áreas la usan como su ubicación predeterminada
        // (AreaInteres.UbicacionPred == Ubicacion.Nombre). Es la relación inversa, en modo lectura.
        public void RecalcularAreasDeUso()
        {
            foreach (var ubi in ListaUbicaciones)
            {
                ubi.AreasQueUsan.Clear();

                if (!string.IsNullOrWhiteSpace(ubi.Nombre))
                {
                    var areas = _areasUsuario
                        .Where(a => !string.IsNullOrWhiteSpace(a.UbicacionPred)
                                 && string.Equals(a.UbicacionPred, ubi.Nombre, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(a => a.Nombre);

                    foreach (var a in areas)
                        ubi.AreasQueUsan.Add(new AreaUso { Nombre = a.Nombre, ColorHex = a.ColorHex });
                }

                ubi.TieneAreasQueUsan = ubi.AreasQueUsan.Count > 0;
            }
        }

        private async Task CargarUbicacionesRealesAsync()
        {
            if (_sesionService.UsuarioActual == null) return;
            var ubicacionesDb = await _ubicacionRepo.ObtenerUbicacionesPorUsuario(_sesionService.UsuarioActual.IdUsuario);

            // Cacheamos las áreas para resolver la relación inversa (qué áreas usan cada ubicación).
            _areasUsuario = await _areaRepo.ObtenerAreasPorUsuario(_sesionService.UsuarioActual.IdUsuario);

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

            RecalcularAreasDeUso();
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
            else
            {
                // El mapa siempre arranca centrado en la comuna del usuario (la del registro).
                // Las ubicaciones guardadas se ven como pines; el usuario hace click en una para enfocarla.
                await CentrarMapaEnComunaUsuarioAsync();
            }
        }

        // Geocodifica la comuna/región que el usuario indicó al registrarse y centra el mapa ahí.
        // Si no hay dato o la geocodificación falla, la View mantiene su centro por defecto.
        private async Task CentrarMapaEnComunaUsuarioAsync()
        {
            var ubiTexto = _sesionService.UsuarioActual?.Ubicacion;
            if (string.IsNullOrWhiteSpace(ubiTexto)) return;

            var geo = await _geoService.ValidarDireccionAsync(ubiTexto, $"{ubiTexto}, Chile");
            if (geo != null)
                CentrarEnUbicacionInicial?.Invoke(geo.Latitud, geo.Longitud);
        }

        private async void AgregarUbicacion(object parametro)
        {
            var resultado = await _dialogService.ShowAddLocationDialog(_geoService);
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

                    RecalcularAreasDeUso();
                    MapaDebeActualizarse?.Invoke();
                }
            }
        }

        private async void EditarUbicacion(UbicacionVisual ubicacionAEditar)
        {
            if (ubicacionAEditar == null) return;
            var resultado = await _dialogService.ShowEditLocationDialog(
                _geoService,
                ubicacionAEditar.Nombre!, ubicacionAEditar.DireccionExacta!,
                ubicacionAEditar.AreaInteres ?? "General", ubicacionAEditar.ColorHex!, ubicacionAEditar.TransportePreferido!);

            if (resultado != null && _sesionService.UsuarioActual != null)
            {
                // Si cambió la dirección, re-geocodificamos para mover el pin.
                // Si la geocodificación falla, conservamos las coordenadas previas.
                double lat = ubicacionAEditar.Latitud;
                double lon = ubicacionAEditar.Longitud;
                if (!string.IsNullOrWhiteSpace(resultado.Direccion) &&
                    resultado.Direccion != ubicacionAEditar.DireccionExacta)
                {
                    var geo = await _geoService.ValidarDireccionAsync(resultado.Nombre, resultado.Direccion);
                    if (geo != null) { lat = geo.Latitud; lon = geo.Longitud; }
                }

                var ubicacionDb = new UbicacionGuardada
                {
                    IdUbicacion = ubicacionAEditar.IdUbicacion!,
                    IdUsuario = _sesionService.UsuarioActual.IdUsuario,
                    Nombre = resultado.Nombre,
                    AreaInteres = resultado.AreaInteres,
                    DireccionExacta = resultado.Direccion,
                    ColorHex = resultado.ColorHex,
                    TransportePreferido = resultado.Transporte,
                    Latitud = lat,
                    Longitud = lon
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

                RecalcularAreasDeUso();
                MapaDebeActualizarse?.Invoke();
            }
        }

        private async void EliminarUbicacion(UbicacionVisual ubicacionAEliminar)
        {
            if (ubicacionAEliminar == null || ubicacionAEliminar.IdUbicacion == null) return;
            await _ubicacionRepo.EliminarUbicacion(ubicacionAEliminar.IdUbicacion);
            ListaUbicaciones.Remove(ubicacionAEliminar);

            RecalcularAreasDeUso();
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
                _geoService,
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

                    RecalcularAreasDeUso();
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

    // Un área que usa una ubicación como su ubicación predeterminada (relación inversa, solo lectura).
    public class AreaUso
    {
        public string Nombre { get; set; } = string.Empty;
        public string ColorHex { get; set; } = "#a78bfa";
    }

    public class UbicacionVisual : INotifyPropertyChanged
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

        // Áreas que usan esta ubicación como su ubicación predeterminada (UbicacionPred).
        public ObservableCollection<AreaUso> AreasQueUsan { get; } = new();

        private bool _tieneAreasQueUsan;
        public bool TieneAreasQueUsan
        {
            get => _tieneAreasQueUsan;
            set { _tieneAreasQueUsan = value; OnPropertyChanged(nameof(TieneAreasQueUsan)); }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}