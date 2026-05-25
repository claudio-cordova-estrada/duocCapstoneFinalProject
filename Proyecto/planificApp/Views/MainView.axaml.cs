using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using planificApp.Helpers;
using planificApp.ViewModels;

namespace planificApp;

public partial class MainView : Window
{
    public MainView()
    {
        InitializeComponent();
    }

    private async void NewAreaInteresButton_Click(object? sender, RoutedEventArgs e)
    {
        var result = await DialogHelper.ShowNewAreaDialog(this);
        if (result && DataContext is MainViewModel vm)
        {
            await vm.ReloadAreasAsync();
        }
    }
}