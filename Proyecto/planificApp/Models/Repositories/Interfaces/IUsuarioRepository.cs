using System.Collections.Generic;
using System.Threading.Tasks;
using PlanificApp.Models;

namespace PlanificApp.Models.Repositories.Interfaces;

public interface IUsuarioRepository
{
    Task RegistrarUsuario(Usuario nuevoUsuario);
    Task<Usuario?> BuscarPorCorreo(string correo);
    Task ActualizarPassword(string correo, string nuevaPasswordHash);
    Task ActualizarFotoPerfil(string idUsuario, string fotoBase64);
    Task ActualizarUbicacionActual(string idUsuario, string ubicacionActual);

    // --- NUEVOS MÉTODOS PARA EL ADMINISTRADOR ---
    Task<IEnumerable<Usuario>> ObtenerTodosLosUsuarios();
    Task ActualizarUsuario(string idUsuario, Usuario usuarioActualizado);
}