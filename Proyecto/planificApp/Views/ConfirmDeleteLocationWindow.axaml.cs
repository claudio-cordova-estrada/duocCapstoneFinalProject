using Avalonia.Controls;
using Avalonia.Interactivity;

namespace planificApp.Views;

public partial class ConfirmDeleteLocationWindow : Window
{
    public ConfirmDeleteLocationWindow()
    {
        InitializeComponent();
    }

    public void SetLocationName(string name)
    {
        DeleteTitle.Text = $"\u00bfEliminar \"{name}\"?";
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