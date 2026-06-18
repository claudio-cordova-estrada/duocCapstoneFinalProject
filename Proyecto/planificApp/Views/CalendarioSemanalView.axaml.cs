using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using PlanificApp.Models;
using PlanificApp.Models.Enums;
using planificApp.ViewModels;

namespace planificApp.Views;

public partial class CalendarioSemanalView : UserControl
{
    private CalendarioSemanalViewModel? _viewModel;
    private readonly Panel[] _dayPanels = new Panel[7];
    private readonly TextBlock[] _dayHeaderNames = new TextBlock[7];
    private readonly TextBlock[] _dayHeaderNums = new TextBlock[7];
    private readonly Border[] _dayHeaders = new Border[7];
    private static readonly SolidColorBrush HourLineBrush = new(Color.Parse("#141414"));
    private static readonly SolidColorBrush AccentLightBrush = new(Color.Parse("#241f45"));
    private static readonly SolidColorBrush TrasladoBackgroundBrush = new(Color.Parse("#210101"));
    private static readonly SolidColorBrush TrasladoBorderBrush = new(Color.Parse("#ef4444"));
    private const double PixelsPerHour = 64.0;
    private const double SnapMinutes = 15;
    private const double SnapPixels = PixelsPerHour * SnapMinutes / 60.0;
    private const double MinBlockHeight = PixelsPerHour * 15.0 / 60.0;
    private const double ResizeHandleSize = 6;

    private enum DragMode { None, Move, ResizeTop, ResizeBottom, MoveSubBloque, ResizeTopSubBloque, ResizeBottomSubBloque }

    private DragMode _dragMode = DragMode.None;
    private Border? _draggedBorder;
    private BloqueCalendario? _draggedBloque;
    private DiaCalendario? _draggedDia;
    private int _draggedDayIndex = -1;
    private double _dragStartY;
    private double _dragOriginalTop;
    private double _dragOriginalHeight;
    private TimeSpan _dragOriginalHoraInicio;
    private TimeSpan _dragOriginalHoraFin;

    // Sub-bloque drag state
    private SubBloqueTarea? _draggedSubBloque;
    private BloqueCalendario? _draggedSubParentBloque;
    private Border? _draggedSubBorder;
    private Panel? _draggedSubContentPanel;
    private DiaCalendario? _draggedSubDia;
    private int _draggedSubDayIndex = -1;
    private double _draggedSubOriginalRelTop;
    private double _draggedSubOriginalHeight;
    private TimeSpan _draggedSubOriginalInicio;
    private TimeSpan _draggedSubOriginalFin;

    private bool _panelDragStarted;
    private Point _panelDragStartPoint;
    private PointerPressedEventArgs? _panelDragInitiatingEvent;
    private object? _panelDragItem;
    private string? _panelDragType;

    public CalendarioSemanalView()
    {
        InitializeComponent();

        for (int i = 0; i < 7; i++)
        {
            _dayPanels[i] = this.FindControl<Panel>($"Day{i}")!;
            _dayHeaderNames[i] = this.FindControl<TextBlock>($"DayHeader{i}_Name")!;
            _dayHeaderNums[i] = this.FindControl<TextBlock>($"DayHeader{i}_Num")!;
            _dayHeaders[i] = this.FindControl<Border>($"DayHeader{i}")!;
        }

        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;

        AddHandler(PointerPressedEvent, OnPanelPointerPressed, RoutingStrategies.Tunnel);
        AddHandler(PointerMovedEvent, OnPanelPointerMoved, RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, OnPanelPointerReleased, RoutingStrategies.Tunnel);

        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        SetupDropHandlers();

        if (_viewModel != null)
            await _viewModel.InitializeAsync();

        ScrollToCurrentHour();
    }

    private void ScrollToCurrentHour()
    {
        var scrollViewer = this.FindControl<ScrollViewer>("CalendarScrollViewer");
        if (scrollViewer != null)
        {
            var currentHour = DateTime.Now.Hour;
            var offset = Math.Max(0, currentHour * PixelsPerHour - 200);
            scrollViewer.Offset = new Vector(0, offset);
        }
    }

