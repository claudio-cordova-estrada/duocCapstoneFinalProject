using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using planificApp.Data;
using planificApp.Factories;

namespace planificApp.ViewModels;

public enum UserSection { Tareas, Calendario, Ubicaciones, Cuenta, Config }
public enum AdminSection { Estadisticas, Usuarios }

public partial class MainViewModel : ViewModelBase
{
    private readonly PageFactory _pageFactory;
    
    [ObservableProperty] private bool _sideMenuExpanded = true;
    [ObservableProperty] private UserSection _activeUserSection = UserSection.Tareas;
    [ObservableProperty] private PageViewModel _currentPage;
    [ObservableProperty] private bool _isAdminMode = false;

    public MainViewModel(PageFactory pageFactory)
    {
        _pageFactory = pageFactory;
        CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.UserInbox);
    }

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

    // SB2 - Cuenta
    [RelayCommand] private void GoToDatos() => CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.UserDatos);
    [RelayCommand] private void GoToSoporte() => CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.UserSoporte);
    [RelayCommand] private void GoToSobre() => CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.UserSobre);

    // Active state helpers for SB1
    public bool IsTareasActive => ActiveUserSection == UserSection.Tareas;
    public bool IsCalendarioActive => ActiveUserSection == UserSection.Calendario;
    public bool IsUbicacionesActive => ActiveUserSection == UserSection.Ubicaciones;
    public bool IsCuentaActive => ActiveUserSection == UserSection.Cuenta;
    public bool IsConfigActive => ActiveUserSection == UserSection.Config;

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
    public bool ShowSB2 => ActiveUserSection is UserSection.Tareas or UserSection.Calendario or UserSection.Cuenta;
}
