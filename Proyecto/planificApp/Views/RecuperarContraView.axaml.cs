using Avalonia.Controls;
using Avalonia.Interactivity;

namespace planificApp.Views;

public partial class RecuperarContraView : UserControl
{
    public RecuperarContraView()
    {
        InitializeComponent();
    }

    private async void Enviar_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new Window
        {
            Title = "Correo enviado",
            Width = 400,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Background = Avalonia.Media.Brush.Parse("#141414"),
        };

        var stack = new StackPanel { Margin = new Avalonia.Thickness(24), Spacing = 16, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };

        stack.Children.Add(new TextBlock
        {
            Text = "Correo enviado",
            FontSize = 16,
            FontWeight = Avalonia.Media.FontWeight.Medium,
            Foreground = Avalonia.Media.Brush.Parse("#e2e8f0"),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
        });

        stack.Children.Add(new TextBlock
        {
            Text = "Se envió un correo para recuperar tu contraseña. Revisa tu bandeja de entrada.",
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
            ViewModels.MainViewModel? mainVm = null;
            if (DataContext is ViewModels.RecuperarContraViewModel rcvm) mainVm = rcvm.Main;
            if (mainVm != null) mainVm.GoToLoginCommand.Execute(null);
        };

        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner == null) return;
        await dialog.ShowDialog(owner);
    }
}