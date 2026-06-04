using Avalonia.Controls;
using Avalonia.Interactivity;

namespace planificApp.Views;

public partial class ConfirmDeleteAreaWindow : Window
{
    public ConfirmDeleteAreaWindow()
    {
        InitializeComponent();
    }

    public void SetAreaName(string name)
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