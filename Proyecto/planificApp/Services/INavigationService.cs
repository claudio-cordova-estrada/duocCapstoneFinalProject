using planificApp.Data;

namespace planificApp.Services;

public interface INavigationService
{
    void OnLoginSuccess();
    void NavigateToPage(ApplicationPageNames page);
    void GoToLogin();
    void GoToRegistro();
    void GoToRecuperarContra();
    bool IsAdminToggle { get; set; }
}