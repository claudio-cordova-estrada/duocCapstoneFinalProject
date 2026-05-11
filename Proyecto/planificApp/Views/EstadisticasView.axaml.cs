using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using planificApp.Data;

namespace planificApp.Views;

public partial class EstadisticasView : UserControl
{
    private static readonly string[] Months = { "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic" };
    private static readonly string[] Regions = { "Todas", "Metropolitana", "Valparaíso", "Bío-Bío", "La Araucanía" };
    private static readonly Dictionary<string, string[]> Comunas = new()
    {
        ["Todas"] = new[] { "Todas" },
        ["Metropolitana"] = new[] { "Todas", "Santiago", "Providencia", "Ñuñoa", "Maipú" },
        ["Valparaíso"] = new[] { "Todas", "Valparaíso", "Viña del Mar" },
        ["Bío-Bío"] = new[] { "Todas", "Concepción" },
        ["La Araucanía"] = new[] { "Todas", "Temuco" }
    };

    private static readonly string[] MetricKeys = { "tc", "tco", "gr", "gm", "tg", "tm" };
    private static readonly string[] MetricLabels = { "Tareas creadas", "Tareas completadas", "Generaciones realizadas", "Generaciones modificadas", "Tareas en G.S.", "Tareas mod. en G.S." };
    private static readonly Dictionary<string, int> MetricBases = new() { ["tc"]=310, ["tco"]=270, ["gr"]=22, ["gm"]=8, ["tg"]=140, ["tm"]=35 };

    private const int NowY = 2026, NowM = 4, MinY = 2024;

    private string _selectedRegion = "Todas";
    private string _selectedComuna = "Todas";
    private int _year = NowY;
    private int _month = NowM;

    public EstadisticasView()
    {
        InitializeComponent();
        BuildRegionPills();
        BuildComunaPills();
        UpdateYearMonth();
        UpdateStats();
        UpdateMetrics();
    }

    private void BuildRegionPills()
    {
        RegionPills.Children.Clear();
        foreach (var region in Regions)
        {
            var btn = new Button
            {
                Content = new TextBlock { Text = region, FontSize = 13, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center },
                Width = 110,
                Classes = { "pill-rounded", "admin" },
            };
            if (region == "Todas") btn.Classes.Add("active");
            btn.Click += (s, e) =>
            {
                foreach (var c in RegionPills.Children) if (c is Button b) b.Classes.Remove("active");
                ((Button)s!).Classes.Add("active");
                _selectedRegion = region;
                _selectedComuna = "Todas";
                BuildComunaPills();
            };
            RegionPills.Children.Add(btn);
        }
    }

    private void BuildComunaPills()
    {
        ComunaPills.Children.Clear();
        foreach (var comuna in Comunas[_selectedRegion])
        {
            var btn = new Button
            {
                Content = new TextBlock { Text = comuna, FontSize = 13, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center },
                Width = 110,
                Classes = { "pill-rounded", "admin" },
            };
            if (comuna == "Todas") btn.Classes.Add("active");
            btn.Click += (s, e) =>
            {
                foreach (var c in ComunaPills.Children) if (c is Button b) b.Classes.Remove("active");
                ((Button)s!).Classes.Add("active");
                _selectedComuna = comuna;
            };
            ComunaPills.Children.Add(btn);
        }
    }

    private void UpdateYearMonth()
    {
        YearLabel.Text = _year.ToString();
        MonthPills.Children.Clear();
        for (int i = 0; i < 12; i++)
        {
            var btn = new Button
            {
                Content = Months[i],
                FontSize = 16,
                Padding = new Avalonia.Thickness(8, 6),
                Classes = { "pill-rounded", "admin" },
            };
            if (i == _month) btn.Classes.Add("active");
            if (IsFuture(_year, i)) { btn.Classes.Add("disabled"); btn.IsEnabled = false; }
            var idx = i;
            btn.Click += (s, e) =>
            {
                if (!IsFuture(_year, idx))
                {
                    _month = idx;
                    UpdateYearMonth();
                    UpdateMetrics();
                }
            };
            MonthPills.Children.Add(btn);
        }
    }

    private void UpdateStats()
    {
        StatTotalUsuarios.Text = "7";
        StatUsuariosActivos.Text = "5";
        StatUsanGen.Text = "4";
        StatUsanGenSub.Text = "de 5 activos";
        StatCambiosGen.Text = "2.4";
    }

    private void UpdateMetrics()
    {
        MetricsMonthLabel.Text = $"{Months[_month]} {_year}";
        MetricsGrid.Children.Clear();
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
                    Padding = new Thickness(12),
                    [Grid.RowProperty] = row,
                    [Grid.ColumnProperty] = col,
                };
                var panel = new StackPanel();
                panel.Children.Add(new TextBlock { Text = MetricLabels[idx], FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#444")), TextWrapping = TextWrapping.Wrap, LetterSpacing = 0.4 });
                panel.Children.Add(new TextBlock { Text = GetMet(key, _year, _month).ToString(), FontSize = 22, FontWeight = FontWeight.SemiBold, Foreground = new SolidColorBrush(Color.Parse("#e2e8f0")), Margin = new Thickness(0, 6, 0, 0) });
                border.Child = panel;
                MetricsGrid.Children.Add(border);
            }
        }
    }

    private static int GetMet(string key, int year, int month)
    {
        int b = MetricBases.GetValueOrDefault(key, 0);
        int seed = (year * 12 + month) * 7 + key.Length;
        return Math.Max(0, (int)Math.Round(b * (0.4 + 0.8 * ((seed % 17) / 17.0))));
    }

    private static bool IsFuture(int y, int m) => y > NowY || (y == NowY && m > NowM);

    private void YearLeft_Click(object? sender, RoutedEventArgs e)
    {
        if (_year > MinY) { _year--; UpdateYearMonth(); UpdateMetrics(); }
    }

    private void YearRight_Click(object? sender, RoutedEventArgs e)
    {
        if (_year < NowY) { _year++; if (IsFuture(_year, _month)) _month = NowM; UpdateYearMonth(); UpdateMetrics(); }
    }
}