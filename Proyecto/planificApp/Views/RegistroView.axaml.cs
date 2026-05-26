using Avalonia.Controls;
using Avalonia.Interactivity;
using planificApp.ViewModels;

namespace planificApp.Views;

public partial class RegistroView : UserControl
{
    public RegistroView()
    {
        InitializeComponent();
        DatePickerNacimiento.SelectedDateChanged += DatePickerNacimiento_SelectedDateChanged;
    }

    private void DatePickerNacimiento_SelectedDateChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is RegistroViewModel vm && DatePickerNacimiento.SelectedDate.HasValue)
        {
            vm.FecNacimiento = DatePickerNacimiento.SelectedDate.Value;
        }
    }

    private async void CrearCuenta_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not RegistroViewModel vm) return;
        await vm.RegistroCommand.ExecuteAsync(null);

        if (!vm.RegistroExitoso) return;

        var dialog = new Window
        {
            Title = "Cuenta creada",
            Width = 400,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Background = Avalonia.Media.Brush.Parse("#141414"),
        };

        var stack = new StackPanel { Margin = new Avalonia.Thickness(24), Spacing = 16, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };

        stack.Children.Add(new TextBlock
        {
            Text = "¡Cuenta creada exitosamente!",
            FontSize = 16,
            FontWeight = Avalonia.Media.FontWeight.Medium,
            Foreground = Avalonia.Media.Brush.Parse("#e2e8f0"),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
        });

        stack.Children.Add(new TextBlock
        {
            Text = "Ya puedes iniciar sesión con tu cuenta.",
            FontSize = 13,
            Foreground = Avalonia.Media.Brush.Parse("#aaaaaa"),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            TextAlignment = Avalonia.Media.TextAlignment.Center,
        });

        var btn = new Button
        {
            Content = "Volver al login",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Classes = { "accent" },
            Padding = new Avalonia.Thickness(16, 8),
        };
        stack.Children.Add(btn);

        dialog.Content = stack;

        btn.Click += (_, _) =>
        {
            dialog.Close();
            vm.Main.GoToLoginCommand.Execute(null);
        };

        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner == null) return;
        await dialog.ShowDialog(owner);
    }
}
