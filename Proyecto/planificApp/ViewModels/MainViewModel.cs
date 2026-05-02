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
    private PageViewModel _currentPage;

    public bool InboxPageIsActive => CurrentPage.PageName == ApplicationPageNames.Inbox;
    public bool CalendarioSemanalPageIsActive => CurrentPage.PageName == ApplicationPageNames.CalendarioSemanal;

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
    private void GoToToday() => CurrentPage = _pageFactory.GetPageViewModel(ApplicationPageNames.Today);
}