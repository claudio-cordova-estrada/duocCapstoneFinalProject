using planificApp.Data;
using PlanificApp.Models; // Añadido para reconocer a Usuario

namespace planificApp.Services;

public interface INavigationService
{
    void OnLoginSuccess();
    void NavigateToPage(ApplicationPageNames page);
    void GoToLogin();
    void GoToRegistro();
    void GoToRecuperarContra();

    // ¡AQUÍ ESTÁ EL PUENTE! Ahora pide el usuario
    void GoToAdminUsuarioDetalle(Usuario usuario);

    bool IsAdminToggle { get; set; }
}