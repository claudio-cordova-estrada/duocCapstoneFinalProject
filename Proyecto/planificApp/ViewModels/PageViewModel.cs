using CommunityToolkit.Mvvm.ComponentModel;
using planificApp.Data;

namespace planificApp.ViewModels;

public partial class PageViewModel : ViewModelBase
{
    [ObservableProperty]
    private ApplicationPageNames _pageName;
}