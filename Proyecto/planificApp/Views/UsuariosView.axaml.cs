using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using planificApp.ViewModels;

namespace planificApp.Views;

public partial class UsuariosView : UserControl
{
    private static readonly string[] Regions = { "Todas", "Metropolitana", "Valparaíso", "Bío-Bío", "La Araucanía" };
    private static readonly Dictionary<string, string[]> Comunas = new()
    {
        ["Todas"] = new[] { "Todas" },
        ["Metropolitana"] = new[] { "Todas", "Santiago", "Providencia", "Ñuñoa", "Maipú" },
        ["Valparaíso"] = new[] { "Todas", "Valparaíso", "Viña del Mar" },
        ["Bío-Bío"] = new[] { "Todas", "Concepción" },
        ["La Araucanía"] = new[] { "Todas", "Temuco" }
    };

    private record User(string Id, string Name, string Region, string Comuna, bool Active, int Meses, string Reg);

    private static readonly User[] AllUsers = {
        new("USR001", "Carlos López", "Metropolitana", "Santiago", true, 14, "12 mar 2025"),
        new("USR002", "María González", "Metropolitana", "Providencia", true, 11, "01 jun 2025"),
        new("USR003", "Felipe Rojas", "Valparaíso", "Viña del Mar", false, 3, "15 ene 2026"),
        new("USR004", "Ana Martínez", "Bío-Bío", "Concepción", true, 8, "20 sep 2025"),
        new("USR005", "Diego Pérez", "Metropolitana", "Ñuñoa", true, 6, "10 nov 2025"),
        new("USR006", "Valentina Silva", "La Araucanía", "Temuco", false, 2, "02 mar 2026"),
        new("USR007", "Tomás Herrera", "Metropolitana", "Maipú", true, 9, "05 ago 2025"),
        new("USR008", "Camila Rivera", "Metropolitana", "Santiago", true, 12, "18 abr 2025"),
        new("USR009", "Sebastián Morán", "Valparaíso", "Valparaíso", true, 7, "22 jul 2025"),
        new("USR010", "Francisca Lagos", "Metropolitana", "Providencia", false, 4, "10 oct 2025"),
        new("USR011", "Nicolás Bravo", "Bío-Bío", "Concepción", true, 15, "03 feb 2025"),
        new("USR012", "Isidora Muñoz", "Metropolitana", "Ñuñoa", true, 10, "25 may 2025"),
        new("USR013", "Matías Fuentes", "La Araucanía", "Temuco", true, 5, "14 dic 2025"),
        new("USR014", "Sofía Vargas", "Metropolitana", "Maipú", false, 1, "28 feb 2026"),
        new("USR015", "Joaquín Araya", "Valparaíso", "Viña del Mar", true, 18, "07 ene 2025"),
        new("USR016", "Fernanda Díaz", "Metropolitana", "Santiago", true, 3, "19 ene 2026"),
        new("USR017", "Cristóbal Soto", "Bío-Bío", "Concepción", true, 13, "11 mar 2025"),
        new("USR018", "Antonella Reyes", "Metropolitana", "Providencia", true, 16, "30 ene 2025"),
        new("USR019", "Vicente Toro", "La Araucanía", "Temuco", false, 6, "05 sep 2025"),
        new("USR020", "Javiera Castillo", "Metropolitana", "Ñuñoa", true, 11, "17 abr 2025"),
        new("USR021", "Gabriel Riquelme", "Valparaíso", "Valparaíso", true, 8, "23 ago 2025"),
        new("USR022", "Mariana Espinoza", "Metropolitana", "Maipú", false, 2, "11 feb 2026"),
        new("USR023", "Álvaro Contreras", "Bío-Bío", "Concepción", true, 20, "01 ene 2025"),
        new("USR024", "Carolina Figueroa", "Metropolitana", "Santiago", true, 9, "14 jul 2025"),
        new("USR025", "Ignacio Padilla", "La Araucanía", "Temuco", true, 4, "22 nov 2025"),
    };

    private string _selectedRegion = "Todas";
    private string _selectedComuna = "Todas";
    private string _searchText = "";
    private int _currentPage = 0;
    private const int PageSize = 10;

    public UsuariosView()
    {
        InitializeComponent();
        BuildRegionPills();
        BuildComunaPills();
        SearchBox.TextChanged += SearchBox_TextChanged;
        RenderTable();
    }

