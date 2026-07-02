using System.IO;
using System;
using System.Globalization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using planificApp.Data;
using planificApp.Helpers;
using PlanificApp.Models.Services.Interfaces;
using PlanificApp.Models.Repositories.Interfaces;
using Avalonia.Media.Imaging;

namespace planificApp.ViewModels;

public partial class DatosViewModel : PageViewModel
{
    [ObservableProperty] private string _nombreCompleto = string.Empty;
    [ObservableProperty] private string _correo = string.Empty;
    [ObservableProperty] private string _iniciales = string.Empty;
    [ObservableProperty] private string _fecCreacion;
    [ObservableProperty] private string _fecNacimiento = string.Empty;
    [ObservableProperty] private string _ubicacion = string.Empty;
    [ObservableProperty] private Bitmap? _fotoPerfil;
    [ObservableProperty] private bool _tieneFoto = false;

    public IRegionesService ServicioRegiones { get; }

    private readonly ISesionService _sesion;
    private readonly IUsuarioRepository _usuarioRepo;

    public DatosViewModel(ISesionService sesion, IUsuarioRepository usuarioRepo, IRegionesService regionesService)
    {
        _sesion = sesion;
        _usuarioRepo = usuarioRepo;
        PageName = ApplicationPageNames.UserDatos;
        ServicioRegiones = regionesService;
        CargarDatos();
    }


    private void CargarDatos()
    {
        if (_sesion.UsuarioActual != null)
        {
            if (!string.IsNullOrEmpty(_sesion.UsuarioActual.FotoPerfil))
            {
                var bytes = Convert.FromBase64String(_sesion.UsuarioActual.FotoPerfil);
                FotoPerfil = new Bitmap(new MemoryStream(bytes));
                TieneFoto = true;
            }

            NombreCompleto = StringHelper.ToTitleCase(_sesion.UsuarioActual.NombreCompleto);
            Correo = _sesion.UsuarioActual.Correo;
            FecCreacion = _sesion.UsuarioActual.FecCreacion.ToString("dd/MM/yyyy");

            if (_sesion.UsuarioActual.FecNacimiento != DateTime.MinValue)
                FecNacimiento = _sesion.UsuarioActual.FecNacimiento.ToString("dd MMMM yyyy");

            Ubicacion = _sesion.UsuarioActual.Ubicacion ?? "Sin ubicación";

            ActualizarIniciales();
        }
    }

    private void ActualizarIniciales()
    {
        var partes = NombreCompleto.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Iniciales = partes.Length > 1
            ? $"{partes[0][0]}{partes[^1][0]}"
            : (NombreCompleto.Length > 0 ? NombreCompleto.Substring(0, 1) : string.Empty);
    }

    // Fecha de nacimiento actual como DateTime (para preseleccionar el CalendarDatePicker al editar).
    public DateTime? FechaNacimientoValor =>
        _sesion.UsuarioActual is { FecNacimiento: var f } && f != DateTime.MinValue ? f : null;

    // --- GUARDADO DE DATOS PERSONALES (persisten en la sesión actual + MongoDB) ---

    private async Task PersistirDatosAsync()
    {
        var u = _sesion.UsuarioActual;
        if (u == null || string.IsNullOrEmpty(u.IdUsuario)) return;
        await _usuarioRepo.ActualizarDatosPersonales(u.IdUsuario, u.NombreCompleto, u.Correo, u.FecNacimiento, u.Ubicacion);
    }

    public async Task GuardarNombreAsync(string nuevoNombre)
    {
        var u = _sesion.UsuarioActual;
        if (u == null) return;
        u.NombreCompleto = nuevoNombre;
        NombreCompleto = StringHelper.ToTitleCase(nuevoNombre);
        ActualizarIniciales();
        await PersistirDatosAsync();
    }

    public async Task<(bool Ok, string? Error)> GuardarCorreoAsync(string nuevoCorreo)
    {
        var u = _sesion.UsuarioActual;
        if (u == null) return (false, "No hay sesión activa.");

        // El correo es la llave del login: si cambia, validamos que no lo use otro usuario.
        if (!nuevoCorreo.Equals(u.Correo, StringComparison.OrdinalIgnoreCase))
        {
            var existente = await _usuarioRepo.BuscarPorCorreo(nuevoCorreo);
            if (existente != null && existente.IdUsuario != u.IdUsuario)
                return (false, "Ese correo ya está registrado por otro usuario.");
        }

        u.Correo = nuevoCorreo;
        Correo = nuevoCorreo;
        await PersistirDatosAsync();
        return (true, null);
    }

    public async Task GuardarFechaAsync(DateTime fecha)
    {
        var u = _sesion.UsuarioActual;
        if (u == null) return;
        u.FecNacimiento = fecha;
        FecNacimiento = fecha.ToString("dd MMMM yyyy", new CultureInfo("es-CL"));
        await PersistirDatosAsync();
    }

    public async Task GuardarUbicacionAsync(string nuevaUbicacion)
    {
        var u = _sesion.UsuarioActual;
        if (u == null) return;
        u.Ubicacion = nuevaUbicacion;
        Ubicacion = nuevaUbicacion;
        await PersistirDatosAsync();
    }

    public async Task GuardarFotoAsync(byte[] imageBytes)
    {
        const int maxSize = 500 * 1024;

        if (imageBytes.Length > maxSize)
            return;

        if (_sesion.UsuarioActual == null || string.IsNullOrEmpty(_sesion.UsuarioActual.IdUsuario))
            return;

        var base64 = Convert.ToBase64String(imageBytes);

        await _usuarioRepo.ActualizarFotoPerfil(_sesion.UsuarioActual.IdUsuario!, base64);

        _sesion.UsuarioActual.FotoPerfil = base64;
        FotoPerfil = new Bitmap(new MemoryStream(imageBytes));
        TieneFoto = true;
    }
}
