using Avalonia.Controls;
using Avalonia.Interactivity;
using planificApp.Helpers;

namespace planificApp.Views;

public partial class CalendarioSemanalView : UserControl
{
    public CalendarioSemanalView()
    {
        InitializeComponent();
    }

    private void GenerarSemana_Click(object? sender, RoutedEventArgs e)
    {
        DialogHelper.ShowGenerarSemanaDialog(this);
    }
}