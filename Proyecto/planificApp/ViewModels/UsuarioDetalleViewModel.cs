using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using planificApp.Data;
using planificApp.Services;
using PlanificApp.Models;
using PlanificApp.Models.Repositories.Interfaces;
using PlanificApp.Models.Services;
using PlanificApp.Models.Services.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;

namespace planificApp.ViewModels;

public class MesUI
{
    public int Numero { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public override string ToString() => Nombre;
}

public partial class UsuarioDetalleViewModel : PageViewModel
{
    private readonly INavigationService _navigationService;
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IRegionesService _regionesService;
    private readonly ITareaRepository _tareaRepo;
    private readonly IMetricasService _metricasService;
    private readonly IAreaInteresRepository _areaRepo;
    private readonly IUbicacionRepository _ubicacionRepo;
    private readonly IBloqueAreaInteresScheduleRepository _bloqueRepo;
    private readonly IDialogService _dialogService;
    private readonly ISesionService _sesion;
    private readonly IGeneracionRepository _generacionRepo;
    private Usuario? _usuarioOriginal;

    // --- DATOS PERSONALES ---
    [ObservableProperty] private string _nombre = string.Empty;
    [ObservableProperty] private string _correo = string.Empty;
    [ObservableProperty] private string _idUsuario = string.Empty;
    [ObservableProperty] private string _edad = "0";
    [ObservableProperty] private DateTime? _fechaNacimientoDate;
    [ObservableProperty] private string _mesesActivo = string.Empty;
    [ObservableProperty] private string _fechaRegistro = string.Empty;


    // --- UBICACIÓN ---
    [ObservableProperty] private ObservableCollection<string> _regiones = new();
    [ObservableProperty] private string _regionSeleccionada = string.Empty;
    [ObservableProperty] private ObservableCollection<string> _comunas = new();
    [ObservableProperty] private string _comunaSeleccionada = string.Empty;

    // --- ESTADO Y UI ---
    [ObservableProperty] private bool _esActivo = true;
    [ObservableProperty] private bool _mostrarMensajeExito = false;
    [ObservableProperty] private bool _mostrarError = false;
    [ObservableProperty] private string _errorValidacion = string.Empty;

    // ⚠️ TEMP DEV: resultado del generador de datos de prueba. Quitar antes de entregar.
    [ObservableProperty] private string _seedResultado = string.Empty;

    public string TextoBotonEstado => EsActivo ? "Desactivar usuario" : "Reactivar Usuario";
    public string ColorTextoEstado => EsActivo ? "#ef4444" : "#38bdf8";
    public string StatusBadgeText => EsActivo ? "ACTIVO" : "INACTIVO";
    public string ColorBadgeEstado => EsActivo ? "#4ade80" : "#ef4444";

    // --- MÉTRICAS ---
    [ObservableProperty] private int _yearActual = DateTime.Now.Year;
    [ObservableProperty] private int _tareasCreadas = 0;
    [ObservableProperty] private int _tareasCompletadas = 0;
    [ObservableProperty] private int _generacionesRealizadas = 0;

    // Colección observable para renderizar la barra de meses interactiva
    public ObservableCollection<MesUI> ListaMeses { get; } = new()
    {
        new MesUI { Numero = 1, Nombre = "Ene" }, new MesUI { Numero = 2, Nombre = "Feb" },
        new MesUI { Numero = 3, Nombre = "Mar" }, new MesUI { Numero = 4, Nombre = "Abr" },
        new MesUI { Numero = 5, Nombre = "May" }, new MesUI { Numero = 6, Nombre = "Jun" },
        new MesUI { Numero = 7, Nombre = "Jul" }, new MesUI { Numero = 8, Nombre = "Ago" },
        new MesUI { Numero = 9, Nombre = "Sep" }, new MesUI { Numero = 10, Nombre = "Oct" },
        new MesUI { Numero = 11, Nombre = "Nov" }, new MesUI { Numero = 12, Nombre = "Dic" }
    };
    [ObservableProperty] private MesUI? _mesSeleccionado;

