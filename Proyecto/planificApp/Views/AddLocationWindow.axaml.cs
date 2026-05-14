using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace planificApp.Views;

public partial class AddLocationWindow : Window
{
    private string _selectedColor = "#a78bfa";
    private string _selectedTransport = "Metro";
    private bool _isEditMode = false;

    public AddLocationWindow()
    {
        InitializeComponent();
        SelectColor(_selectedColor);
        SelectTransport(_selectedTransport);
    }

    public void SetEditMode(string name, string type, string color, string transport)
    {
        _isEditMode = true;
        Title = "Editar ubicaci\u00f3n";
        NameInput.Text = name;
        DeleteButton.IsVisible = true;

        _selectedColor = color;
        _selectedTransport = transport;

        // Set area
        for (int i = 0; i < AreaSelect.ItemCount; i++)
        {
            if (AreaSelect.Items[i] is ComboBoxItem item && item.Content?.ToString() == type)
            {
                AreaSelect.SelectedIndex = i;
                break;
            }
        }

        SelectColor(_selectedColor);
        SelectTransport(_selectedTransport);
    }

    private void ColorOption_Tapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border b && b.Tag is string color)
        {
            _selectedColor = color;
            SelectColor(color);
        }
    }

    private void SelectColor(string color)
    {
        foreach (var child in ColorGrid.Children)
        {
            if (child is Border b)
            {
                if (b.Tag?.ToString() == color)
                {
                    b.BorderThickness = new Thickness(2);
                    b.BorderBrush = new SolidColorBrush(Colors.White);
                    b.Width = 26;
                    b.Height = 26;
                }
                else
                {
                    b.BorderThickness = new Thickness(0);
                    b.Width = 22;
                    b.Height = 22;
                }
            }
        }
    }

    private void TransportOption_Tapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border b && b.Tag is string transport)
        {
            _selectedTransport = transport;
            SelectTransport(transport);
        }
    }

    private void SelectTransport(string transport)
    {
        foreach (var child in TransportGrid.Children)
        {
            if (child is Border b)
            {
                bool isSelected = b.Tag?.ToString() == transport;
                if (isSelected)
                {
                    b.Background = new SolidColorBrush(Color.Parse("#1e1a2e"));
                    b.BorderBrush = new SolidColorBrush(Color.Parse("#3d3060"));
                    if (b.Child is TextBlock tb)
                        tb.Foreground = new SolidColorBrush(Color.Parse("#a78bfa"));
                }
                else
                {
                    b.Background = new SolidColorBrush(Color.Parse("#1a1a1a"));
                    b.BorderBrush = new SolidColorBrush(Color.Parse("#222222"));
                    if (b.Child is TextBlock tb)
                        tb.Foreground = new SolidColorBrush(Color.Parse("#555555"));
                }
            }
        }
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void DeleteButton_Click(object? sender, RoutedEventArgs e)
    {
        // In a real app, this would trigger a delete confirmation
        // For the prototype, just close
        Close(false);
    }
}