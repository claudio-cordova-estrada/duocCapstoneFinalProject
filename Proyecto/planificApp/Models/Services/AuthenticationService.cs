using System.Threading.Tasks;
using PlanificApp.Models.Repositories.Interfaces;
using PlanificApp.Models.Services.Interfaces;
using planificApp.Helpers;

namespace PlanificApp.Models.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IUsuarioRepository _usuarioRepository;

    public AuthenticationService(IUsuarioRepository usuarioRepository)
    {
        _usuarioRepository = usuarioRepository;
    }

    public async Task<Usuario?> Login(string correo, string password)
    {
        var usuario = await _usuarioRepository.BuscarPorCorreo(correo);
        if (usuario == null) return null;
        if (!PasswordHelper.VerifyPassword(password, usuario.PasswordHash)) return null;

        // Al volver a entrar, una cuenta inactiva se reactiva automáticamente.
        if (!usuario.EstaActivo && !string.IsNullOrEmpty(usuario.IdUsuario))
        {
            usuario.EstaActivo = true;
            await _usuarioRepository.ActualizarEstadoActivo(usuario.IdUsuario, true);
        }

        return usuario;
    }

    public string HashPassword(string password) => PasswordHelper.HashPassword(password);
}