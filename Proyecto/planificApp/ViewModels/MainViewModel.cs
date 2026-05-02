using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace planificApp.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty] 
    private bool _sideMenuExpanded = true;
    
    [ObservableProperty] 
    private string _user = "Nicolás de Suárez";

    [RelayCommand]
    private void SideMenuResize()
    {
        SideMenuExpanded = !SideMenuExpanded;
    }
}