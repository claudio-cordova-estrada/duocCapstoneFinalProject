using Avalonia.Controls;
using Avalonia.Interactivity;

namespace planificApp.Views;

public partial class ConfirmDeleteUsuarioWindow : Window
{
    public ConfirmDeleteUsuarioWindow()
    {
        InitializeComponent();
    }

    public void SetUsuarioName(string name)
    {
        DeleteTitle.Text = $"\"{name}\" se eliminará permanentemente";
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void DeleteButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }
}