    public UsuarioDetalleViewModel(INavigationService navigationService, IUsuarioRepository usuarioRepo, IRegionesService regionesService, ITareaRepository tareaRepo, IMetricasService metricasService, IAreaInteresRepository areaRepo, IUbicacionRepository ubicacionRepo, IBloqueAreaInteresScheduleRepository bloqueRepo, IDialogService dialogService, ISesionService sesion, IGeneracionRepository generacionRepo)
    {
        PageName = ApplicationPageNames.AdminUsuarioDetalle;
        _navigationService = navigationService;
        _usuarioRepo = usuarioRepo;
        _regionesService = regionesService;
        _tareaRepo = tareaRepo;
        _metricasService = metricasService;
        _areaRepo = areaRepo;
        _ubicacionRepo = ubicacionRepo;
        _bloqueRepo = bloqueRepo;
        _dialogService = dialogService;
        _sesion = sesion;
        _generacionRepo = generacionRepo;

        // Selección automática del mes en curso del sistema
        MesSeleccionado = ListaMeses.FirstOrDefault(m => m.Numero == DateTime.Now.Month) ?? ListaMeses[0];
        _ = CargarRegionesGeneralesAsync();
    }

    private async Task CargarRegionesGeneralesAsync()
    {
        var regionesDb = await _regionesService.ObtenerRegionesAsync();
        Regiones.Clear();
        foreach (var r in regionesDb) Regiones.Add(r);
    }

    partial void OnRegionSeleccionadaChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _ = CargarComunasAsync(value);
        }
    }

    private async Task CargarComunasAsync(string region)
    {
        var comunasDb = await _regionesService.ObtenerComunasPorRegionAsync(region);
        Comunas.Clear();
        foreach (var c in comunasDb) Comunas.Add(c);
    }

    // Dispara la recarga de métricas asíncronas de la base de datos al cambiar el mes seleccionado
    partial void OnMesSeleccionadoChanged(MesUI? value)
    {
        _ = CargarMetricasDelAnioActualAsync();
    }

    // Recalcula la edad cuando cambia la fecha de nacimiento (carga inicial o el
    // CalendarDatePicker de la vista), para que "EDAD" no quede siempre en 0.
    partial void OnFechaNacimientoDateChanged(DateTime? value)
    {
        RecalcularEdad();
    }

    private void RecalcularEdad()
    {
        if (!FechaNacimientoDate.HasValue)
        {
            Edad = "—";
            return;
        }

        var nacimiento = FechaNacimientoDate.Value.Date;
        var hoy = DateTime.Today;
        int edad = hoy.Year - nacimiento.Year;
        // Resta 1 si todavía no cumplió años este año.
        if (nacimiento > hoy.AddYears(-edad)) edad--;

        Edad = edad < 0 ? "—" : $"{edad} años";
    }

    public async void CargarDatosUsuario(Usuario usuario)
    {
        if (usuario == null) return;
        _usuarioOriginal = usuario;

        Nombre = usuario.NombreCompleto ?? string.Empty;
        Correo = usuario.Correo ?? string.Empty;
        IdUsuario = usuario.IdUsuario ?? "ID_DESC";
        EsActivo = usuario.EstaActivo;

        var hoy = DateTime.Today;
        if (usuario.FecNacimiento != DateTime.MinValue)
        {
            FechaNacimientoDate = usuario.FecNacimiento;
        }

        if (usuario.FecCreacion != DateTime.MinValue)
        {
            FechaRegistro = usuario.FecCreacion.ToString("dd MMM yyyy");
            int meses = ((hoy.Year - usuario.FecCreacion.Year) * 12) + hoy.Month - usuario.FecCreacion.Month;
            MesesActivo = $"{meses} meses";
        }

        if (!string.IsNullOrEmpty(usuario.Ubicacion) && usuario.Ubicacion.Contains(","))
        {
            var partes = usuario.Ubicacion.Split(',');
            if (partes.Length >= 2)
            {
                string comunaGuardada = partes[0].Trim();
                string regionGuardada = partes[1].Trim();

                RegionSeleccionada = regionGuardada;
                await CargarComunasAsync(regionGuardada);
                ComunaSeleccionada = comunaGuardada;
            }
        }

        ActualizarTextosVisuales();
        await CargarMetricasDelAnioActualAsync();
    }

    [RelayCommand]
    private void ToggleEstadoUsuario()
    {
        EsActivo = !EsActivo;
        ActualizarTextosVisuales();
    }

    // Elimina al usuario Y EN CASCADA todo lo que creó (tareas, áreas, ubicaciones y
    // bloques de calendario). Es irreversible → pide confirmación antes de tocar la BD.
    [RelayCommand]
    private async Task EliminarUsuario()
    {
        MostrarError = false;
        MostrarMensajeExito = false;

        if (_usuarioOriginal == null || string.IsNullOrEmpty(_usuarioOriginal.IdUsuario))
            return;

        // Barrera de seguridad: el admin logueado no puede borrarse a sí mismo.
        if (_sesion.UsuarioActual != null && _sesion.UsuarioActual.IdUsuario == _usuarioOriginal.IdUsuario)
        {
            ErrorValidacion = "No puedes eliminar tu propia cuenta mientras la sesión está iniciada.";
            MostrarError = true;
            return;
        }

        // Confirmación explícita: acción destructiva e irreversible.
        var confirmado = await _dialogService.ShowConfirmDeleteUsuarioDialog(
            string.IsNullOrWhiteSpace(Nombre) ? "este usuario" : Nombre);
        if (!confirmado) return;

        var idUsuario = _usuarioOriginal.IdUsuario;

        try
        {
            // Orden: primero los datos dependientes y POR ÚLTIMO el usuario, para que si algo
            // falla no quede un usuario "vivo" con datos ya borrados de forma silenciosa.
            await _tareaRepo.EliminarTareasPorUsuario(idUsuario);
            await _areaRepo.EliminarAreasPorUsuario(idUsuario);
            await _ubicacionRepo.EliminarUbicacionesPorUsuario(idUsuario);
            await _bloqueRepo.EliminarBloquesPorUsuario(idUsuario);
            await _usuarioRepo.EliminarUsuario(idUsuario);
        }
        catch (Exception ex)
        {
            ErrorValidacion = $"No se pudo eliminar el usuario: {ex.Message}";
            MostrarError = true;
            Console.WriteLine($"Error al eliminar usuario en cascada: {ex}");
            return;
        }

        // Volvemos al listado. UsuariosViewModel es transient: al navegar se reconstruye
        // y recarga desde la BD, por lo que el usuario borrado ya no aparece.
        _navigationService.NavigateToPage(ApplicationPageNames.AdminUsuarios);
    }

    private void ActualizarTextosVisuales()
    {
        OnPropertyChanged(nameof(TextoBotonEstado));
        OnPropertyChanged(nameof(ColorTextoEstado));
        OnPropertyChanged(nameof(StatusBadgeText));
        OnPropertyChanged(nameof(ColorBadgeEstado));
    }

    [RelayCommand]
    private void Volver()
    {
        _navigationService.NavigateToPage(ApplicationPageNames.AdminUsuarios);
    }

    // ⚠️ TEMP DEV: siembra datos de prueba (áreas, ubicaciones, tareas) para el usuario
    // abierto. Borra y regenera. Quitar antes de entregar (comando + botón en la vista +
    // registro en DI + DatosPruebaSeeder.cs).
    [RelayCommand]
    private async Task GenerarDatosPrueba()
    {
        if (_usuarioOriginal == null || string.IsNullOrEmpty(_usuarioOriginal.IdUsuario))
            return;

        SeedResultado = "Generando datos de prueba…";
        try
        {
            var seeder = App.Services.GetRequiredService<DatosPruebaSeeder>();
            SeedResultado = await seeder.SembrarAsync(_usuarioOriginal.IdUsuario);
        }
        catch (Exception ex)
        {
            SeedResultado = $"Error al generar datos: {ex.Message}";
            Console.WriteLine($"Error en DatosPruebaSeeder: {ex}");
        }
    }

    [RelayCommand]
    private async Task GuardarCambios()
    {
        MostrarError = false;
        MostrarMensajeExito = false;

        // --- VALIDACIONES DE CAMPOS ---
        if (string.IsNullOrWhiteSpace(Nombre))
        {
            ErrorValidacion = "El campo de nombre completo no puede quedar vacío.";
            MostrarError = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(Correo) || !Correo.Contains("@") || !Correo.Contains("."))
        {
            ErrorValidacion = "Estructura de correo electrónico inválida.";
            MostrarError = true;
            return;
        }

        if (!FechaNacimientoDate.HasValue || FechaNacimientoDate.Value.Date > DateTime.Today)
        {
            ErrorValidacion = "La fecha de nacimiento seleccionada es inválida.";
            MostrarError = true;
            return;
        }

        if (string.IsNullOrEmpty(RegionSeleccionada) || string.IsNullOrEmpty(ComunaSeleccionada))
        {
            ErrorValidacion = "Es obligatorio seleccionar una región y comuna.";
            MostrarError = true;
            return;
        }

        if (_usuarioOriginal != null)
        {
            _usuarioOriginal.EstaActivo = EsActivo;
            _usuarioOriginal.NombreCompleto = Nombre;
            _usuarioOriginal.Correo = Correo;
            _usuarioOriginal.FecNacimiento = FechaNacimientoDate.Value;
            _usuarioOriginal.Ubicacion = $"{ComunaSeleccionada}, {RegionSeleccionada}";

            await _usuarioRepo.ActualizarUsuario(_usuarioOriginal.IdUsuario, _usuarioOriginal);

            MostrarMensajeExito = true;
            await Task.Delay(3000);
            MostrarMensajeExito = false;
        }
    }

    [RelayCommand]
    private async Task DetailYearLeft()
    {
        YearActual--;
        await CargarMetricasDelAnioActualAsync();
    }

    [RelayCommand]
    private async Task DetailYearRight()
    {
        if (YearActual < DateTime.Now.Year)
        {
            YearActual++;
            await CargarMetricasDelAnioActualAsync();
        }
    }

    private async Task CargarMetricasDelAnioActualAsync()
    {
        if (_usuarioOriginal == null || string.IsNullOrEmpty(_usuarioOriginal.IdUsuario))
            return;

        try
        {
            var tareasDelUsuario = await _tareaRepo.ObtenerTareasPorUsuario(_usuarioOriginal.IdUsuario);

            // Filtro dinámico combinando el año y el mes seleccionado en la barra interactiva
            var tareasFiltradas = tareasDelUsuario.Where(t => t.FecCreacion.Year == YearActual);

            if (MesSeleccionado != null)
            {
                tareasFiltradas = tareasFiltradas.Where(t => t.FecCreacion.Month == MesSeleccionado.Numero);
            }

            var listaFinal = tareasFiltradas.ToList();

            TareasCreadas = listaFinal.Count;
            TareasCompletadas = listaFinal.Count(t => t.CompletadoEnTiempo);

            // Conteo real de generaciones del usuario en el año/mes seleccionado.
            var generaciones = await _generacionRepo.ObtenerPorUsuario(_usuarioOriginal.IdUsuario);
            var gensFiltradas = generaciones.Where(g => g.FecGeneracion.Year == YearActual);
            if (MesSeleccionado != null)
                gensFiltradas = gensFiltradas.Where(g => g.FecGeneracion.Month == MesSeleccionado.Numero);
            GeneracionesRealizadas = gensFiltradas.Count();
        }
        catch (Exception ex)
        {
            TareasCreadas = 0;
            TareasCompletadas = 0;
            GeneracionesRealizadas = 0;
            Console.WriteLine($"Error al computar métricas: {ex.Message}");
        }
    }
}