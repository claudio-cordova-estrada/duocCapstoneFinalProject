using System;
using PlanificApp.Models;

namespace PlanificApp.Models.Services.Interfaces;

public interface ISesionService
{
    Usuario? UsuarioActual { get; }
    event Action? SesionCambiada;
    void IniciarSesion(Usuario usuario);
    void CerrarSesion();
}