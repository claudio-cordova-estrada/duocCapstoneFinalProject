using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using planificApp.Helpers;

namespace planificApp.Views;

public partial class UbicacionesView : UserControl
{
    private record LocData(string Name, string Type, string Color, string Transport, string LastVisit, double PinX, double PinY);

    private readonly LocData[] _locations = new[]
    {
        new LocData("Casa", "Hogar", "#34d399", "Metro", "12 abr 2026", 0.38, 0.52),
        new LocData("Trabajo", "Trabajo", "#a78bfa", "Metro", "06 may 2026", 0.55, 0.34),
        new LocData("Iglesia", "Social", "#f472b6", "A pie", "28 abr 2026", 0.30, 0.38),
        new LocData("M\u00e9dico 1", "Salud", "#60a5fa", "Auto", "01 mar 2026", 0.62, 0.58),
        new LocData("M\u00e9dico 2", "Salud", "#60a5fa", "Bus", "15 feb 2026", 0.72, 0.42),
    };

    private readonly Border[] _locItems;
    private readonly StackPanel?[] _actionPanels;
    private readonly Border[] _pins;
    private int _activeIndex = 0;

    public UbicacionesView()
    {
        InitializeComponent();

        _locItems = new[] { LocCasa, LocTrabajo, LocIglesia, LocMedico1, LocMedico2 };
        _actionPanels = new StackPanel?[] { ActionsCasa, ActionsTrabajo, ActionsIglesia, ActionsMedico1, ActionsMedico2 };

        // Create map pins
        _pins = new Border[_locations.Length];
        for (int i = 0; i < _locations.Length; i++)
        {
            var pin = new Border
            {
                Width = i == 0 ? 15 : 11,
                Height = i == 0 ? 15 : 11,
                CornerRadius = new CornerRadius(i == 0 ? 8 : 6),
                Background = new SolidColorBrush(Color.Parse(_locations[i].Color)),
                BorderBrush = new SolidColorBrush(Color.Parse("#0f0f0f")),
                BorderThickness = new Thickness(i == 0 ? 2 : 2),
                Cursor = new Cursor(StandardCursorType.Hand),
                Tag = i
            };

            if (i == 0)
            {
                pin.BoxShadow = new BoxShadows(new BoxShadow
                {
                    Color = Color.Parse(_locations[i].Color),
                    OffsetX = 0, OffsetY = 0, Blur = 6, Spread = 2
                });
            }

            pin.PointerPressed += (s, e) =>
            {
                if (s is Border b && b.Tag is int idx)
                    SelectLocation(idx);
            };

            _pins[i] = pin;
            PinCanvas.Children.Add(pin);
        }

        MapContainer.SizeChanged += (_, _) => UpdatePinPositions();
        UpdateDetailPanel(0);
    }

    private void UpdatePinPositions()
    {
        var w = MapContainer.Bounds.Width;
        var h = MapContainer.Bounds.Height;
        if (w <= 0 || h <= 0) return;

        for (int i = 0; i < _locations.Length; i++)
        {
            var pin = _pins[i];
            var x = w * _locations[i].PinX - pin.Width / 2;
            var y = h * _locations[i].PinY - pin.Height;
            Canvas.SetLeft(pin, x);
            Canvas.SetTop(pin, y);
        }
    }

    private void SelectLocation(int index)
    {
        if (index < 0 || index >= _locations.Length) return;

        // Remove active from all items
        for (int i = 0; i < _locItems.Length; i++)
        {
            _locItems[i].Classes.Remove("active");
        }

        // Add active to selected
        _locItems[index].Classes.Add("active");

        // Update action panels visibility
        for (int i = 0; i < _actionPanels.Length; i++)
        {
            if (_actionPanels[i] != null)
                _actionPanels[i]!.IsVisible = i == index;
        }

        // Update pins
        for (int i = 0; i < _pins.Length; i++)
        {
            if (i == index)
            {
                _pins[i].Width = 15;
                _pins[i].Height = 15;
                _pins[i].CornerRadius = new CornerRadius(8);
                _pins[i].BoxShadow = new BoxShadows(new BoxShadow
                {
                    Color = Color.Parse(_locations[i].Color),
                    OffsetX = 0, OffsetY = 0, Blur = 6, Spread = 2
                });
            }
            else
            {
                _pins[i].Width = 11;
                _pins[i].Height = 11;
                _pins[i].CornerRadius = new CornerRadius(6);
                _pins[i].BoxShadow = default;
            }
        }

        UpdatePinPositions();
        UpdateDetailPanel(index);
        _activeIndex = index;
    }

    private void UpdateDetailPanel(int index)
    {
        var loc = _locations[index];
        DetailName.Text = loc.Name;
        DetailName.Foreground = new SolidColorBrush(Color.Parse(loc.Color));
        DetailType.Text = loc.Type;
        DetailVisit.Text = loc.LastVisit;
        DetailTransport.Text = loc.Transport;
    }

    // Location item tap handlers
    private void LocCasa_Tapped(object? sender, TappedEventArgs e) => SelectLocation(0);
    private void LocTrabajo_Tapped(object? sender, TappedEventArgs e) => SelectLocation(1);
    private void LocIglesia_Tapped(object? sender, TappedEventArgs e) => SelectLocation(2);
    private void LocMedico1_Tapped(object? sender, TappedEventArgs e) => SelectLocation(3);
    private void LocMedico2_Tapped(object? sender, TappedEventArgs e) => SelectLocation(4);

    // Edit/Delete handlers
    private void EditCasa_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => DialogHelper.ShowEditLocationDialog(this);
    private void DeleteCasa_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => DialogHelper.ShowConfirmDeleteLocationDialog(this);
    private void EditTrabajo_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => DialogHelper.ShowEditLocationDialog(this);
    private void DeleteTrabajo_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => DialogHelper.ShowConfirmDeleteLocationDialog(this);
    private void EditIglesia_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => DialogHelper.ShowEditLocationDialog(this);
    private void DeleteIglesia_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => DialogHelper.ShowConfirmDeleteLocationDialog(this);
    private void EditMedico1_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => DialogHelper.ShowEditLocationDialog(this);
    private void DeleteMedico1_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => DialogHelper.ShowConfirmDeleteLocationDialog(this);
    private void EditMedico2_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => DialogHelper.ShowEditLocationDialog(this);
    private void DeleteMedico2_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => DialogHelper.ShowConfirmDeleteLocationDialog(this);

    // Add location
    private void AddLocation_Tapped(object? sender, TappedEventArgs e) => DialogHelper.ShowAddLocationDialog(this);
}