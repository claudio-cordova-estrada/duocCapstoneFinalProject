using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlanificApp.Models.Services;
using planificApp.Helpers;
using planificApp.Data;

namespace planificApp.ViewModels;

public partial class RecuperarContraViewModel : PageViewModel
{
    private readonly MongoService _mongo;

    public MainViewModel Main { get; set; }

    [ObservableProperty] private string _correo = string.Empty;
    [ObservableProperty] private string _nuevaPassword = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _hasError = false;
    [ObservableProperty] private bool _recuperacionExitosa = false;

    public RecuperarContraViewModel(MongoService mongo)
    {
        _mongo = mongo;
        PageName = ApplicationPageNames.RecuperarContra;
    }

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
            ErrorMessage = "La contraseña debe tener al menos 6 caracteres.";
            HasError = true; return;
        }

        var usuario = await _mongo.BuscarPorCorreo(Correo);
        if (usuario == null)
        {
            ErrorMessage = "Correo no encontrado.";
            HasError = true; return;
        }

        var nuevoHash = PasswordHelper.HashPassword(NuevaPassword);
        await _mongo.ActualizarPassword(Correo, nuevoHash);
        RecuperacionExitosa = true;
    }
}