using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using planificApp.Data;
using planificApp.Factories;

namespace planificApp.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private PageFactory _pageFactory;
    private const string buttonActiveClass = "active";
    
    [ObservableProperty] 
    private bool _sideMenuExpanded = true;
    
    [ObservableProperty] 
    private string _user = "Nicolás de Suárez";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InboxPageIsActive))]
    [NotifyPropertyChangedFor(nameof(CalendarioSemanalPageIsActive))]
    [NotifyPropertyChangedFor(nameof(TodayTaskPageIsActive))]
    [NotifyPropertyChangedFor(nameof(SettingsPageIsActive))]
    [NotifyPropertyChangedFor(nameof(AreaInteresPageIsActive))]
    [NotifyPropertyChangedFor(nameof(LocationPageIsActive))]
    [NotifyPropertyChangedFor(nameof(CalendarioMensualPageIsActive))]
    [NotifyPropertyChangedFor(nameof(SugerenciasSemanalesPageIsActive))]
    [NotifyPropertyChangedFor(nameof(CuentaPageIsActive))]
    [NotifyPropertyChangedFor(nameof(SoportePageIsActive))]
    [NotifyPropertyChangedFor(nameof(AboutPageIsActive))]
    [NotifyPropertyChangedFor(nameof(EstadisticaUsuarioPageIsActive))]
    [NotifyPropertyChangedFor(nameof(ModificacionUsuarioPageIsActive))]
    [NotifyPropertyChangedFor(nameof(BusquedaUsuarioPageIsActive))]
    [NotifyPropertyChangedFor(nameof(DetalleUsuarioPageIsActive))]
    [NotifyPropertyChangedFor(nameof(LoginPageIsActive))]
    [NotifyPropertyChangedFor(nameof(RegistroPageIsActive))]
    [NotifyPropertyChangedFor(nameof(RecuperarContraPageIsActive))]
    private PageViewModel _currentPage;

    public bool InboxPageIsActive => CurrentPage.PageName == ApplicationPageNames.Inbox;
    public bool CalendarioSemanalPageIsActive => CurrentPage.PageName == ApplicationPageNames.CalendarioSemanal;
    public bool TodayTaskPageIsActive => CurrentPage.PageName == ApplicationPageNames.TodayTask;
    public bool SettingsPageIsActive => CurrentPage.PageName == ApplicationPageNames.Settings;
    public bool AreaInteresPageIsActive => CurrentPage.PageName == ApplicationPageNames.AreaInteres;
    public bool LocationPageIsActive => CurrentPage.PageName == ApplicationPageNames.Location;
    public bool CalendarioMensualPageIsActive => CurrentPage.PageName == ApplicationPageNames.CalendarioMensual;
    public bool SugerenciasSemanalesPageIsActive => CurrentPage.PageName == ApplicationPageNames.SugerenciasSemanales;
    public bool CuentaPageIsActive => CurrentPage.PageName == ApplicationPageNames.Cuenta;
    public bool SoportePageIsActive => CurrentPage.PageName == ApplicationPageNames.Soporte;
    public bool AboutPageIsActive => CurrentPage.PageName == ApplicationPageNames.About;
    public bool EstadisticaUsuarioPageIsActive => CurrentPage.PageName == ApplicationPageNames.EstadisticaUsuario;
    public bool ModificacionUsuarioPageIsActive => CurrentPage.PageName == ApplicationPageNames.ModificacionUsuario;
    public bool BusquedaUsuarioPageIsActive => CurrentPage.PageName == ApplicationPageNames.BusquedaUsuario;
    public bool DetalleUsuarioPageIsActive => CurrentPage.PageName == ApplicationPageNames.DetalleUsuario;
    public bool LoginPageIsActive => CurrentPage.PageName == ApplicationPageNames.Login;
    public bool RegistroPageIsActive => CurrentPage.PageName == ApplicationPageNames.Registro;
    public bool RecuperarContraPageIsActive => CurrentPage.PageName == ApplicationPageNames.RecuperarContra;

    public MainViewModel()
    {
        CurrentPage = new InboxViewModel();
    }
    
    public MainViewModel(PageFactory pageFactory)
    {
        _pageFactory = pageFactory;
        GoToInbox();
    }

    [RelayCommand]
    private void SideMenuResize()
    {
        SideMenuExpanded = !SideMenuExpanded;
    }

    [RelayCommand]
    private void GoToInbox() => CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.Inbox);
    
    [RelayCommand]
    private void GoToCalendarioSemanal() => CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.CalendarioSemanal);
    
    [RelayCommand]
    private void GoToToday() => CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.TodayTask);
    
    [RelayCommand]
    private void GoToSettings() => CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.Settings);
    
    [RelayCommand]
    private void GoToAreaInteres() => CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.AreaInteres);
    
    [RelayCommand]
    private void GoToLocation() => CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.Location);
    
    [RelayCommand]
    private void GoToCalendarioMensual() => CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.CalendarioMensual);
    
    [RelayCommand]
    private void GoToSugerenciasSemanales() => CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.SugerenciasSemanales);
    
    [RelayCommand]
    private void GoToCuenta() => CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.Cuenta);
    
    [RelayCommand]
    private void GoToSoporte() => CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.Soporte);
    
    [RelayCommand]
    private void GoToAbout() => CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.About);
    
    [RelayCommand]
    private void GoToEstadisticaUsuario() => CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.EstadisticaUsuario);
    
    [RelayCommand]
    private void GoToModificacionUsuario() => CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.ModificacionUsuario);
    
    [RelayCommand]
    private void GoToBusquedaUsuario() => CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.BusquedaUsuario);
    
    [RelayCommand]
    private void GoToDetalleUsuario() => CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.DetalleUsuario);
    
    [RelayCommand]
    private void GoToLogin() => CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.Login);
    
    [RelayCommand]
    private void GoToRegistro() => CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.Registro);
    
    [RelayCommand]
    private void GoToRecuperarContra() => CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.RecuperarContra);
}