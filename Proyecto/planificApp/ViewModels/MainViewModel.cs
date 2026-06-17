using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using planificApp.Data;
using planificApp.Factories;
using PlanificApp.Models;
using PlanificApp.Models.Services.Interfaces;
using PlanificApp.Models.Repositories.Interfaces;
using planificApp.Services;

namespace planificApp.ViewModels;

public enum UserSection { Tareas, Calendario, Ubicaciones, Cuenta, Config }
public enum AdminSection { Estadisticas, Usuarios }
public enum AuthPage { Login, Registro, RecuperarContra }

public partial class MainViewModel : ViewModelBase, INavigationService
{
    private readonly IAreaInteresRepository _areaRepo;
    private readonly ISesionService _sesion;
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
    private ObservableCollection<AreaInteres> _areasInteres = new();

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

    public MainViewModel(PageFactory pageFactory, ISesionService sesion, IAreaInteresRepository areaRepo)
    {
        _pageFactory = pageFactory;
        _sesion = sesion;
        _areaRepo = areaRepo;
    }

    public void Initialize()
    {
        NavigateToAuth(ApplicationPageNames.Login);
    }

    private void NavigateToAuth(ApplicationPageNames page)
    {
        var vm = _pageFactory.GetPageViewModel(page);
        CurrentPage = vm;
    }

    public void OnLoginSuccess() => LoginCommand.Execute(null);

    public void NavigateToPage(ApplicationPageNames page) => CurrentPage = _pageFactory.GetPageViewModel(page);

    // Auth navigation (also implements INavigationService)
    [RelayCommand] public void GoToLogin() { CurrentAuthPage = AuthPage.Login; NavigateToAuth(ApplicationPageNames.Login); }
    [RelayCommand] public void GoToRegistro() { CurrentAuthPage = AuthPage.Registro; NavigateToAuth(ApplicationPageNames.Registro); }
    [RelayCommand] public void GoToRecuperarContra() { CurrentAuthPage = AuthPage.RecuperarContra; NavigateToAuth(ApplicationPageNames.RecuperarContra); }

    [RelayCommand] private async Task LoginAsync()
    {
        IsLoggedIn = true;
        if (IsAdminToggle)
        {
            ActiveAdminSection = AdminSection.Estadisticas;
            CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.AdminEstadisticas);
        }
        else
        {
            var areas = await _areaRepo.ObtenerAreasPorUsuario(_sesion.UsuarioActual!.IdUsuario!);
            AreasInteres = new ObservableCollection<AreaInteres>(areas);
            
            ActiveUserSection = UserSection.Tareas;
            var vm = (InboxViewModel)_pageFactory.GetPageViewModel(ApplicationPageNames.UserInbox);
            vm.SetModo(ModoVista.Inbox);
            CurrentPage = vm;
        }
    }

    [RelayCommand] private void Logout()
    {
        _sesion.CerrarSesion();
        IsLoggedIn = false;
        IsAdminToggle = false;
        NavigateToAuth(ApplicationPageNames.Login);
    }
    
    [RelayCommand]
    public void GoToAreaEspecifica(AreaInteres area)
    {
        var vm = (AreaInteresViewModel)_pageFactory.GetPageViewModel(ApplicationPageNames.UserAreaInteres);
        vm.SetArea(area);
        CurrentPage = vm;
    }
    
    public async Task ReloadAreasAsync()
    {
        if (_sesion.UsuarioActual == null) return;
        var areas = await _areaRepo.ObtenerAreasPorUsuario(_sesion.UsuarioActual.IdUsuario!);
        AreasInteres = new ObservableCollection<AreaInteres>(areas);
    }

    // Admin navigation
    [RelayCommand] private void GoToAdminEstadisticas() { ActiveAdminSection = AdminSection.Estadisticas; CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.AdminEstadisticas); }
    [RelayCommand] private void GoToAdminUsuarios() { ActiveAdminSection = AdminSection.Usuarios; CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.AdminUsuarios); }
    public void GoToAdminUsuarioDetalle(Usuario usuario)
    {
        // 1. Obtenemos el ViewModel de la fábrica
        var vm = (UsuarioDetalleViewModel)_pageFactory.GetPageViewModel(ApplicationPageNames.AdminUsuarioDetalle);

        // 2. Le inyectamos los datos del usuario clickeado
        vm.CargarDatosUsuario(usuario);

        // 3. Mostramos la página ya cargada
        CurrentPage = vm;
    }

    // SB1 Navigation
    [RelayCommand] private void GoToTareas() { ActiveUserSection = UserSection.Tareas; var vm = (InboxViewModel)_pageFactory.GetPageViewModel(ApplicationPageNames.UserInbox); vm.SetModo(ModoVista.Inbox); CurrentPage = vm; }
    [RelayCommand] private void GoToCalendario() { ActiveUserSection = UserSection.Calendario; CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.UserCalendarioSemanal); }
    [RelayCommand] private void GoToUbicaciones() { ActiveUserSection = UserSection.Ubicaciones; CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.UserUbicaciones); }
    [RelayCommand] private void GoToCuenta() { ActiveUserSection = UserSection.Cuenta; CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.UserDatos); }
    [RelayCommand] private void GoToConfig() { ActiveUserSection = UserSection.Config; CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.UserConfig); }

    // SB2 - Tareas
    [RelayCommand] private void GoToInbox() { var vm = (InboxViewModel)_pageFactory.GetPageViewModel(ApplicationPageNames.UserInbox); vm.SetModo(ModoVista.Inbox); CurrentPage = vm; }
    [RelayCommand] private void GoToHoy() { var vm = (InboxViewModel)_pageFactory.GetPageViewModel(ApplicationPageNames.UserInbox); vm.SetModo(ModoVista.Hoy); CurrentPage = vm; }
    [RelayCommand] private void GoToSemana() { var vm = (InboxViewModel)_pageFactory.GetPageViewModel(ApplicationPageNames.UserInbox); vm.SetModo(ModoVista.Semana); CurrentPage = vm; }
    [RelayCommand] private void GoToMes() { var vm = (InboxViewModel)_pageFactory.GetPageViewModel(ApplicationPageNames.UserInbox); vm.SetModo(ModoVista.Mes); CurrentPage = vm; }
    [RelayCommand] private void GoToNuevaArea() => CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.UserAreaInteres);

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
    public bool IsInboxActive => CurrentPage is InboxViewModel vm && vm.ModoActual == ModoVista.Inbox;
    public bool IsHoyActive => CurrentPage is InboxViewModel vm && vm.ModoActual == ModoVista.Hoy;
    public bool IsSemanaActive => CurrentPage is InboxViewModel vm && vm.ModoActual == ModoVista.Semana;
    public bool IsMesActive => CurrentPage is InboxViewModel vm && vm.ModoActual == ModoVista.Mes;
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