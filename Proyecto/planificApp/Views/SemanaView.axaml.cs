using Avalonia.Controls;
using Avalonia.Interactivity;
using planificApp.Helpers;

namespace planificApp.Views;

public partial class SemanaView : UserControl
{
    public SemanaView()
    {
        InitializeComponent();
    }

    private void NewTaskButton_Click(object? sender, RoutedEventArgs e)
    {
        DialogHelper.ShowNewTaskDialog(this);
    }
}