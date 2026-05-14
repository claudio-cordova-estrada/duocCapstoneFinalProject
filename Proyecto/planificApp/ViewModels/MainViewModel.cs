using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using planificApp.Data;
using planificApp.Factories;

namespace planificApp.ViewModels;

public enum UserSection { Tareas, Calendario, Ubicaciones, Cuenta, Config }
public enum AdminSection { Estadisticas, Usuarios }
public enum AuthPage { Login, Registro, RecuperarContra }

public partial class MainViewModel : ViewModelBase
{
    private readonly PageFactory _pageFactory;
    
    [ObservableProperty] private bool _sideMenuExpanded = true;
    
    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(IsTareasActive))]
    [NotifyPropertyChangedFor(nameof(IsCalendarioActive))]
    [NotifyPropertyChangedFor(nameof(IsUbicacionesActive))]
    [NotifyPropertyChangedFor(nameof(IsCuentaActive))]
    [NotifyPropertyChangedFor(nameof(IsConfigActive))]
    [NotifyPropertyChangedFor(nameof(ShowSB2))]
    private UserSection _activeUserSection = UserSection.Tareas;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAdminEstadisticasActive))]
    [NotifyPropertyChangedFor(nameof(IsAdminUsuariosActive))]
    private AdminSection _activeAdminSection = AdminSection.Estadisticas;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsInboxActive))]
    [NotifyPropertyChangedFor(nameof(IsHoyActive))]
    [NotifyPropertyChangedFor(nameof(IsSemanaActive))]
    [NotifyPropertyChangedFor(nameof(IsMesActive))]
    [NotifyPropertyChangedFor(nameof(IsAreaInteresActive))]
    [NotifyPropertyChangedFor(nameof(IsCalSemanalActive))]
    [NotifyPropertyChangedFor(nameof(IsCalMensualActive))]
    [NotifyPropertyChangedFor(nameof(IsDatosActive))]
    [NotifyPropertyChangedFor(nameof(IsSoporteActive))]
    [NotifyPropertyChangedFor(nameof(IsSobreActive))]
    private PageViewModel _currentPage;
    
    [ObservableProperty] private bool _isAdminMode = false;

    // Auth state
    [ObservableProperty] private bool _isLoggedIn = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowSB2))]
    private bool _isAdminToggle = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAppLayout))]
    [NotifyPropertyChangedFor(nameof(ShowSB2))]
    private AuthPage _currentAuthPage = AuthPage.Login;

    public bool ShowAppLayout => IsLoggedIn;

    public MainViewModel()
    {
        ActiveUserSection = UserSection.Tareas;
        CurrentPage = new InboxViewModel();
    }

    public MainViewModel(PageFactory pageFactory)
    {
        _pageFactory = pageFactory;
        NavigateToAuth(ApplicationPageNames.Login);
    }

    private void NavigateToAuth(ApplicationPageNames page)
    {
        var vm = _pageFactory.GetPageViewModel(page);
        if (vm is LoginViewModel login) login.Main = this;
        else if (vm is RegistroViewModel registro) registro.Main = this;
        else if (vm is RecuperarContraViewModel recuperar) recuperar.Main = this;
        CurrentPage = vm;
    }

    // Auth navigation
    [RelayCommand] private void GoToLogin() { CurrentAuthPage = AuthPage.Login; NavigateToAuth(ApplicationPageNames.Login); }
    [RelayCommand] private void GoToRegistro() { CurrentAuthPage = AuthPage.Registro; NavigateToAuth(ApplicationPageNames.Registro); }
    [RelayCommand] private void GoToRecuperarContra() { CurrentAuthPage = AuthPage.RecuperarContra; NavigateToAuth(ApplicationPageNames.RecuperarContra); }

    [RelayCommand] private void Login()
    {
        IsLoggedIn = true;
        if (IsAdminToggle)
        {
            ActiveAdminSection = AdminSection.Estadisticas;
            CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.AdminEstadisticas);
        }
        else
        {
            ActiveUserSection = UserSection.Tareas;
            CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.UserInbox);
        }
    }

    [RelayCommand] private void Logout()
    {
        IsLoggedIn = false;
        IsAdminToggle = false;
        NavigateToAuth(ApplicationPageNames.Login);
    }

    // Admin navigation
    [RelayCommand] private void GoToAdminEstadisticas() { ActiveAdminSection = AdminSection.Estadisticas; CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.AdminEstadisticas); }
    [RelayCommand] private void GoToAdminUsuarios() { ActiveAdminSection = AdminSection.Usuarios; CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.AdminUsuarios); }
    public void GoToAdminUsuarioDetalle() { CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.AdminUsuarioDetalle); }

    // SB1 Navigation
    [RelayCommand] private void GoToTareas() { ActiveUserSection = UserSection.Tareas; CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.UserInbox); }
    [RelayCommand] private void GoToCalendario() { ActiveUserSection = UserSection.Calendario; CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.UserCalendarioSemanal); }
    [RelayCommand] private void GoToUbicaciones() { ActiveUserSection = UserSection.Ubicaciones; CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.UserUbicaciones); }
    [RelayCommand] private void GoToCuenta() { ActiveUserSection = UserSection.Cuenta; CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.UserDatos); }
    [RelayCommand] private void GoToConfig() { ActiveUserSection = UserSection.Config; CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.UserConfig); }

    // SB2 - Tareas
    [RelayCommand] private void GoToInbox() => CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.UserInbox);
    [RelayCommand] private void GoToHoy() => CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.UserHoy);
    [RelayCommand] private void GoToSemana() => CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.UserSemana);
    [RelayCommand] private void GoToMes() => CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.UserMes);
    [RelayCommand] private void GoToAreaInteres() => CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.UserAreaInteres);

    // SB2 - Calendario
    [RelayCommand] private void GoToCalendarioSemanal() => CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.UserCalendarioSemanal);
    [RelayCommand] private void GoToCalendarioMensual() => CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.UserCalendarioMensual);
    public void GoToPropuestasSemanales() => CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.UserPropuestasSemanales);

    // SB2 - Cuenta
    [RelayCommand] private void GoToDatos() => CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.UserDatos);
    [RelayCommand] private void GoToSoporte() => CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.UserSoporte);
    [RelayCommand] private void GoToSobre() => CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.UserSobre);

    // Active state helpers for SB1 - User
    public bool IsTareasActive => ActiveUserSection == UserSection.Tareas;
    public bool IsCalendarioActive => ActiveUserSection == UserSection.Calendario;
    public bool IsUbicacionesActive => ActiveUserSection == UserSection.Ubicaciones;
    public bool IsCuentaActive => ActiveUserSection == UserSection.Cuenta;
    public bool IsConfigActive => ActiveUserSection == UserSection.Config;

    // Active state helpers for SB1 - Admin
    public bool IsAdminEstadisticasActive => ActiveAdminSection == AdminSection.Estadisticas;
    public bool IsAdminUsuariosActive => ActiveAdminSection == AdminSection.Usuarios;

    // Active state helpers for SB2 - Tareas
    public bool IsInboxActive => CurrentPage.PageName == ApplicationPageNames.UserInbox;
    public bool IsHoyActive => CurrentPage.PageName == ApplicationPageNames.UserHoy;
    public bool IsSemanaActive => CurrentPage.PageName == ApplicationPageNames.UserSemana;
    public bool IsMesActive => CurrentPage.PageName == ApplicationPageNames.UserMes;
    public bool IsAreaInteresActive => CurrentPage.PageName == ApplicationPageNames.UserAreaInteres;

    // Active state helpers for SB2 - Calendario
    public bool IsCalSemanalActive => CurrentPage.PageName == ApplicationPageNames.UserCalendarioSemanal;
    public bool IsCalMensualActive => CurrentPage.PageName == ApplicationPageNames.UserCalendarioMensual;

    // Active state helpers for SB2 - Cuenta
    public bool IsDatosActive => CurrentPage.PageName == ApplicationPageNames.UserDatos;
    public bool IsSoporteActive => CurrentPage.PageName == ApplicationPageNames.UserSoporte;
    public bool IsSobreActive => CurrentPage.PageName == ApplicationPageNames.UserSobre;

    // SB2 visibility
    public bool ShowSB2 => !IsAdminToggle && ActiveUserSection is UserSection.Tareas or UserSection.Calendario or UserSection.Cuenta;
}