using System;
using PlanificApp.Models;

namespace PlanificApp.Models.Services;

public class SesionService
{
    public Usuario? UsuarioActual { get; private set; }
    public event Action? SesionCambiada;

    public void IniciarSesion(Usuario usuario)
    {
        UsuarioActual = usuario;
        SesionCambiada?.Invoke();
    }

    public void CerrarSesion()
    {
        UsuarioActual = null;
        SesionCambiada?.Invoke();
    }
}
