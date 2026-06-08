using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlanificApp.Models;
using PlanificApp.Models.Services.Interfaces;
using PlanificApp.Models.Repositories.Interfaces;
using planificApp.Data;
using planificApp.Services;

namespace planificApp.ViewModels;

public partial class RegistroViewModel : PageViewModel
{
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IAuthenticationService _authService;
    private readonly INavigationService _navigation;

    [ObservableProperty] private string _nombreCompleto = string.Empty;
    [ObservableProperty] private string _correo = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _repetirPassword = string.Empty;
    [ObservableProperty] private DateTime? _fecNacimiento = null;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _hasError = false;
    [ObservableProperty] private bool _registroExitoso = false;

    public RegistroViewModel(IUsuarioRepository usuarioRepo, IAuthenticationService authService, INavigationService navigation)
    {
        _usuarioRepo = usuarioRepo;
        _authService = authService;
        _navigation = navigation;
        PageName = ApplicationPageNames.Registro;
    }

    [RelayCommand] private void GoToLogin() => _navigation.GoToLogin();

    [RelayCommand]
    private async Task RegistroAsync()
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(NombreCompleto))
        {
            ErrorMessage = "Ingresa tu nombre completo.";
            HasError = true; return;
        }
        if (string.IsNullOrWhiteSpace(Correo))
        {
            ErrorMessage = "Ingresa tu correo.";
            HasError = true; return;
        }
        if (FecNacimiento == null)
        {
            ErrorMessage = "Ingresa tu fecha de nacimiento.";
            HasError = true; return;
        }
        if (string.IsNullOrWhiteSpace(Password) || Password.Length < 6)
        {
            ErrorMessage = "La contrase�a debe tener al menos 6 caracteres.";
            HasError = true; return;
        }
        if (Password != RepetirPassword)
        {
            ErrorMessage = "Las contrase�as no coinciden.";
            HasError = true; return;
        }

        try
        {
            var nuevoUsuario = new Usuario
            {
                NombreCompleto = NombreCompleto,
                Correo = Correo,
                PasswordHash = _authService.HashPassword(Password),
                CuentaConfirmada = true,
                HoraInicioJornada = TimeSpan.FromHours(9),
                HoraFinJornada = TimeSpan.FromHours(18),
                FecCreacion = DateTime.Now,
                FecNacimiento = FecNacimiento!.Value,
                Ubicacion = "Concepci�n",
            };

            await _usuarioRepo.RegistrarUsuario(nuevoUsuario);
            RegistroExitoso = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message.Contains("ya est� registrado") ? ex.Message : "Error al crear la cuenta.";
            HasError = true;
        }
    }
}
