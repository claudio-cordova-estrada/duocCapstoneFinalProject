using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlanificApp.Models.Services.Interfaces;
using planificApp.Data;
using planificApp.Services;

namespace planificApp.ViewModels;

public partial class LoginViewModel : PageViewModel
{
    private readonly IAuthenticationService _authService;
    private readonly ISesionService _sesion;
    private readonly INavigationService _navigation;

    [ObservableProperty] private string _correo = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _hasError = false;
    [ObservableProperty] private bool _isLoading = false;

    public bool IsAdminToggle
    {
        get => _navigation.IsAdminToggle;
        set => _navigation.IsAdminToggle = value;
    }

    public LoginViewModel(IAuthenticationService authService, ISesionService sesion, INavigationService navigation)
    {
        _authService = authService;
        _sesion = sesion;
        _navigation = navigation;
        PageName = ApplicationPageNames.Login;
    }

    [RelayCommand] private void GoToRegistro() => _navigation.GoToRegistro();
    [RelayCommand] private void GoToRecuperarContra() => _navigation.GoToRecuperarContra();

    // Atajos de prueba: autocompletan el formulario con credenciales conocidas.
    [RelayCommand]
    private void RellenarUsuario()
    {
        Correo = "michael@gmail.com";
        Password = "asdfasdf";
    }

    [RelayCommand]
    private void RellenarAdmin()
    {
        Correo = "c@planificapp.com";
        Password = "asdfasdf";
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Correo) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Ingresa correo y contraseña.";
            HasError = true;
            return;
        }

        IsLoading = true;

        try
        {
            var usuario = await _authService.Login(Correo, Password);
            if (usuario != null)
            {
                _sesion.IniciarSesion(usuario);

                // --- LÓGICA AUTOMÁTICA DE ROLES ---
                string correoValidar = Correo.Trim().ToLower();

                if (correoValidar.Contains("@planificapp."))
                {
                    _navigation.IsAdminToggle = true; // Activa el rol Admin
                }
                else
                {
                    _navigation.IsAdminToggle = false; // Se asegura de que sea Usuario normal
                }

                // Disparamos el éxito. El servicio de navegación decidirá a dónde ir según el Toggle
                _navigation.OnLoginSuccess();
            }
            else
            {
                ErrorMessage = "Correo o contraseña incorrectos.";
                HasError = true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LoginAsync] Error: {ex}");
            ErrorMessage = $"Error: {ex.Message}";
            HasError = true;
        }
        finally
        {
            IsLoading = false;
        }
    }
}