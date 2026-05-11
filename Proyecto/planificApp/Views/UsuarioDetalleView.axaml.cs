using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using planificApp.ViewModels;

namespace planificApp.Views;

public partial class UsuarioDetalleView : UserControl
{
    private static readonly string[] Months = { "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic" };
    private static readonly string[] MetricKeys = { "tc", "tco", "gr", "gm", "tg", "tm" };
    private static readonly string[] MetricLabels = { "Tareas creadas", "Tareas completadas", "Generaciones realizadas", "Generaciones modificadas", "Tareas en G.S.", "Tareas mod. en G.S." };
    private static readonly Dictionary<string, int> MetricBases = new() { ["tc"]=310, ["tco"]=270, ["gr"]=22, ["gm"]=8, ["tg"]=140, ["tm"]=35 };
    private const int NowY = 2026, NowM = 4, MinY = 2024;

    private const string SelectRegion = "Seleccione una región";
    private const string SelectComuna = "Seleccione una comuna";
    private static readonly string[] Regiones = { SelectRegion, "Metropolitana", "Valparaíso", "Bío-Bío", "La Araucanía" };
    private static readonly Dictionary<string, string[]> ComunasPorRegion = new()
    {
        [SelectRegion] = new[] { SelectComuna },
        ["Metropolitana"] = new[] { "Santiago", "Providencia", "Ñuñoa", "Maipú" },
        ["Valparaíso"] = new[] { "Valparaíso", "Viña del Mar" },
        ["Bío-Bío"] = new[] { "Concepción" },
        ["La Araucanía"] = new[] { "Temuco" }
    };

    private readonly SolidColorBrush _readonlyBg = new(Color.Parse("#00000000"));
    private readonly SolidColorBrush _readonlyBorder = new(Color.Parse("#00000000"));
    private readonly SolidColorBrush _editBg = new(Color.Parse("#0f0f0f"));
    private readonly SolidColorBrush _editBorder = new(Color.Parse("#5c3a1a"));
    private readonly SolidColorBrush _fieldFg = new(Color.Parse("#cccccc"));

    private readonly Dictionary<TextBox, Label> _fieldIcons = new();
    private int _detailYear = NowY;
    private int _detailMonth = NowM;

    public UsuarioDetalleView()
    {
        InitializeComponent();

        _fieldIcons[FieldName] = IconName;
        _fieldIcons[FieldCorreo] = IconCorreo;
        _fieldIcons[FieldFechaNac] = IconFechaNac;

        FieldName.GotFocus += Field_GotFocus;
        FieldCorreo.GotFocus += Field_GotFocus;
        FieldFechaNac.GotFocus += Field_GotFocus;

        SetupRegionComunaDropdowns();

        UpdateDetailYearMonth();
        UpdateDetailMetrics();

        UserName.Text = "Carlos López";
        UserId.Text = "USR001";
        StatusText.Text = "Activo";
        StatusBadge.Background = new SolidColorBrush(Color.Parse("#0f2a1a"));
        StatusBadge.BorderBrush = new SolidColorBrush(Color.Parse("#4ade8033"));
        StatusBadge.BorderThickness = new Thickness(1);
        StatusText.Foreground = new SolidColorBrush(Color.Parse("#4ade80"));
        FieldMeses.Text = "14 meses";
        FieldRegistro.Text = "12 mar 2025";
    }

    private void SetupRegionComunaDropdowns()
    {
        FieldRegion.ItemsSource = Regiones;
        FieldRegion.SelectedIndex = 0;
        FieldRegion.SelectionChanged += Region_SelectionChanged;

        FieldComuna.ItemsSource = new[] { SelectComuna };
        FieldComuna.SelectedIndex = 0;
    }