    private void SetupDropHandlers()
    {
        for (int i = 0; i < 7; i++)
        {
            if (_dayPanels[i] != null)
            {
                DragDrop.SetAllowDrop(_dayPanels[i], true);
                _dayPanels[i].AddHandler(DragDrop.DragOverEvent, OnDayDragOver, RoutingStrategies.Bubble);
                _dayPanels[i].AddHandler(DragDrop.DropEvent, OnDayDrop, RoutingStrategies.Bubble);
            }
        }
    }

private void OnDayDragOver(object? sender, DragEventArgs e)
    {
        if (e.DataTransfer.Formats.Contains(DataFormat.Text))
        {
            var text = e.DataTransfer.TryGetText();
            if (text != null && text.StartsWith("CalendarDrop:"))
                e.DragEffects = DragDropEffects.Copy;
            else
                e.DragEffects = DragDropEffects.None;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private async void OnDayDrop(object? sender, DragEventArgs e)
    {
        if (_viewModel == null) return;

        var text = e.DataTransfer.TryGetText();
        if (text == null || !text.StartsWith("CalendarDrop:")) return;

        var parts = text.Split(':');
        if (parts.Length < 4) return;

        var dropType = parts[1];
        var dropId = parts[2];

        var panel = sender as Panel;
        if (panel == null) return;

        int dayIndex = Array.IndexOf(_dayPanels, panel);
        if (dayIndex < 0 || dayIndex >= _viewModel.Dias.Count) return;

        var dia = _viewModel.Dias[dayIndex];
        var dropY = e.GetPosition(panel).Y;
        var horaInicio = PixelsToTime(dropY);
        horaInicio = TimeSpan.FromMinutes(Math.Round(horaInicio.TotalMinutes / 15) * 15);

        var horaMin = _viewModel.HoraInicioJornada;
        var horaMax = _viewModel.HoraFinJornada;
        if (horaInicio < horaMin) horaInicio = horaMin;
        if (horaInicio >= horaMax) horaInicio = horaMax - TimeSpan.FromMinutes(15);

        try
        {
            if (dropType == "AreaInteres")
            {
                var area = _viewModel.AreasDisponibles.FirstOrDefault(a => a.IdAreaInteres == dropId);
                if (area == null) return;

                if (OverlapsWithAnyTraslado(horaInicio, TimeSpan.FromHours(1), dia))
                    return;

                await _viewModel.AgregarBloqueInteresEnDiaAsync(area, dia, horaInicio);
            }
            else if (dropType == "Tarea")
            {
                var tarea = _viewModel.TareasDisponibles.FirstOrDefault(t => t.IdTarea == dropId);
                if (tarea == null) return;

                var duracionTarea = tarea.TiempoEstimado > 0
                    ? TimeSpan.FromMinutes(tarea.TiempoEstimado)
                    : TimeSpan.FromMinutes(60);

                if (OverlapsWithAnyTraslado(horaInicio, duracionTarea, dia))
                    return;

                await _viewModel.AgregarTareaEnDiaAsync(tarea, dia, horaInicio);
            }
        }
        catch { }
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_viewModel != null)
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

        _viewModel = DataContext as CalendarioSemanalViewModel;

        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            Dispatcher.UIThread.Post(async () =>
            {
                UpdateDayHeaders();
                RenderAllDays();
                try { await _viewModel.InitializeAsync(); } catch { }
            });
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CalendarioSemanalViewModel.Dias))
        {
            Dispatcher.UIThread.Post(() =>
            {
                UpdateDayHeaders();
                RenderAllDays();
            });
        }
    }

    private void UpdateDayHeaders()
    {
        if (_viewModel == null) return;

        for (int i = 0; i < 7; i++)
        {
            if (i < _viewModel.Dias.Count)
            {
                var dia = _viewModel.Dias[i];
                _dayHeaderNames[i].Text = dia.NombreDia;
                _dayHeaderNums[i].Text = dia.NumeroDia.ToString();

                if (dia.EsHoy)
                {
                    _dayHeaderNames[i].Foreground = new SolidColorBrush(Color.Parse("#a78bfa"));
                    _dayHeaderNums[i].Foreground = new SolidColorBrush(Color.Parse("#a78bfa"));
                    _dayHeaderNums[i].FontWeight = Avalonia.Media.FontWeight.Medium;
                    if (i < _dayHeaders.Length && _dayHeaders[i] != null)
                        _dayHeaders[i].Background = AccentLightBrush;
                }
                else
                {
                    _dayHeaderNames[i].Foreground = new SolidColorBrush(Color.Parse("#555555"));
                    _dayHeaderNums[i].Foreground = new SolidColorBrush(Color.Parse("#555555"));
                    _dayHeaderNums[i].FontWeight = Avalonia.Media.FontWeight.Normal;
                    if (i < _dayHeaders.Length && _dayHeaders[i] != null)
                        _dayHeaders[i].Background = Brushes.Transparent;
                }
            }
            else
            {
                _dayHeaderNames[i].Text = "";
                _dayHeaderNums[i].Text = "";
            }
        }
    }

    private void RenderAllDays()
    {
        if (_viewModel == null) return;

        var horaInicio = _viewModel.HoraInicioJornada;
        var horaFin = _viewModel.HoraFinJornada;

        for (int i = 0; i < 7; i++)
        {
            if (i < _dayPanels.Length && _dayPanels[i] != null)
            {
                _dayPanels[i].Children.Clear();

                if (i < _viewModel.Dias.Count && _viewModel.Dias[i].EsHoy)
                    _dayPanels[i].Background = AccentLightBrush;
                else
                    _dayPanels[i].Background = Brushes.Transparent;

                RenderHourLines(_dayPanels[i]);
                RenderOffHoursOverlay(_dayPanels[i], horaInicio, horaFin);

                if (i < _viewModel.Dias.Count)
                    RenderDayBlocks(_dayPanels[i], _viewModel.Dias[i], i);
            }
        }
    }

    private static readonly SolidColorBrush OffHoursBrush = new(Color.Parse("#0a0a12")) { Opacity = 0.55 };

    private static void RenderOffHoursOverlay(Panel panel, TimeSpan horaInicio, TimeSpan horaFin)
    {
        if (horaInicio > TimeSpan.Zero)
        {
            var topPx = (double)horaInicio.TotalMinutes * (PixelsPerHour / 60.0);
            var overlay = new Border
            {
                Height = topPx,
                VerticalAlignment = VerticalAlignment.Top,
                Background = OffHoursBrush,
                IsHitTestVisible = false
            };
            panel.Children.Add(overlay);
        }

        if (horaFin < TimeSpan.FromHours(24))
        {
            var topPx = (double)horaFin.TotalMinutes * (PixelsPerHour / 60.0);
            var heightPx = 24.0 * PixelsPerHour - topPx;
            var overlay = new Border
            {
                Margin = new Thickness(0, topPx, 0, 0),
                Height = heightPx,
                VerticalAlignment = VerticalAlignment.Top,
                Background = OffHoursBrush,
                IsHitTestVisible = false
            };
            panel.Children.Add(overlay);
        }
    }

    private static void RenderHourLines(Panel panel)
    {
        for (int hour = 1; hour < 24; hour++)
        {
            var line = new Border
            {
                Margin = new Thickness(0, hour * 64, 0, 0),
                Height = 1,
                Background = HourLineBrush,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top
            };
            panel.Children.Add(line);
        }
    }

    private void RenderDayBlocks(Panel panel, DiaCalendario dia, int dayIndex)
    {
        CalculateOverlapColumns(dia.Bloques.ToList());

        foreach (var bloque in dia.Bloques)
        {
            var border = CreateBloqueVisual(bloque, dia, dayIndex, panel);
            panel.Children.Add(border);
        }
    }

    private Border CreateBloqueVisual(BloqueCalendario bloque, DiaCalendario dia, int dayIndex, Panel panel)
    {
        double topPx = bloque.TopPx;
        double heightPx = bloque.HeightPx;
        if (heightPx < MinBlockHeight) heightPx = MinBlockHeight;

        var panelWidth = panel.Bounds.Width > 0 ? panel.Bounds.Width : 190;
        var columnCount = bloque.ColumnCount;
        var columnIndex = bloque.ColumnIndex;
        var blockWidth = (panelWidth - 4) / columnCount;
        var leftOffset = 2 + columnIndex * blockWidth;

        if (columnCount > 1) blockWidth -= 2;

        var border = new Border
        {
            Margin = new Thickness(leftOffset, topPx, 2, 0),
            Width = blockWidth,
            Height = heightPx,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Left,
            CornerRadius = new CornerRadius(5),
            ClipToBounds = true,
            Tag = (bloque, dia, dayIndex),
            Opacity = bloque.Completada ? 0.45 : 1.0,
            Cursor = bloque.Tipo != TipoBloqueCalendario.SeccionTraslado
                ? new Cursor(StandardCursorType.SizeAll)
                : new Cursor(StandardCursorType.Arrow)
        };

        Grid contentGrid;

        switch (bloque.Tipo)
        {
            case TipoBloqueCalendario.BloqueInteres:
                var areaColor = !string.IsNullOrEmpty(bloque.ColorHex) ? bloque.ColorHex : "#a78bfa";
                border.Background = CreateBrush(areaColor, 0.25);
                border.BorderBrush = CreateBrush(areaColor, 0.27);
                border.BorderThickness = new Thickness(1, 0, 0, 2);

                contentGrid = new Grid();
                contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(ResizeHandleSize) });
                contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(ResizeHandleSize) });

                var topHandle = new Border
                {
                    Height = ResizeHandleSize,
                    Background = Brushes.Transparent,
                    Cursor = new Cursor(StandardCursorType.SizeNorthSouth)
                };
                topHandle.PointerPressed += (s, e) => OnResizeHandlePressed(e, border, bloque, dia, dayIndex, DragMode.ResizeTop);
                Grid.SetRow(topHandle, 0);
                contentGrid.Children.Add(topHandle);

                var bottomHandle = new Border
                {
                    Height = ResizeHandleSize,
                    Background = Brushes.Transparent,
                    Cursor = new Cursor(StandardCursorType.SizeNorthSouth)
                };
                bottomHandle.PointerPressed += (s, e) => OnResizeHandlePressed(e, border, bloque, dia, dayIndex, DragMode.ResizeBottom);
                Grid.SetRow(bottomHandle, 2);
                contentGrid.Children.Add(bottomHandle);

                var contentPanel = new Panel { ClipToBounds = false };
                Grid.SetRow(contentPanel, 1);
                contentGrid.Children.Add(contentPanel);

                if (bloque.TareasInternas != null && bloque.TareasInternas.Count > 0)
                {
                    foreach (var sub in bloque.TareasInternas)
                    {
                        var subBorder = CreateSubBloqueVisual(sub, bloque, dia, dayIndex, contentPanel);
                        contentPanel.Children.Add(subBorder);
                    }
                }
                else
                {
                    contentPanel.Children.Add(new TextBlock
                    {
                        Text = bloque.NombreArea ?? "Área",
                        FontSize = 10,
                        Foreground = CreateBrush(areaColor, 0.7),
                        VerticalAlignment = VerticalAlignment.Top,
                        Margin = new Thickness(4, 2)
                    });
                }

                border.ClipToBounds = false;
                border.Child = contentGrid;
                break;

            case TipoBloqueCalendario.Tarea:
                var taskColor = !string.IsNullOrEmpty(bloque.ColorHex) ? bloque.ColorHex : "#666666";
                border.Background = CreateBrush(taskColor, 0.55);
                border.BorderBrush = CreateBrush(taskColor, 0.7);
                border.BorderThickness = new Thickness(1, 0, 0, 2);

                contentGrid = new Grid();
                contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(ResizeHandleSize) });
                contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(ResizeHandleSize) });

                var topH2 = new Border
                {
                    Height = ResizeHandleSize,
                    Background = Brushes.Transparent,
                    Cursor = new Cursor(StandardCursorType.SizeNorthSouth)
                };
                topH2.PointerPressed += (s, e) => OnResizeHandlePressed(e, border, bloque, dia, dayIndex, DragMode.ResizeTop);
                Grid.SetRow(topH2, 0);
                contentGrid.Children.Add(topH2);

                var bottomH2 = new Border
                {
                    Height = ResizeHandleSize,
                    Background = Brushes.Transparent,
                    Cursor = new Cursor(StandardCursorType.SizeNorthSouth)
                };
                bottomH2.PointerPressed += (s, e) => OnResizeHandlePressed(e, border, bloque, dia, dayIndex, DragMode.ResizeBottom);
                Grid.SetRow(bottomH2, 2);
                contentGrid.Children.Add(bottomH2);

                var taskLabel = new TextBlock
                {
                    Text = bloque.NombreTarea ?? "Tarea",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.Parse("#ffffff")),
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Margin = new Thickness(4, 0),
                    VerticalAlignment = VerticalAlignment.Top
                };
                if (bloque.Completada)
                {
                    taskLabel.TextDecorations = TextDecorations.Strikethrough;
                    taskLabel.Opacity = 0.7;
                }
                Grid.SetRow(taskLabel, 1);
                contentGrid.Children.Add(taskLabel);
                border.Child = contentGrid;
                break;

