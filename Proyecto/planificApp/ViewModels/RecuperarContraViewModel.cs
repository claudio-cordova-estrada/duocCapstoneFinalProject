using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlanificApp.Models.Services.Interfaces;
using PlanificApp.Models.Repositories.Interfaces;
using planificApp.Data;
using planificApp.Services;

namespace planificApp.ViewModels;

public partial class RecuperarContraViewModel : PageViewModel
{
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IAuthenticationService _authService;
    private readonly INavigationService _navigation;

    [ObservableProperty] private string _correo = string.Empty;
    [ObservableProperty] private string _nuevaPassword = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _hasError = false;
    [ObservableProperty] private bool _recuperacionExitosa = false;

    public RecuperarContraViewModel(IUsuarioRepository usuarioRepo, IAuthenticationService authService, INavigationService navigation)
    {
        _usuarioRepo = usuarioRepo;
        _authService = authService;
        _navigation = navigation;
        PageName = ApplicationPageNames.RecuperarContra;
    }

    [RelayCommand] private void GoToLogin() => _navigation.GoToLogin();

    [RelayCommand]
    private async Task CambiarPasswordAsync()
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Correo))
        {
            ErrorMessage = "Ingresa tu correo.";
            HasError = true; return;
        }

        if (string.IsNullOrWhiteSpace(NuevaPassword) || NuevaPassword.Length < 6)
        {
            ErrorMessage = "La contrase�a debe tener al menos 6 caracteres.";
            HasError = true; return;
        }

        var usuario = await _usuarioRepo.BuscarPorCorreo(Correo);
        if (usuario == null)
        {
            ErrorMessage = "Correo no encontrado.";
            HasError = true; return;
        }

        var nuevoHash = _authService.HashPassword(NuevaPassword);
        await _usuarioRepo.ActualizarPassword(Correo, nuevoHash);
        RecuperacionExitosa = true;
    }
}