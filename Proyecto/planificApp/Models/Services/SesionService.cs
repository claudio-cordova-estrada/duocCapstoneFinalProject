using System;
using PlanificApp.Models;
using PlanificApp.Models.Services.Interfaces;

namespace PlanificApp.Models.Services;

public class SesionService : ISesionService
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