    private void SearchBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        _searchText = SearchBox.Text ?? "";
        _currentPage = 0;
        RenderTable();
    }

    private IEnumerable<User> GetFilteredUsers()
    {
        return AllUsers.Where(u =>
        {
            bool regionMatch = _selectedRegion == "Todas" || u.Region == _selectedRegion;
            bool comunaMatch = _selectedComuna == "Todas" || u.Comuna == _selectedComuna;
            bool searchMatch = string.IsNullOrEmpty(_searchText) ||
                u.Name.Contains(_searchText, System.StringComparison.OrdinalIgnoreCase) ||
                u.Id.Contains(_searchText, System.StringComparison.OrdinalIgnoreCase);
            return regionMatch && comunaMatch && searchMatch;
        });
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
                _currentPage = 0;
                BuildComunaPills();
                RenderTable();
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
                _currentPage = 0;
                RenderTable();
            };
            ComunaPills.Children.Add(btn);
        }
    }

    private void RenderTable()
    {
        var filtered = GetFilteredUsers().ToList();
        int totalPages = Math.Max(1, (int)Math.Ceiling(filtered.Count / (double)PageSize));
        if (_currentPage >= totalPages) _currentPage = totalPages - 1;

        var pageUsers = filtered.Skip(_currentPage * PageSize).Take(PageSize);

        TableRows.Children.Clear();

        if (!filtered.Any())
        {
            TableRows.Children.Add(new TextBlock { Text = "Sin resultados", FontSize = 14, Foreground = new SolidColorBrush(Color.Parse("#555")), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, Margin = new Thickness(0, 24, 0, 24) });
        }
        else
        {
            foreach (var u in pageUsers)
            {
                var row = new Border
                {
                    Classes = { "user-row" },
                    BorderBrush = new SolidColorBrush(Color.Parse("#141414")),
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Padding = new Thickness(14, 10),
                    Cursor = new Cursor(StandardCursorType.Hand),
                };
                var grid = new Grid { ColumnDefinitions = ColumnDefinitions.Parse("80,2*,1*,1*,1*,1*"), IsHitTestVisible = false };

                grid.Children.Add(new TextBlock { Text = u.Id, FontSize = 12, Foreground = new SolidColorBrush(Color.Parse("#555")), FontFamily = new FontFamily("Consolas") });
                grid.Children.Add(new TextBlock { Text = u.Name, FontSize = 14, FontWeight = FontWeight.Medium, Foreground = new SolidColorBrush(Color.Parse("#ccc")), [Grid.ColumnProperty] = 1 });
                grid.Children.Add(new TextBlock { Text = u.Region, FontSize = 13, Foreground = new SolidColorBrush(Color.Parse("#aaa")), [Grid.ColumnProperty] = 2 });
                grid.Children.Add(new TextBlock { Text = u.Comuna, FontSize = 13, Foreground = new SolidColorBrush(Color.Parse("#aaa")), [Grid.ColumnProperty] = 3 });

                var badge = new Border
                {
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(8, 3),
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    [Grid.ColumnProperty] = 4,
                };
                if (u.Active)
                {
                    badge.Background = new SolidColorBrush(Color.Parse("#0f2a1a"));
                    badge.BorderBrush = new SolidColorBrush(Color.Parse("#4ade8033"));
                    badge.BorderThickness = new Thickness(1);
                    badge.Child = new TextBlock { Text = "Activo", FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#4ade80")), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
                }
                else
                {
                    badge.Background = new SolidColorBrush(Color.Parse("#1a1a1a"));
                    badge.BorderBrush = new SolidColorBrush(Color.Parse("#2a2a2a"));
                    badge.BorderThickness = new Thickness(1);
                    badge.Child = new TextBlock { Text = "Inactivo", FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#555")), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
                }
                grid.Children.Add(badge);

                grid.Children.Add(new TextBlock { Text = u.Meses.ToString(), FontSize = 14, Foreground = new SolidColorBrush(Color.Parse("#aaa")), [Grid.ColumnProperty] = 5 });

                row.Child = grid;
                row.PointerPressed += (s, e) =>
                {
                    var topLevel = TopLevel.GetTopLevel(this);
                    if (topLevel?.DataContext is MainViewModel vm)
                        vm.GoToAdminUsuarioDetalle();
                };
                TableRows.Children.Add(row);
            }
        }

        PaginationPills.Children.Clear();
        for (int i = 0; i < totalPages; i++)
        {
            var page = i;
            var btn = new Button
            {
                Content = new TextBlock { Text = (i + 1).ToString(), FontSize = 12, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center },
                Width = 32, Height = 32,
                Classes = { "pill-rounded", "admin" },
            };
            if (i == _currentPage) btn.Classes.Add("active");
            btn.Click += (s, e) => { _currentPage = page; RenderTable(); };
            PaginationPills.Children.Add(btn);
        }
    }
}