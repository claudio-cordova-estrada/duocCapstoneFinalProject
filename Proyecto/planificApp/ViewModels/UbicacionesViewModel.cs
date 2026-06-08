using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using planificApp.Views;
using PlanificApp.Models;
using PlanificApp.Models.Services;
using PlanificApp.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace planificApp.ViewModels
{
    public class UbicacionesViewModel : PageViewModel
    {
        private readonly GeoService _geoService;
        private readonly MongoService _mongoService;
        private readonly SesionService _sesionService;

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

        // NUEVO COMANDO PARA DESCARTAR
        public ICommand DescartarUbicacionTemporalCommand { get; set; }

        public GeoService ServicioGeo => _geoService;
        public Action? MapaDebeActualizarse { get; set; }
        public Action<double, double>? EnfocarEnUbicacion { get; set; }

        // NUEVA ACCIÓN PARA ELIMINAR EL PIN
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

        public UbicacionesViewModel(GeoService geoService, MongoService mongoService, SesionService sesionService)
        {
            _geoService = geoService;
            _mongoService = mongoService;
            _sesionService = sesionService;

            MisAreasDeInteres.Add("General");
            MisAreasDeInteres.Add("Work Work Work Work");
            MisAreasDeInteres.Add("Hogar y Familia");
            MisAreasDeInteres.Add("Salud y Deporte");

            EliminarUbicacionCommand = new RelayCommand<UbicacionVisual>(EliminarUbicacion);
            EditarUbicacionCommand = new RelayCommand<UbicacionVisual>(EditarUbicacion);
            AgregarUbicacionCommand = new RelayCommand<object>(AgregarUbicacion);
            AbrirCalculadoraRutaCommand = new RelayCommand<object>(AbrirCalculadoraRuta);
            GuardarUbicacionTemporalCommand = new RelayCommand<object>(GuardarUbicacionTemporal);

            // INICIALIZAMOS EL COMANDO DESCARTAR
            DescartarUbicacionTemporalCommand = new RelayCommand<object>(DescartarUbicacionTemporal);

            _ = CargarUbicacionesRealesAsync();
        }

        private async Task CargarUbicacionesRealesAsync()
        {
            if (_sesionService.UsuarioActual == null) return;
            var ubicacionesDb = await _mongoService.ObtenerUbicacionesPorUsuario(_sesionService.UsuarioActual.IdUsuario);
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
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var ventana = new AddLocationWindow(_geoService);
                ventana.SetAreasDeInteres(MisAreasDeInteres);
                var resultado = await ventana.ShowDialog<LocationFormData>(desktop.MainWindow!);
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
                        await _mongoService.CrearUbicacion(nuevaUbicacionDb);
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
        }

        private async void EditarUbicacion(UbicacionVisual ubicacionAEditar)
        {
            if (ubicacionAEditar == null) return;
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var ventana = new AddLocationWindow(_geoService);
                ventana.SetAreasDeInteres(MisAreasDeInteres);
                ventana.SetEditMode(ubicacionAEditar.Nombre!, ubicacionAEditar.DireccionExacta!, ubicacionAEditar.AreaInteres!, ubicacionAEditar.ColorHex!, ubicacionAEditar.TransportePreferido!);
                var resultado = await ventana.ShowDialog<LocationFormData>(desktop.MainWindow!);
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
                    await _mongoService.ActualizarUbicacion(ubicacionAEditar.IdUbicacion!, ubicacionDb);
                    MapaDebeActualizarse?.Invoke();
                }
            }
        }

        private async void EliminarUbicacion(UbicacionVisual ubicacionAEliminar)
        {
            if (ubicacionAEliminar == null || ubicacionAEliminar.IdUbicacion == null) return;
            await _mongoService.EliminarUbicacion(ubicacionAEliminar.IdUbicacion);
            ListaUbicaciones.Remove(ubicacionAEliminar);
            MapaDebeActualizarse?.Invoke();
        }

        private async void AbrirCalculadoraRuta(object parametro)
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var ventana = new CalcularRutaWindow(_geoService, ListaUbicaciones);
                await ventana.ShowDialog(desktop.MainWindow!);
            }
        }

        private async void GuardarUbicacionTemporal(object parametro)
        {
            if (UbicacionSeleccionada == null || !UbicacionSeleccionada.EsTemporal) return;

            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                var ventana = new AddLocationWindow(_geoService);
                ventana.SetAreasDeInteres(MisAreasDeInteres);

                ventana.SetEditMode(
                    "",
                    UbicacionSeleccionada.AreaInteres ?? "",
                    "General",
                    "#10b981",
                    "Auto"
                );

                var resultado = await ventana.ShowDialog<LocationFormData>(desktop.MainWindow!);

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

                        await _mongoService.CrearUbicacion(nuevaUbicacionDb);

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
        }

        // NUEVO MÉTODO PARA DESCARTAR
        private void DescartarUbicacionTemporal(object parametro)
        {
            UbicacionSeleccionada = null;
            BorrarPinTemporalDelMapa?.Invoke();
        }

    } // <--- LLAVE CRÍTICA QUE CIERRA LA CLASE UbicacionesViewModel

    public class UbicacionVisual
    {
        public string? IdUbicacion { get; set; }
        public string? Nombre { get; set; }
        public string? AreaInteres { get; set; }
        public string? DireccionExacta { get; set; }
        public bool EsTemporal { get; set; } = false;
        public string? ColorHex { get; set; }
        public string? TransportePreferido { get; set; }
        public string? UltimaVisitaFormateada { get; set; }
        public double Latitud { get; set; }
        public double Longitud { get; set; }
        public string? Categoria { get; set; }
    }

#pragma warning disable CS0067
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        public RelayCommand(Action<T> execute) => _execute = execute;
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _execute((T)parameter!);
        public event EventHandler? CanExecuteChanged;
    }
#pragma warning restore CS0067

}