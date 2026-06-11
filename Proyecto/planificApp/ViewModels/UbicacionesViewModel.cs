using PlanificApp.Models;
using PlanificApp.Models.Services.Interfaces;
using PlanificApp.Models.Repositories.Interfaces;
using PlanificApp.Models.Services;
using planificApp.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        public UbicacionesViewModel(IGeoService geoService, IUbicacionRepository ubicacionRepo, ISesionService sesionService, IDialogService dialogService)
        {
            _geoService = geoService;
            _ubicacionRepo = ubicacionRepo;
            _sesionService = sesionService;
            _dialogService = dialogService;

            MisAreasDeInteres.Add("General");
            MisAreasDeInteres.Add("Work Work Work Work");
            MisAreasDeInteres.Add("Hogar y Familia");
            MisAreasDeInteres.Add("Salud y Deporte");

            EliminarUbicacionCommand = new RelayCommand<UbicacionVisual>(EliminarUbicacion);
            EditarUbicacionCommand = new RelayCommand<UbicacionVisual>(EditarUbicacion);
            AgregarUbicacionCommand = new RelayCommand<object>(AgregarUbicacion);
            AbrirCalculadoraRutaCommand = new RelayCommand<object>(AbrirCalculadoraRuta);
            GuardarUbicacionTemporalCommand = new RelayCommand<object>(GuardarUbicacionTemporal);
            DescartarUbicacionTemporalCommand = new RelayCommand<object>(DescartarUbicacionTemporal);
            UnfocusCommand = new RelayCommand<object>(UnfocusLocation);

            _ = CargarUbicacionesRealesAsync();
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
                ubicacionAEditar.AreaInteres!, ubicacionAEditar.ColorHex!, ubicacionAEditar.TransportePreferido!);
            if (resultado != null && _sesionService.UsuarioActual != null)
            {
                ubicacionAEditar.Nombre = resultado.Nombre;
                ubicacionAEditar.AreaInteres = resultado.AreaInteres;
                ubicacionAEditar.ColorHex = resultado.ColorHex;
                ubicacionAEditar.TransportePreferido = resultado.Transporte;
                var ubicacionDb = new UbicacionGuardada
                {
                    IdUbicacion = ubicacionAEditar.IdUbicacion!,
                    IdUsuario = _sesionService.UsuarioActual.IdUsuario,
                    Nombre = resultado.Nombre,
                    AreaInteres = resultado.AreaInteres,
                    DireccionExacta = ubicacionAEditar.DireccionExacta!,
                    ColorHex = resultado.ColorHex,
                    TransportePreferido = resultado.Transporte,
                    Latitud = ubicacionAEditar.Latitud,
                    Longitud = ubicacionAEditar.Longitud
                };
                await _ubicacionRepo.ActualizarUbicacion(ubicacionAEditar.IdUbicacion!, ubicacionDb);
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
                "", UbicacionSeleccionada.AreaInteres ?? "", "General", "#10b981", "Auto");

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
                        Longitud = nuevaUbicacionDb.Longitud
                    };

                    ListaUbicaciones.Add(nuevaVisual);
                    UbicacionSeleccionada = nuevaVisual;
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

#pragma warning disable CS0067

}
#pragma warning restore CS0067
