using CommunityToolkit.Mvvm.ComponentModel;

namespace planificApp.ViewModels;

public partial class NewTaskViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _taskName = string.Empty;
}