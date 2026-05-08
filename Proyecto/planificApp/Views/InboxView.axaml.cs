using Avalonia.Controls;
using Avalonia.Interactivity;
using planificApp.Helpers;

namespace planificApp.Views;

public partial class InboxView : UserControl
{
    public InboxView()
    {
        InitializeComponent();
    }

    private void NewTaskButton_Click(object? sender, RoutedEventArgs e)
    {
        DialogHelper.ShowNewTaskDialog(this);
    }
}