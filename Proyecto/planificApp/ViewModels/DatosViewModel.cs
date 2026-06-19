using System.IO;
using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using planificApp.Data;
using planificApp.Helpers;
using PlanificApp.Models.Services.Interfaces;
using PlanificApp.Models.Repositories.Interfaces;
using Avalonia.Media.Imaging;
using planificApp.Services;

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

            var partes = NombreCompleto.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            Iniciales = partes.Length > 1
                ? $"{partes[0][0]}{partes[^1][0]}"
                : NombreCompleto.Substring(0, 1);
        }
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
