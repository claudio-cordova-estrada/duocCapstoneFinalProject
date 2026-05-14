using Avalonia.Controls;
using Avalonia.Interactivity;
using planificApp.Helpers;

namespace planificApp.Views;

public partial class MesView : UserControl
{
    public MesView()
    {
        InitializeComponent();
    }

    private void NewTaskButton_Click(object? sender, RoutedEventArgs e)
    {
        DialogHelper.ShowNewTaskDialog(this);
    }
}