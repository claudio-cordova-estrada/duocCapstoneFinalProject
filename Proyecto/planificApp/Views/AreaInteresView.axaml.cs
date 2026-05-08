using Avalonia.Controls;
using Avalonia.Interactivity;
using planificApp.Helpers;

namespace planificApp.Views;

public partial class AreaInteresView : UserControl
{
    public AreaInteresView()
    {
        InitializeComponent();
    }

    private void NewTaskButton_Click(object? sender, RoutedEventArgs e)
    {
        DialogHelper.ShowNewTaskDialog(this);
    }
}