using System;

namespace PlanificApp.Models.Services.Interfaces;

public interface ISesionService
{
    Usuario? UsuarioActual { get; }
    event Action? SesionCambiada;
    void IniciarSesion(Usuario usuario);
    void CerrarSesion();
    void SetUbicacionActual(string ubicacion);
}