case TipoBloqueCalendario.SeccionTraslado:
                border.Background = TrasladoBackgroundBrush;
                border.BorderBrush = TrasladoBorderBrush;
                border.BorderThickness = new Thickness(2, 0, 0, 0);
                border.CornerRadius = new CornerRadius(3);
                border.Cursor = new Cursor(StandardCursorType.Arrow);

                if (heightPx < 30)
                {
                    topPx = topPx + heightPx - 30;
                    if (topPx < 0) topPx = 0;
                    heightPx = 30;
                    border.Margin = new Thickness(leftOffset, topPx, 2, 0);
                    border.Height = heightPx;
                }
                border.Margin = new Thickness(leftOffset, topPx, 2, 0);
                border.Height = heightPx;

                var durationText = bloque.DuracionMinutos.HasValue
                    ? $"{bloque.DuracionMinutos} min"
                    : "~15 min";
                if (bloque.EsEstimado) durationText += " ~";
                var transportLabel = bloque.MetodoTransporte switch
                {
                    MetodoTransporte.Caminar => "A pie",
                    MetodoTransporte.Auto => "Auto",
                    MetodoTransporte.Bus => "Bus",
                    _ => ""
                };

                var trasladoStack = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Spacing = 1
                };

                trasladoStack.Children.Add(new TextBlock
                {
                    Text = durationText,
                    FontSize = 9,
                    FontWeight = FontWeight.Bold,
                    Foreground = TrasladoBorderBrush,
                    HorizontalAlignment = HorizontalAlignment.Center
                });

                if (!string.IsNullOrEmpty(transportLabel))
                {
                    trasladoStack.Children.Add(new TextBlock
                    {
                        Text = transportLabel,
                        FontSize = 8,
                        Foreground = TrasladoBorderBrush,
                        HorizontalAlignment = HorizontalAlignment.Center
                    });
                }

                border.Child = trasladoStack;
                break;
        }

        if (bloque.Tipo != TipoBloqueCalendario.SeccionTraslado)
        {
            border.PointerPressed += (s, e) => OnBloquePointerPressed(e, border, bloque, dia, dayIndex);
        }

        return border;
    }

    private Border CreateSubBloqueVisual(SubBloqueTarea sub, BloqueCalendario parentBloque, DiaCalendario dia, int dayIndex, Panel contentPanel)
    {
        double relativeTop = (sub.HoraInicio - parentBloque.HoraInicio).TotalMinutes * (PixelsPerHour / 60.0);
        double subHeight = Math.Max(MinBlockHeight, (sub.HoraFin - sub.HoraInicio).TotalMinutes * (PixelsPerHour / 60.0));
        var areaColor = parentBloque.ColorHex ?? "#a78bfa";

        var subBorder = new Border
        {
            Margin = new Thickness(1, relativeTop, 1, 0),
            Height = subHeight,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Background = CreateBrush(areaColor, 0.6),
            BorderBrush = CreateBrush(areaColor, 1.0),
            BorderThickness = new Thickness(1, 1, 1, 2),
            CornerRadius = new CornerRadius(3),
            Cursor = new Cursor(StandardCursorType.SizeAll),
            ClipToBounds = true,
            Opacity = sub.Completada ? 0.45 : 1.0
        };

        var subGrid = new Grid();
        subGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(ResizeHandleSize) });
        subGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        subGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(ResizeHandleSize) });

        var topHandle = new Border { Height = ResizeHandleSize, Background = Brushes.Transparent, Cursor = new Cursor(StandardCursorType.SizeNorthSouth) };
        topHandle.PointerPressed += (s, e) => OnSubBloqueResizePressed(e, subBorder, sub, parentBloque, dia, dayIndex, DragMode.ResizeTopSubBloque, contentPanel);
        Grid.SetRow(topHandle, 0);
        subGrid.Children.Add(topHandle);

        var bottomHandle = new Border { Height = ResizeHandleSize, Background = Brushes.Transparent, Cursor = new Cursor(StandardCursorType.SizeNorthSouth) };
        bottomHandle.PointerPressed += (s, e) => OnSubBloqueResizePressed(e, subBorder, sub, parentBloque, dia, dayIndex, DragMode.ResizeBottomSubBloque, contentPanel);
        Grid.SetRow(bottomHandle, 2);
        subGrid.Children.Add(bottomHandle);

        var label = new TextBlock
        {
            Text = sub.Nombre,
            FontSize = 10,
            Foreground = new SolidColorBrush(Color.Parse("#ffffff")),
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(4, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        if (sub.Completada) { label.TextDecorations = TextDecorations.Strikethrough; label.Opacity = 0.55; }
        Grid.SetRow(label, 1);
        subGrid.Children.Add(label);

        subBorder.Child = subGrid;
        subBorder.PointerPressed += (s, e) => OnSubBloquePointerPressed(e, subBorder, sub, parentBloque, dia, dayIndex, contentPanel);
        return subBorder;
    }

    private void OnSubBloquePointerPressed(PointerPressedEventArgs e, Border subBorder, SubBloqueTarea sub, BloqueCalendario parentBloque, DiaCalendario dia, int dayIndex, Panel contentPanel)
    {
        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            e.Handled = true;
            ShowSubBloqueContextMenu(subBorder, sub, parentBloque, dia);
            return;
        }

        e.Handled = true;
        _dragMode = DragMode.MoveSubBloque;
        _draggedSubBloque = sub;
        _draggedSubParentBloque = parentBloque;
        _draggedSubBorder = subBorder;
        _draggedSubContentPanel = contentPanel;
        _draggedSubDia = dia;
        _draggedSubDayIndex = dayIndex;
        _dragStartY = e.GetCurrentPoint(this).Position.Y;
        _draggedSubOriginalRelTop = subBorder.Margin.Top;
        _draggedSubOriginalHeight = subBorder.Height;
        _draggedSubOriginalInicio = sub.HoraInicio;
        _draggedSubOriginalFin = sub.HoraFin;
    }

    private void OnSubBloqueResizePressed(PointerPressedEventArgs e, Border subBorder, SubBloqueTarea sub, BloqueCalendario parentBloque, DiaCalendario dia, int dayIndex, DragMode mode, Panel contentPanel)
    {
        e.Handled = true;
        _dragMode = mode;
        _draggedSubBloque = sub;
        _draggedSubParentBloque = parentBloque;
        _draggedSubBorder = subBorder;
        _draggedSubContentPanel = contentPanel;
        _draggedSubDia = dia;
        _draggedSubDayIndex = dayIndex;
        _dragStartY = e.GetCurrentPoint(this).Position.Y;
        _draggedSubOriginalRelTop = subBorder.Margin.Top;
        _draggedSubOriginalHeight = subBorder.Height;
        _draggedSubOriginalInicio = sub.HoraInicio;
        _draggedSubOriginalFin = sub.HoraFin;
    }

    private void ShowSubBloqueContextMenu(Border subBorder, SubBloqueTarea sub, BloqueCalendario parentBloque, DiaCalendario dia)
    {
        var menu = new ContextMenu();

        var extractItem = new MenuItem { Header = "Quitar del bloque" };
        extractItem.Click += async (s, args) =>
        {
            if (_viewModel != null)
            {
                await _viewModel.ExtraerSubTareaAsync(dia, parentBloque, sub.IdTarea, null);
                RenderAllDays();
            }
        };
        menu.Items.Add(extractItem);

        var completarItem = new MenuItem { Header = sub.Completada ? "Descompletar" : "Completar" };
        completarItem.Click += (s, args) => _viewModel?.CompletarTareaBloqueCommand.Execute(sub);
        menu.Items.Add(completarItem);

        var deleteItem = new MenuItem { Header = "Eliminar" };
        deleteItem.Click += async (s, args) =>
        {
            if (_viewModel != null)
            {
                await _viewModel.EliminarSubTareaAsync(dia, parentBloque.Id, sub.IdTarea);
                RenderAllDays();
            }
        };
        menu.Items.Add(deleteItem);

        menu.Open(subBorder);
    }

    private void OnBloquePointerPressed(PointerPressedEventArgs e, Border border, BloqueCalendario bloque, DiaCalendario dia, int dayIndex)
    {
        if (bloque.Tipo == TipoBloqueCalendario.SeccionTraslado) return;

        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            e.Handled = true;
            ShowBlockContextMenu(e, border, bloque, dia, dayIndex);
            return;
        }

        e.Handled = true;
        _dragMode = DragMode.Move;
        _draggedBorder = border;
        _draggedBloque = bloque;
        _draggedDia = dia;
        _draggedDayIndex = dayIndex;
        _dragStartY = e.GetCurrentPoint(this).Position.Y;
        _dragOriginalTop = bloque.TopPx;
        _dragOriginalHoraInicio = bloque.HoraInicio;
        _dragOriginalHoraFin = bloque.HoraFin;
        _dragOriginalHeight = bloque.HeightPx;

        if (_viewModel != null) _viewModel.BloqueSeleccionado = bloque;
    }

    private async void ShowBlockContextMenu(PointerPressedEventArgs e, Border border, BloqueCalendario bloque, DiaCalendario dia, int dayIndex)
    {
        var menu = new ContextMenu();

        if (bloque.Tipo == TipoBloqueCalendario.Tarea && bloque.IdTarea != null)
        {
            var completarItem = new MenuItem { Header = bloque.Completada ? "Descompletar" : "Completar" };
            completarItem.Click += async (s, args) =>
            {
                if (_viewModel != null)
                {
                    await _viewModel.ToggleCompletarTareaCommand.ExecuteAsync(bloque);
                    RenderAllDays();
                }
            };
            menu.Items.Add(completarItem);

            var unassignItem = new MenuItem { Header = "Quitar del calendario" };
            unassignItem.Click += async (s, args) =>
            {
                if (_viewModel != null)
                {
                    await _viewModel.EliminarBloqueAsync(dia, bloque.Id);
                    RenderAllDays();
                }
            };
            menu.Items.Add(unassignItem);
        }
        else if (bloque.Tipo == TipoBloqueCalendario.BloqueInteres)
        {
            var unassignItem = new MenuItem { Header = "Quitar del calendario" };
            unassignItem.Click += async (s, args) =>
            {
                if (_viewModel != null)
                {
                    await _viewModel.EliminarBloqueAsync(dia, bloque.Id);
                    RenderAllDays();
                }
            };
            menu.Items.Add(unassignItem);
        }

        var deleteItem = new MenuItem { Header = "Eliminar" };
        deleteItem.Click += async (s, args) =>
        {
            if (_viewModel != null)
            {
                await _viewModel.EliminarTareaPermanentementeAsync(dia, bloque.Id);
                RenderAllDays();
            }
        };
        menu.Items.Add(deleteItem);

        menu.Open(border);
    }

    private void OnResizeHandlePressed(PointerPressedEventArgs e, Border border, BloqueCalendario bloque, DiaCalendario dia, int dayIndex, DragMode mode)
    {
        if (bloque.Tipo == TipoBloqueCalendario.SeccionTraslado) return;

        e.Handled = true;
        _dragMode = mode;
        _draggedBorder = border;
        _draggedBloque = bloque;
        _draggedDia = dia;
        _draggedDayIndex = dayIndex;
        _dragStartY = e.GetCurrentPoint(this).Position.Y;
        _dragOriginalTop = bloque.TopPx;
        _dragOriginalHoraInicio = bloque.HoraInicio;
        _dragOriginalHoraFin = bloque.HoraFin;
        _dragOriginalHeight = bloque.HeightPx;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed && _viewModel?.BloqueSeleccionado != null)
            _viewModel.BloqueSeleccionado = null;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragMode == DragMode.None) return;

        var currentY = e.GetCurrentPoint(this).Position.Y;
        var deltaY = currentY - _dragStartY;

        if (_dragMode is DragMode.MoveSubBloque or DragMode.ResizeTopSubBloque or DragMode.ResizeBottomSubBloque)
        {
            if (_draggedSubBorder == null || _draggedSubBloque == null || _draggedSubParentBloque == null) return;

            var parentContentHeight = (_draggedSubParentBloque.HoraFin - _draggedSubParentBloque.HoraInicio).TotalMinutes * (PixelsPerHour / 60.0);

            switch (_dragMode)
            {
                case DragMode.MoveSubBloque:
                {
                    var newRelTop = SnapToGrid(_draggedSubOriginalRelTop + deltaY);
                    newRelTop = Math.Max(0, Math.Min(newRelTop, parentContentHeight - _draggedSubOriginalHeight));
                    _draggedSubBorder.Margin = new Thickness(1, newRelTop, 1, 0);

                    var relMinutes = newRelTop * 60.0 / PixelsPerHour;
                    var newInicio = _draggedSubParentBloque.HoraInicio + TimeSpan.FromMinutes(Math.Round(relMinutes / SnapMinutes) * SnapMinutes);
                    var duration = _draggedSubOriginalFin - _draggedSubOriginalInicio;
                    _draggedSubBloque.HoraInicio = newInicio;
                    _draggedSubBloque.HoraFin = newInicio + duration;
                    break;
                }
                case DragMode.ResizeTopSubBloque:
                {
                    var newRelTop = SnapToGrid(_draggedSubOriginalRelTop + deltaY);
                    var maxRelTop = _draggedSubOriginalRelTop + _draggedSubOriginalHeight - MinBlockHeight;
                    newRelTop = Math.Max(0, Math.Min(newRelTop, maxRelTop));
                    var newHeight = (_draggedSubOriginalRelTop + _draggedSubOriginalHeight) - newRelTop;

                    _draggedSubBorder.Margin = new Thickness(1, newRelTop, 1, 0);
                    _draggedSubBorder.Height = newHeight;

                    var relMinutes = newRelTop * 60.0 / PixelsPerHour;
                    _draggedSubBloque.HoraInicio = _draggedSubParentBloque.HoraInicio + TimeSpan.FromMinutes(Math.Round(relMinutes / SnapMinutes) * SnapMinutes);
                    break;
                }
                case DragMode.ResizeBottomSubBloque:
                {
                    var newHeight = Math.Max(MinBlockHeight, SnapToGrid(_draggedSubOriginalHeight + deltaY));
                    newHeight = Math.Min(newHeight, parentContentHeight - _draggedSubOriginalRelTop);
                    _draggedSubBorder.Height = newHeight;

                    var relBottom = _draggedSubOriginalRelTop + newHeight;
                    var relMinutes = relBottom * 60.0 / PixelsPerHour;
                    _draggedSubBloque.HoraFin = _draggedSubParentBloque.HoraInicio + TimeSpan.FromMinutes(Math.Round(relMinutes / SnapMinutes) * SnapMinutes);
                    break;
                }
            }
            return;
        }

        if (_draggedBorder == null || _draggedBloque == null || _draggedDia == null) return;

        switch (_dragMode)
        {
            case DragMode.Move:
                {
                    var newTop = _dragOriginalTop + deltaY;
                    newTop = Math.Max(0, SnapToGrid(newTop));
                    var newHoraInicio = PixelsToTime(newTop);
                    var duracion = _dragOriginalHoraFin - _dragOriginalHoraInicio;
                    var newHoraFin = newHoraInicio + duracion;

                    if (_viewModel != null)
                    {
                        if (newHoraInicio < _viewModel.HoraInicioJornada)
                            newHoraInicio = _viewModel.HoraInicioJornada;
                        if (newHoraFin > _viewModel.HoraFinJornada)
                            return;
                    }
                    else if (newHoraFin > TimeSpan.FromHours(24))
                    {
                        return;
                    }

                    _draggedBloque.HoraInicio = newHoraInicio;
                    _draggedBloque.HoraFin = newHoraInicio + duracion;
                    _draggedBorder.Margin = new Thickness(2, newTop, 2, 0);
                    break;
                }
case DragMode.ResizeTop:
                {
                    var newTop = _dragOriginalTop + deltaY;
                    newTop = Math.Max(0, SnapToGrid(newTop));
                    var maxTop = _dragOriginalTop + _dragOriginalHeight - MinBlockHeight;
                    newTop = Math.Min(newTop, maxTop);

                    var newHoraInicio = PixelsToTime(newTop);
                    if (newHoraInicio >= _dragOriginalHoraFin - TimeSpan.FromMinutes(15)) return;

                    if (_viewModel != null && newHoraInicio < _viewModel.HoraInicioJornada)
                    {
                        newHoraInicio = _viewModel.HoraInicioJornada;
                        newTop = newHoraInicio.TotalMinutes * (PixelsPerHour / 60.0);
                    }

                    _draggedBloque.HoraInicio = newHoraInicio;
                    var newHeight = (_dragOriginalTop + _dragOriginalHeight) - newTop;

                    _draggedBorder.Margin = new Thickness(2, newTop, 2, 0);
                    _draggedBorder.Height = newHeight;

                    if (_draggedBloque.Tipo == TipoBloqueCalendario.BloqueInteres && _draggedBloque.TareasInternas != null)
                    {
                        foreach (var sub in _draggedBloque.TareasInternas)
                        {
                            if (sub.HoraInicio < newHoraInicio) sub.HoraInicio = newHoraInicio;
                        }
                    }
                    break;
                }
                case DragMode.ResizeBottom:
                {
                    var newHeight = _dragOriginalHeight + deltaY;
                    newHeight = Math.Max(MinBlockHeight, SnapToGrid(newHeight));
                    var newHoraFin = PixelsToTime(_dragOriginalTop + newHeight);

                    if (_viewModel != null && newHoraFin > _viewModel.HoraFinJornada)
                    {
                        newHoraFin = _viewModel.HoraFinJornada;
                        newHeight = (newHoraFin.TotalMinutes - _dragOriginalHoraInicio.TotalMinutes) * (PixelsPerHour / 60.0);
                        newHeight = Math.Max(MinBlockHeight, newHeight);
                    }
                    else if (newHoraFin > TimeSpan.FromHours(24))
                    {
                        return;
                    }

                    if (newHoraFin - _draggedBloque.HoraInicio < TimeSpan.FromMinutes(15)) return;

                    _draggedBloque.HoraFin = newHoraFin;
                    _draggedBorder.Height = newHeight;

                    if (_draggedBloque.Tipo == TipoBloqueCalendario.BloqueInteres && _draggedBloque.TareasInternas != null)
                    {
                        foreach (var sub in _draggedBloque.TareasInternas)
                        {
                            if (sub.HoraFin > newHoraFin) sub.HoraFin = newHoraFin;
                            if (sub.HoraFin - sub.HoraInicio < TimeSpan.FromMinutes(15))
                                sub.HoraFin = sub.HoraInicio + TimeSpan.FromMinutes(15);
                        }
                    }
                    break;
                }
        }
    }

    private async void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_dragMode is DragMode.MoveSubBloque or DragMode.ResizeTopSubBloque or DragMode.ResizeBottomSubBloque)
        {
            var wasSubMove = _dragMode == DragMode.MoveSubBloque;
            _dragMode = DragMode.None;

            if (_viewModel != null && _draggedSubBloque != null && _draggedSubParentBloque != null && _draggedSubDia != null)
            {
                try
                {
                    if (wasSubMove)
                    {
                        var pointerPos = e.GetPosition(this);
                        var onPanel = pointerPos.X > Bounds.Width - 210;
                        var posOnDay = _draggedSubDayIndex >= 0 ? e.GetPosition(_dayPanels[_draggedSubDayIndex]) : new Point(0, 0);
                        var aboveParent = posOnDay.Y < _draggedSubParentBloque.TopPx;
                        var belowParent = posOnDay.Y > _draggedSubParentBloque.TopPx + _draggedSubParentBloque.HeightPx;

                        if (onPanel)
                        {
                            await _viewModel.ExtraerSubTareaAsync(_draggedSubDia, _draggedSubParentBloque, _draggedSubBloque.IdTarea, null);
                        }
                        else if (aboveParent || belowParent)
                        {
                            var newHoraInicio = PixelsToTime(posOnDay.Y);
                            newHoraInicio = TimeSpan.FromMinutes(Math.Round(newHoraInicio.TotalMinutes / SnapMinutes) * SnapMinutes);
                            await _viewModel.ExtraerSubTareaAsync(_draggedSubDia, _draggedSubParentBloque, _draggedSubBloque.IdTarea, newHoraInicio);
                        }
                        else
                        {
                            await _viewModel.MoverSubTareaAsync(_draggedSubDia, _draggedSubParentBloque.Id, _draggedSubBloque.IdTarea, _draggedSubBloque.HoraInicio, _draggedSubBloque.HoraFin);
                        }
                    }
                    else
                    {
                        await _viewModel.MoverSubTareaAsync(_draggedSubDia, _draggedSubParentBloque.Id, _draggedSubBloque.IdTarea, _draggedSubBloque.HoraInicio, _draggedSubBloque.HoraFin);
                    }
                }
                catch { }
            }

            _draggedSubBloque = null;
            _draggedSubParentBloque = null;
            _draggedSubBorder = null;
            _draggedSubContentPanel = null;
            _draggedSubDia = null;
            _draggedSubDayIndex = -1;
            RenderAllDays();
            return;
        }

        if (_dragMode == DragMode.None || _draggedBloque == null || _draggedDia == null || _viewModel == null)
        {
            _dragMode = DragMode.None;
            return;
        }

        var wasMoved = _dragMode == DragMode.Move;
        var wasResized = _dragMode == DragMode.ResizeTop || _dragMode == DragMode.ResizeBottom;

        _dragMode = DragMode.None;

        try
        {
            if (wasMoved)
            {
                var pointerPos = e.GetPosition(this);
                var viewWidth = this.Bounds.Width;

                if (pointerPos.X > viewWidth - 210)
                {
                    await _viewModel.EliminarBloqueAsync(_draggedDia, _draggedBloque.Id);
                }
                else
                {
                    int targetDayIdx = GetTargetDayIndex(e);
                    if (targetDayIdx >= 0 && targetDayIdx < _viewModel.Dias.Count)
                    {
                        var targetDia = _viewModel.Dias[targetDayIdx];
                        var dropY = e.GetPosition(_dayPanels[targetDayIdx]).Y;
                        var nuevaHora = PixelsToTime(dropY);
                        nuevaHora = TimeSpan.FromMinutes(Math.Round(nuevaHora.TotalMinutes / 15) * 15);

                        var duracion = _draggedBloque.HoraFin - _draggedBloque.HoraInicio;

                        if (OverlapsWithForeignTraslado(nuevaHora, duracion, targetDia, _draggedBloque))
                        {
                            _draggedBloque.HoraInicio = _dragOriginalHoraInicio;
                            _draggedBloque.HoraFin = _dragOriginalHoraFin;
                        }
                        else if (targetDayIdx != _draggedDayIndex)
                        {
                            await _viewModel.MoverBloqueADiaAsync(_draggedDia, targetDia, _draggedBloque.Id, nuevaHora);
                            if (_draggedBloque.Tipo == TipoBloqueCalendario.Tarea
                                && !string.IsNullOrEmpty(_draggedBloque.IdAreaInteresPadre))
                            {
                                var targetBlock = targetDia.Bloques.FirstOrDefault(b =>
                                    b.Tipo == TipoBloqueCalendario.BloqueInteres
                                    && b.IdAreaInteres == _draggedBloque.IdAreaInteresPadre
                                    && nuevaHora >= b.HoraInicio && nuevaHora < b.HoraFin);
                                if (targetBlock != null)
                                {
                                    var movedBloque = targetDia.Bloques.FirstOrDefault(b => b.Id == _draggedBloque.Id);
                                    if (movedBloque != null)
                                    {
                                        await _viewModel.IntentarInsertarTareaEnBloqueAsync(targetDia, movedBloque.Id, targetBlock.Id);
                                    }
                                }
                            }
                        }
                        else
                        {
                            await _viewModel.MoverBloqueAsync(_draggedDia, _draggedBloque.Id, _draggedBloque.HoraInicio);
                            if (_draggedBloque.Tipo == TipoBloqueCalendario.Tarea
                                && !string.IsNullOrEmpty(_draggedBloque.IdAreaInteresPadre))
                            {
                                var targetBlock = _draggedDia.Bloques.FirstOrDefault(b =>
                                    b.Tipo == TipoBloqueCalendario.BloqueInteres
                                    && b.IdAreaInteres == _draggedBloque.IdAreaInteresPadre
                                    && _draggedBloque.HoraInicio >= b.HoraInicio
                                    && _draggedBloque.HoraInicio < b.HoraFin);
                                if (targetBlock != null)
                                {
                                    await _viewModel.IntentarInsertarTareaEnBloqueAsync(_draggedDia, _draggedBloque.Id, targetBlock.Id);
                                }
                            }
                        }
                    }
                }
            }

            if (wasResized)
                await _viewModel.RedimensionarBloqueAsyncResult(_draggedDia, _draggedBloque.Id, _draggedBloque.HoraInicio, _draggedBloque.HoraFin);
        }
        catch { }

        _draggedBorder = null;
        _draggedBloque = null;
        _draggedDia = null;
        _draggedDayIndex = -1;

        RenderAllDays();
    }

    private int GetTargetDayIndex(PointerEventArgs e)
    {
        for (int i = 0; i < 7; i++)
        {
            var pos = e.GetPosition(_dayPanels[i]);
            if (pos.X >= 0 && pos.X < _dayPanels[i].Bounds.Width)
                return i;
        }
        return -1;
    }

    private void OnPanelPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        if (_dragMode != DragMode.None) return;

        var (item, dragType) = FindPanelDragItem(e.Source as Visual);
        if (item == null || dragType == null) return;

        _panelDragItem = item;
        _panelDragType = dragType;
        _panelDragStartPoint = e.GetPosition(this);
        _panelDragStarted = false;
        _panelDragInitiatingEvent = e;
    }

    private void OnPanelPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_panelDragItem == null || _panelDragType == null || _panelDragStarted) return;

        var currentPoint = e.GetPosition(this);
        var diff = currentPoint - _panelDragStartPoint;

        if (Math.Abs(diff.X) > 5 || Math.Abs(diff.Y) > 5)
        {
            _panelDragStarted = true;
            StartPanelDrag();
        }
    }

    private void OnPanelPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _panelDragItem = null;
        _panelDragType = null;
        _panelDragStarted = false;
        _panelDragInitiatingEvent = null;
    }

    private async void StartPanelDrag()
    {
        if (_panelDragInitiatingEvent == null) return;

        var data = new DataTransfer();
        var dragItem = _panelDragItem;
        var dragType = _panelDragType;
        var initiator = _panelDragInitiatingEvent;

        _panelDragItem = null;
        _panelDragType = null;
        _panelDragInitiatingEvent = null;

        if (dragType == "AreaInteres" && dragItem is AreaInteres area)
        {
            data.Add(DataTransferItem.CreateText(
                $"CalendarDrop:AreaInteres:{area.IdAreaInteres}:{area.ColorHex ?? "#a78bfa"}"));
        }
        else if (dragType == "Tarea" && dragItem is Tarea tarea)
        {
            data.Add(DataTransferItem.CreateText(
                $"CalendarDrop:Tarea:{tarea.IdTarea}:{tarea.IdAreaInteres ?? ""}:{tarea.TiempoEstimado}"));
        }
        else
        {
            return;
        }

        try
        {
            await DragDrop.DoDragDropAsync(initiator, data, DragDropEffects.Copy);
        }
        catch { }
    }

    private (object?, string?) FindPanelDragItem(Visual? source)
    {
        if (source == null) return (null, null);
        var current = source;

        while (current != null && current != this)
        {
            if (current is Control control && control.DataContext != null)
            {
                if (control.DataContext is AreaInteres areaItem)
                    return (areaItem, "AreaInteres");
                if (control.DataContext is Tarea tareaItem)
                    return (tareaItem, "Tarea");
                if (control.DataContext is AreaConTareas act)
                    return (act.Area, "AreaInteres");
            }

            current = current.GetVisualParent() as Visual;
        }

        return (null, null);
    }

    private static double SnapToGrid(double pixels)
    {
        return Math.Round(pixels / SnapPixels) * SnapPixels;
    }

    private static TimeSpan PixelsToTime(double pixels)
    {
        var totalMinutes = pixels * 60.0 / PixelsPerHour;
        var snapped = Math.Round(totalMinutes / SnapMinutes) * SnapMinutes;
        return TimeSpan.FromMinutes(Math.Max(0, Math.Min(snapped, 24 * 60)));
    }

    private static SolidColorBrush CreateBrush(string hex, double opacity)
    {
        var color = Color.Parse(hex);
        return new SolidColorBrush(color) { Opacity = opacity };
    }

    private static string GetTransportIcon(MetodoTransporte metodo) => metodo switch
    {
        MetodoTransporte.Caminar => "\uE73A",
        MetodoTransporte.Auto => "\uE8CC",
        MetodoTransporte.Bus => "\uE826",
        _ => "\uE8CC"
    };

    private static void CalculateOverlapColumns(List<BloqueCalendario> blocks)
    {
        foreach (var b in blocks)
        {
            b.ColumnIndex = 0;
            b.ColumnCount = 1;
        }

        if (blocks.Count <= 1) return;

        var sorted = blocks
            .Where(b => b.Tipo != TipoBloqueCalendario.SeccionTraslado)
            .OrderBy(b => b.HoraInicio)
            .ThenByDescending(b => b.HoraFin - b.HoraInicio)
            .ToList();

        if (sorted.Count == 0) return;

        var columns = new List<List<BloqueCalendario>>();

        foreach (var block in sorted)
        {
            int placedColumn = -1;
            for (int c = 0; c < columns.Count; c++)
            {
                bool overlaps = columns[c].Any(b =>
                    b.HoraInicio < block.HoraFin && b.HoraFin > block.HoraInicio);
                if (!overlaps)
                {
                    placedColumn = c;
                    break;
                }
            }

            if (placedColumn == -1)
            {
                placedColumn = columns.Count;
                columns.Add(new List<BloqueCalendario>());
            }

            columns[placedColumn].Add(block);
            block.ColumnIndex = placedColumn;
        }

        var assigned = new HashSet<BloqueCalendario>();
        foreach (var block in sorted)
        {
            if (assigned.Contains(block)) continue;

            var queue = new Queue<BloqueCalendario>();
            queue.Enqueue(block);
            var groupMembers = new List<BloqueCalendario>();

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (assigned.Contains(current)) continue;
                assigned.Add(current);
                groupMembers.Add(current);

                foreach (var other in sorted)
                {
                    if (!assigned.Contains(other) &&
                        other.HoraInicio < current.HoraFin &&
                        other.HoraFin > current.HoraInicio)
                    {
                        queue.Enqueue(other);
                    }
                }
            }

            var maxCol = groupMembers.Max(b => b.ColumnIndex);
            foreach (var b in groupMembers)
                b.ColumnCount = maxCol + 1;
        }

        var traslados = blocks
            .Where(b => b.Tipo == TipoBloqueCalendario.SeccionTraslado)
            .OrderBy(b => b.HoraInicio)
            .ToList();

        foreach (var traslado in traslados)
        {
            var nextTask = sorted
                .FirstOrDefault(b => b.HoraInicio >= traslado.HoraFin);

            if (nextTask != null)
            {
                traslado.ColumnIndex = nextTask.ColumnIndex;
                traslado.ColumnCount = nextTask.ColumnCount;
            }
            else
            {
                var prevTask = sorted
                    .LastOrDefault(b => b.HoraFin <= traslado.HoraInicio);

                if (prevTask != null)
                {
                    traslado.ColumnIndex = prevTask.ColumnIndex;
                    traslado.ColumnCount = prevTask.ColumnCount;
                }
            }
        }
    }

    private bool OverlapsWithForeignTraslado(TimeSpan horaInicio, TimeSpan duracion, DiaCalendario dia, BloqueCalendario draggedBloque)
    {
        var horaFin = horaInicio + duracion;

        var sorted = dia.Bloques.OrderBy(b => b.HoraInicio).ToList();

        var draggedIndex = sorted.FindIndex(b => b.Id == draggedBloque.Id);

        for (int i = 0; i < sorted.Count; i++)
        {
            var bloque = sorted[i];
            if (bloque.Tipo != TipoBloqueCalendario.SeccionTraslado) continue;

            if (horaInicio < bloque.HoraFin && horaFin > bloque.HoraInicio)
            {
                if (i > 0 && sorted[i - 1].Id == draggedBloque.Id)
                    continue;
                if (i + 1 < sorted.Count && sorted[i + 1].Id == draggedBloque.Id)
                    continue;

                return true;
            }
        }

        return false;
    }

    private bool OverlapsWithAnyTraslado(TimeSpan horaInicio, TimeSpan duracion, DiaCalendario dia)
    {
        var horaFin = horaInicio + duracion;

        foreach (var bloque in dia.Bloques)
        {
            if (bloque.Tipo != TipoBloqueCalendario.SeccionTraslado) continue;

            if (horaInicio < bloque.HoraFin && horaFin > bloque.HoraInicio)
                return true;
        }

        return false;
    }
}