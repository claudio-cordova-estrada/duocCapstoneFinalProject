using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using planificApp.Services;

namespace planificApp.Views;

public partial class CalendarioSemanalView : UserControl
{
    public CalendarioSemanalView()
    {
        InitializeComponent();
    }

    private void GenerarSemana_Click(object? sender, RoutedEventArgs e)
    {
        var dialogService = App.Services.GetRequiredService<IDialogService>();
        dialogService.ShowGenerarSemanaDialog();
    }
}