using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using planificApp.Data;
using planificApp.Factories;

namespace planificApp.ViewModels;

public partial class AdminMainViewModel : ViewModelBase
{
    private readonly PageFactory _pageFactory;
    
    [ObservableProperty] private AdminSection _activeAdminSection = AdminSection.Estadisticas;
    [ObservableProperty] private PageViewModel _currentPage;

    public AdminMainViewModel(PageFactory pageFactory)
    {
        _pageFactory = pageFactory;
        CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.AdminEstadisticas);
    }

    [RelayCommand] private void GoToEstadisticas()
    {
        ActiveAdminSection = AdminSection.Estadisticas;
        CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.AdminEstadisticas);
    }

    [RelayCommand] private void GoToUsuarios()
    {
        ActiveAdminSection = AdminSection.Usuarios;
        CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.AdminUsuarios);
    }

    [RelayCommand] private void GoToUsuarioDetalle() =>
        CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.AdminUsuarioDetalle);

    public bool IsEstadisticasActive => ActiveAdminSection == AdminSection.Estadisticas;
    public bool IsUsuariosActive => ActiveAdminSection == AdminSection.Usuarios;
}
