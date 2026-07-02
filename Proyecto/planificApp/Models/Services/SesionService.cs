using System;
using PlanificApp.Models.Repositories.Interfaces;
using PlanificApp.Models.Services.Interfaces;

namespace PlanificApp.Models.Services;

public class SesionService : ISesionService
{
    private readonly IUsuarioRepository? _usuarioRepository;

    public SesionService() { }

    public SesionService(IUsuarioRepository usuarioRepository)
    {
        _usuarioRepository = usuarioRepository;
    }

    public Usuario? UsuarioActual { get; private set; }
    public event Action? SesionCambiada;

    public void IniciarSesion(Usuario usuario)
    {
        UsuarioActual = usuario;
        if (string.IsNullOrEmpty(UsuarioActual.UbicacionActual))
            UsuarioActual.UbicacionActual = "Casa";
        SesionCambiada?.Invoke();
    }

    public void CerrarSesion()
    {
        UsuarioActual = null;
        SesionCambiada?.Invoke();
    }

    public void SetUbicacionActual(string ubicacion)
    {
        if (UsuarioActual == null) return;
        UsuarioActual.UbicacionActual = ubicacion;
        _usuarioRepository?.ActualizarUbicacionActual(UsuarioActual.IdUsuario!, ubicacion);
    }
}
