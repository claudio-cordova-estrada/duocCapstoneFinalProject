using System.Threading.Tasks;
using PlanificApp.Models;

namespace PlanificApp.Models.Services.Interfaces;

public interface IAuthenticationService
{
    Task<Usuario?> Login(string correo, string password);
    string HashPassword(string password);
}