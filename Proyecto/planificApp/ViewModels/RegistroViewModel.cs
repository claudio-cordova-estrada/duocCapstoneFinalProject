using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlanificApp.Models;
using PlanificApp.Models.Services;
using planificApp.Helpers;
using planificApp.Data;

namespace planificApp.ViewModels;

public partial class RegistroViewModel : PageViewModel
{
    private readonly MongoService _mongo;

    public MainViewModel Main { get; set; }

    [ObservableProperty] private string _nombreCompleto = string.Empty;
    [ObservableProperty] private string _correo = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _repetirPassword = string.Empty;
    [ObservableProperty] private string _respuestaSeguridad = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _hasError = false;
    [ObservableProperty] private bool _registroExitoso = false;

    public RegistroViewModel(MongoService mongo)
    {
        _mongo = mongo;
        PageName = ApplicationPageNames.Registro;
    }

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
        if (string.IsNullOrWhiteSpace(Password) || Password.Length < 6)
        {
            ErrorMessage = "La contraseña debe tener al menos 6 caracteres.";
            HasError = true; return;
        }
        if (Password != RepetirPassword)
        {
            ErrorMessage = "Las contraseñas no coinciden.";
            HasError = true; return;
        }
        if (string.IsNullOrWhiteSpace(RespuestaSeguridad))
        {
            ErrorMessage = "Ingresa una respuesta de seguridad.";
            HasError = true; return;
        }

        try
        {
            var nuevoUsuario = new Usuario
            {
                NombreCompleto = NombreCompleto,
                Correo = Correo,
                PasswordHash = PasswordHelper.HashPassword(Password),
                RespuestaSeguridad = PasswordHelper.HashPassword(RespuestaSeguridad),
                CuentaConfirmada = true,
                HoraInicioJornada = TimeSpan.FromHours(9),
                HoraFinJornada = TimeSpan.FromHours(18),
            };

            await _mongo.RegistrarUsuario(nuevoUsuario);
            RegistroExitoso = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message.Contains("ya está registrado") ? ex.Message : "Error al crear la cuenta.";
            HasError = true;
        }
    }
}