    private void Region_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var region = FieldRegion.SelectedItem as string ?? "";
        if (region == SelectRegion || string.IsNullOrEmpty(region))
        {
            FieldComuna.ItemsSource = new[] { SelectComuna };
            FieldComuna.IsEnabled = false;
            FieldComuna.SelectedIndex = 0;
        }
        else
        {
            var comunas = ComunasPorRegion.GetValueOrDefault(region) ?? Array.Empty<string>();
            var items = new List<string> { SelectComuna };
            items.AddRange(comunas);
            FieldComuna.ItemsSource = items;
            FieldComuna.IsEnabled = true;
            FieldComuna.SelectedIndex = 0;
        }
    }

    private void EnterEditMode(TextBox field)
    {
        if (!field.IsReadOnly) return;
        field.IsReadOnly = false;
        field.Background = _editBg;
        field.BorderBrush = _editBorder;
        field.BorderThickness = new Thickness(1);
        field.Foreground = _fieldFg;
        if (_fieldIcons.TryGetValue(field, out var icon))
        {
            icon.Content = "\xEBA6";
            icon.Foreground = new SolidColorBrush(Color.Parse("#fb923c"));
        }
        field.Focus();
        field.SelectAll();
    }

    private void ExitEditMode(TextBox field)
    {
        if (field.IsReadOnly) return;
        field.IsReadOnly = true;
        field.Background = _readonlyBg;
        field.BorderBrush = _readonlyBorder;
        field.BorderThickness = new Thickness(0);
        field.Foreground = _fieldFg;
        if (_fieldIcons.TryGetValue(field, out var icon))
        {
            icon.Content = "\xE3B2";
            icon.Foreground = new SolidColorBrush(Color.Parse("#888888"));
        }
    }

    private void Field_GotFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is not TextBox tb || !tb.IsReadOnly) return;
        if (tb == FieldName) EnterEditMode(FieldName);
        else if (tb == FieldCorreo) EnterEditMode(FieldCorreo);
        else if (tb == FieldFechaNac) EnterEditMode(FieldFechaNac);
    }

    private void BtnName_Click(object? sender, RoutedEventArgs e) { if (FieldName.IsReadOnly) EnterEditMode(FieldName); else ExitEditMode(FieldName); }
    private void BtnCorreo_Click(object? sender, RoutedEventArgs e) { if (FieldCorreo.IsReadOnly) EnterEditMode(FieldCorreo); else ExitEditMode(FieldCorreo); }
    private void BtnFechaNac_Click(object? sender, RoutedEventArgs e) { if (FieldFechaNac.IsReadOnly) EnterEditMode(FieldFechaNac); else ExitEditMode(FieldFechaNac); }

    private static int GetMet(string key, int year, int month)
    {
        int b = MetricBases.GetValueOrDefault(key, 0);
        int seed = (year * 12 + month) * 7 + key.Length;
        return Math.Max(0, (int)Math.Round(b * (0.4 + 0.8 * ((seed % 17) / 17.0))));
    }

    private static bool IsFuture(int y, int m) => y > NowY || (y == NowY && m > NowM);

    private void UpdateDetailYearMonth()
    {
        DetailYearLabel.Text = _detailYear.ToString();
        DetailMonthPills.Children.Clear();
        for (int i = 0; i < 12; i++)
        {
            var btn = new Button
            {
                Content = Months[i],
                FontSize = 16,
                Padding = new Thickness(8, 6),
                Classes = { "pill-rounded", "admin" },
            };
            if (i == _detailMonth) btn.Classes.Add("active");
            if (IsFuture(_detailYear, i)) { btn.Classes.Add("disabled"); btn.IsEnabled = false; }
            var idx = i;
            btn.Click += (s, e) =>
            {
                if (!IsFuture(_detailYear, idx))
                {
                    _detailMonth = idx;
                    UpdateDetailYearMonth();
                    UpdateDetailMetrics();
                }
            };
            DetailMonthPills.Children.Add(btn);
        }
    }

    private void UpdateDetailMetrics()
    {
        DetailMetricsGrid.Children.Clear();
        for (int row = 0; row < 2; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                int idx = row * 3 + col;
                if (idx >= MetricKeys.Length) break;
                var key = MetricKeys[idx];
                var border = new Border
                {
                    Background = new SolidColorBrush(Color.Parse("#0f0f0f")),
                    BorderBrush = new SolidColorBrush(Color.Parse("#1a1a1a")),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(14),
                    [Grid.RowProperty] = row,
                    [Grid.ColumnProperty] = col,
                };
                var panel = new StackPanel();
                panel.Children.Add(new TextBlock { Text = MetricLabels[idx], FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#444")), TextWrapping = TextWrapping.Wrap, LetterSpacing = 0.4 });
                panel.Children.Add(new TextBlock { Text = GetMet(key, _detailYear, _detailMonth).ToString(), FontSize = 22, FontWeight = FontWeight.SemiBold, Foreground = new SolidColorBrush(Color.Parse("#e2e8f0")), Margin = new Thickness(0, 6, 0, 0) });
                border.Child = panel;
                DetailMetricsGrid.Children.Add(border);
            }
        }
    }

    private void DetailYearLeft_Click(object? sender, RoutedEventArgs e)
    {
        if (_detailYear > MinY) { _detailYear--; UpdateDetailYearMonth(); UpdateDetailMetrics(); }
    }

    private void DetailYearRight_Click(object? sender, RoutedEventArgs e)
    {
        if (_detailYear < NowY) { _detailYear++; if (IsFuture(_detailYear, _detailMonth)) _detailMonth = NowM; UpdateDetailYearMonth(); UpdateDetailMetrics(); }
    }

    private void Back_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.DataContext is MainViewModel vm)
            vm.GoToAdminUsuariosCommand.Execute(null);
    }

    private async void Desactivar_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new Window
        {
            Title = "Desactivar usuario",
            Width = 380,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Background = new SolidColorBrush(Color.Parse("#1a1a1a")),
        };

        var stack = new StackPanel { Margin = new Thickness(24), Spacing = 16, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
        stack.Children.Add(new TextBlock { Text = "¿Estás seguro de que quieres desactivar este usuario?", FontSize = 15, Foreground = new SolidColorBrush(Color.Parse("#e2e8f0")), TextWrapping = TextWrapping.Wrap });

        var btnStack = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right, Spacing = 8 };
        var cancelBtn = new Button { Content = "Cancelar", Classes = { "transparent" }, Padding = new Thickness(14, 7), FontSize = 13 };
        var deactiveBtn = new Button { Content = "Desactivar", Classes = { "danger" }, Padding = new Thickness(14, 7), FontSize = 13 };
        btnStack.Children.Add(cancelBtn);
        btnStack.Children.Add(deactiveBtn);
        stack.Children.Add(btnStack);
        dialog.Content = stack;

        cancelBtn.Click += (_, _) => dialog.Close();
        deactiveBtn.Click += (_, _) =>
        {
            dialog.Close();
            if (StatusText.Text == "Activo")
            {
                StatusText.Text = "Inactivo";
                StatusBadge.Background = new SolidColorBrush(Color.Parse("#1a1a1a"));
                StatusBadge.BorderBrush = new SolidColorBrush(Color.Parse("#2a2a2a"));
                StatusText.Foreground = new SolidColorBrush(Color.Parse("#555"));
            }
        };

        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner == null) return;
        await dialog.ShowDialog(owner);
    }

    private void Guardar_Click(object? sender, RoutedEventArgs e)
    {
        ExitEditMode(FieldName);
        ExitEditMode(FieldCorreo);
        ExitEditMode(FieldFechaNac);

        SuccessMessage.IsVisible = true;
        SuccessMessage.Opacity = 1;

        DispatcherTimer.RunOnce(() =>
        {
            FadeOut(SuccessMessage);
        }, TimeSpan.FromSeconds(2));
    }

    private async void FadeOut(TextBlock element)
    {
        for (int i = 9; i >= 0; i--)
        {
            element.Opacity = i / 10.0;
            await Task.Delay(80);
        }
        element.IsVisible = false;
        element.Opacity = 1;
    }
}