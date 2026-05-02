using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace planificApp.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private const string buttonActiveClass = "active";
    
    [ObservableProperty] 
    private bool _sideMenuExpanded = true;
    
    [ObservableProperty] 
    private string _user = "Nicolás de Suárez";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InboxPageIsActive))]
    [NotifyPropertyChangedFor(nameof(CalendarioSemanalPageIsActive))]
    private ViewModelBase _currentPage;

    public bool InboxPageIsActive => CurrentPage == _inboxPage;
    public bool CalendarioSemanalPageIsActive => CurrentPage == _calendarioSemanalPage;

    private readonly InboxViewModel _inboxPage = new();
    private readonly TodayViewModel _todayPage = new();
    private readonly CalendarioSemanalViewModel _calendarioSemanalPage = new();

    public MainViewModel()
    {
        CurrentPage = _inboxPage;
    }

    [RelayCommand]
    private void SideMenuResize()
    {
        SideMenuExpanded = !SideMenuExpanded;
    }

    [RelayCommand]
    private void GoToInbox()
    {
        CurrentPage = _inboxPage;
    }
    
    [RelayCommand]
    private void GoToCalendarioSemanal()
    {
        CurrentPage = _calendarioSemanalPage;
    }
}