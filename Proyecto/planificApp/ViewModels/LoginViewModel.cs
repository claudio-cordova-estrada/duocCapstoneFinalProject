using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlanificApp.Models.Services;
using planificApp.Data;

namespace planificApp.ViewModels;

public partial class LoginViewModel : PageViewModel
{
    private readonly MongoService _mongo;
    private readonly SesionService _sesion;

    public MainViewModel Main { get; set; }

    [ObservableProperty] private string _correo = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _hasError = false;
    [ObservableProperty] private bool _isLoading = false;

    public LoginViewModel(MongoService mongo, SesionService sesion)
    {
        _mongo = mongo;
        _sesion = sesion;
        PageName = ApplicationPageNames.Login;
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
            var usuario = await _mongo.Login(Correo, Password);
            if (usuario != null)
            {
                _sesion.IniciarSesion(usuario);
                Main.LoginCommand.Execute(null);
            }
            else
            {
                ErrorMessage = "Correo o contraseña incorrectos.";
                HasError = true;
            }
        }
        catch
        {
            ErrorMessage = "Error de conexión. Intenta de nuevo.";
            HasError = true;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
