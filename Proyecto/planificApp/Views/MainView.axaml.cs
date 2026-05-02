using Avalonia.Controls;
using Avalonia.Input;
using planificApp.ViewModels;

namespace planificApp;

public partial class MainView : Window
{
    public MainView()
    {
        InitializeComponent();
    }

    private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.ClickCount != 1)
            return;
        
        (DataContext as MainViewModel)?.SideMenuResizeCommand.Execute(null);
    }
}