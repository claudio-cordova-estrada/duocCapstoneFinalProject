using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlanificApp.Models.Enums;

namespace PlanificApp.Models.Repositories.Interfaces;

public interface IUsuarioRepository
{
    Task RegistrarUsuario(Usuario nuevoUsuario);
    Task<Usuario?> BuscarPorCorreo(string correo);
    Task ActualizarPassword(string correo, string nuevaPasswordHash);
    Task ActualizarFotoPerfil(string idUsuario, string fotoBase64);
    Task ActualizarUbicacionActual(string idUsuario, string ubicacionActual);
    Task ActualizarEstadoActivo(string idUsuario, bool estaActivo);
    Task ActualizarDatosPersonales(string idUsuario, string nombreCompleto, string correo, DateTime fecNacimiento, string ubicacion);
    Task ActualizarConfigUsuario(string idUsuario, TimeSpan horaInicio, TimeSpan horaFin, string? ubicacionActual, MetodoTransporte? transportePred, double horasSueno, List<DayOfWeek> diasGeneracion);

    // --- NUEVOS MÉTODOS PARA EL ADMINISTRADOR ---
    Task<IEnumerable<Usuario>> ObtenerTodosLosUsuarios();
    Task ActualizarUsuario(string idUsuario, Usuario usuarioActualizado);
    Task EliminarUsuario(string idUsuario);
}