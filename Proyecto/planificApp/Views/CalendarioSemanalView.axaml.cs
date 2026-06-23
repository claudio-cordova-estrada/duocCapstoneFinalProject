using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
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
    private static readonly SolidColorBrush TrasladoBackgroundBrush = new(Color.Parse("#210101"));
    private static readonly SolidColorBrush TrasladoBorderBrush = new(Color.Parse("#ef4444"));

    // Resuelve un brush del tema actual (claro/oscuro); usa el fallback si no existe.
    private static IBrush ThemeBrush(string key, string fallback)
    {
        if (Avalonia.Application.Current is { } app
            && app.TryGetResource(key, app.ActualThemeVariant, out var res)
            && res is IBrush b)
            return b;
        return new SolidColorBrush(Color.Parse(fallback));
    }
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

    // B1: mientras haya un ContextMenu abierto, posponemos el re-render para no cerrarlo.
    private bool _isContextMenuOpen;
    private bool _pendingRender;

    // C3: fantasma (preview) que sigue al cursor mientras se arrastra un bloque/sub-tarea.
    private Border? _dragGhost;

    // Resize: para re-renderizar los bloques cuando cambia el ancho de la vista.
    private double _lastRenderWidth = -1;

    // Bug 2: guard de re-entrada — mientras se procesa un drop, ignoramos nuevos arrastres/drops
    // para evitar que operaciones async se solapen y corrompan el estado (crash con acciones rápidas).
    private bool _isProcessingDrop;

    public CalendarioSemanalView()
    {
        InitializeComponent();

        for (int i = 0; i < 7; i++)
        {
            _dayPanels[i] = this.FindControl<Panel>($"Day{i}")!;
            _dayHeaderNames[i] = this.FindControl<TextBlock>($"DayHeader{i}_Name")!;
            _dayHeaderNums[i] = this.FindControl<TextBlock>($"DayHeader{i}_Num")!;
            _dayHeaders[i] = this.FindControl<Border>($"DayHeader{i}")!;

            // Feature escoba: botón para limpiar todas las tareas/bloques del día.
            if (_dayHeaders[i].Child is StackPanel headerPanel)
            {
                int dayIdx = i;
                var broom = new Button
                {
                    Content = "\U0001F9F9",
                    FontSize = 11,
                    Padding = new Thickness(2, 0),
                    Margin = new Thickness(0, 2, 0, 0),
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Cursor = new Cursor(StandardCursorType.Hand)
                };
                ToolTip.SetTip(broom, "Limpiar día");
                broom.Click += async (s, e) => await LimpiarDiaSeguro(dayIdx);
                headerPanel.Children.Add(broom);
            }
        }

        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;

        AddHandler(PointerPressedEvent, OnPanelPointerPressed, RoutingStrategies.Tunnel);
        AddHandler(PointerMovedEvent, OnPanelPointerMoved, RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, OnPanelPointerReleased, RoutingStrategies.Tunnel);

        // Resize: cuando cambia el ancho de la vista, re-renderizamos para que los bloques tomen
        // el nuevo ancho de las columnas (que ahora son proporcionales).
        this.PropertyChanged += OnViewPropertyChanged;

        Loaded += OnLoaded;
    }

    private void OnViewPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == BoundsProperty)
        {
            var w = Bounds.Width;
            if (Math.Abs(w - _lastRenderWidth) > 1)
            {
                _lastRenderWidth = w;
                RenderAllDays();
            }
        }
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
        if (_dragGhost != null) ActualizarDragGhost(e.GetPosition(this));

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
                    _dayHeaderNames[i].Foreground = ThemeBrush("AccentColor", "#a78bfa");
                    _dayHeaderNums[i].Foreground = ThemeBrush("AccentColor", "#a78bfa");
                    _dayHeaderNums[i].FontWeight = Avalonia.Media.FontWeight.Medium;
                    if (i < _dayHeaders.Length && _dayHeaders[i] != null)
                        _dayHeaders[i].Background = ThemeBrush("CalendarTodayBackground", "#1c1832");
                }
                else
                {
                    _dayHeaderNames[i].Foreground = ThemeBrush("TextDisabled", "#555555");
                    _dayHeaderNums[i].Foreground = ThemeBrush("TextDisabled", "#555555");
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

    // C3: muestra un fantasma (preview) con el nombre/color del bloque siguiendo al cursor.
    private void MostrarDragGhost(string texto, string? colorHex, Point posEnView)
    {
        var overlay = OverlayLayer.GetOverlayLayer(this);
        if (overlay == null) return;
        QuitarDragGhost();

        var color = !string.IsNullOrEmpty(colorHex) ? colorHex : "#a78bfa";
        _dragGhost = new Border
        {
            Background = CreateBrush(color, 0.85),
            BorderBrush = CreateBrush(color, 1.0),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 4),
            IsHitTestVisible = false,
            Opacity = 0.9,
            Child = new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(texto) ? "Mover" : texto,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.Parse("#ffffff")),
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 160
            }
        };
        overlay.Children.Add(_dragGhost);
        ActualizarDragGhost(posEnView);
    }

    private void ActualizarDragGhost(Point posEnView)
    {
        if (_dragGhost == null) return;
        var overlay = OverlayLayer.GetOverlayLayer(this);
        if (overlay == null) return;
        var p = this.TranslatePoint(posEnView, overlay) ?? posEnView;
        Canvas.SetLeft(_dragGhost, p.X + 12);
        Canvas.SetTop(_dragGhost, p.Y + 12);
    }

    private void QuitarDragGhost()
    {
        if (_dragGhost == null) return;
        OverlayLayer.GetOverlayLayer(this)?.Children.Remove(_dragGhost);
        _dragGhost = null;
    }

    private async Task LimpiarDiaSeguro(int dayIdx)
    {
        if (_viewModel == null || dayIdx < 0 || dayIdx >= _viewModel.Dias.Count) return;
        try
        {
            await _viewModel.LimpiarDiaAsync(_viewModel.Dias[dayIdx]);
            RenderAllDays();
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[CAL] Limpiar día falló: {ex.Message}"); }
    }

    // B1: marca el menú como abierto y, al cerrarse, ejecuta el render que quedó pendiente.
    private void TrackContextMenu(ContextMenu menu)
    {
        _isContextMenuOpen = true;
        menu.Closed += (s, e) =>
        {
            _isContextMenuOpen = false;
            if (_pendingRender)
            {
                _pendingRender = false;
                RenderAllDays();
            }
        };
    }

    private void RenderAllDays()
    {
        if (_viewModel == null) return;

        // B1: si hay un menú contextual abierto, posponemos el render para no cerrarlo.
        if (_isContextMenuOpen) { _pendingRender = true; return; }

        var horaInicio = _viewModel.HoraInicioJornada;
        var horaFin = _viewModel.HoraFinJornada;

        for (int i = 0; i < 7; i++)
        {
            if (i < _dayPanels.Length && _dayPanels[i] != null)
            {
                _dayPanels[i].Children.Clear();

                if (i < _viewModel.Dias.Count && _viewModel.Dias[i].EsHoy)
                    _dayPanels[i].Background = ThemeBrush("CalendarTodayBackground", "#1c1832");
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
                Background = ThemeBrush("CalendarHourLine", "#141414"),
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

                // Panel raíz que ocupa TODO el alto del bloque. Antes el contenido vivía en la
                // fila central de un Grid con manijas de 6px arriba/abajo, lo que desplazaba las
                // sub-tareas ~6px y las acortaba 12px (no calzaban). Ahora el contentPanel cubre
                // todo el bloque y las manijas van superpuestas, así el relativeTop de cada
                // sub-tarea coincide 1:1 con el eje de tiempo del bloque.
                var rootPanel = new Panel { ClipToBounds = false };

                var contentPanel = new Panel { ClipToBounds = false };
                rootPanel.Children.Add(contentPanel);

                if (bloque.TareasInternas != null && bloque.TareasInternas.Count > 0)
                {
                    // C2: columnas para sub-tareas que se solapan, para que se partan dentro del
                    // bloque en vez de encimarse.
                    CalculateSubOverlapColumns(bloque.TareasInternas);
                    foreach (var sub in bloque.TareasInternas)
                    {
                        var subBorder = CreateSubBloqueVisual(sub, bloque, dia, dayIndex, contentPanel, blockWidth);
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

                // Manijas de resize superpuestas (no consumen alto del contenido)
                var topHandle = new Border
                {
                    Height = ResizeHandleSize,
                    VerticalAlignment = VerticalAlignment.Top,
                    Background = Brushes.Transparent,
                    Cursor = new Cursor(StandardCursorType.SizeNorthSouth)
                };
                topHandle.PointerPressed += (s, e) => OnResizeHandlePressed(e, border, bloque, dia, dayIndex, DragMode.ResizeTop);
                rootPanel.Children.Add(topHandle);

                var bottomHandle = new Border
                {
                    Height = ResizeHandleSize,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Background = Brushes.Transparent,
                    Cursor = new Cursor(StandardCursorType.SizeNorthSouth)
                };
                bottomHandle.PointerPressed += (s, e) => OnResizeHandlePressed(e, border, bloque, dia, dayIndex, DragMode.ResizeBottom);
                rootPanel.Children.Add(bottomHandle);

                border.ClipToBounds = false;
                border.Child = rootPanel;
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

    private Border CreateSubBloqueVisual(SubBloqueTarea sub, BloqueCalendario parentBloque, DiaCalendario dia, int dayIndex, Panel contentPanel, double anchoBloque)
    {
        double relativeTop = (sub.HoraInicio - parentBloque.HoraInicio).TotalMinutes * (PixelsPerHour / 60.0);
        double subHeight = Math.Max(MinBlockHeight, (sub.HoraFin - sub.HoraInicio).TotalMinutes * (PixelsPerHour / 60.0));
        var areaColor = parentBloque.ColorHex ?? "#a78bfa";

        // C2: ancho y posición por columna cuando hay sub-tareas solapadas dentro del bloque.
        var colCount = Math.Max(1, sub.ColumnCount);
        var colIndex = Math.Max(0, sub.ColumnIndex);
        var anchoCol = (anchoBloque - 2) / colCount;
        if (anchoCol < 8) anchoCol = 8;
        var leftOffset = 1 + colIndex * anchoCol;
        var anchoSub = colCount > 1 ? anchoCol - 2 : anchoCol;

        var subBorder = new Border
        {
            Margin = new Thickness(leftOffset, relativeTop, 0, 0),
            Width = anchoSub,
            Height = subHeight,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Left,
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

        if (_isProcessingDrop) return;

        e.Handled = true;
        _dragMode = DragMode.MoveSubBloque;
        MostrarDragGhost(sub.Nombre, parentBloque.ColorHex, e.GetCurrentPoint(this).Position);
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
            if (_viewModel == null) return;
            try
            {
                await _viewModel.ExtraerSubTareaAsync(dia, parentBloque, sub.IdTarea, null);
                RenderAllDays();
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[CAL] Quitar del bloque falló: {ex.Message}"); }
        };
        menu.Items.Add(extractItem);

        var completarItem = new MenuItem { Header = sub.Completada ? "Descompletar" : "Completar" };
        completarItem.Click += (s, args) => _viewModel?.CompletarTareaBloqueCommand.Execute(sub);
        menu.Items.Add(completarItem);

        var deleteItem = new MenuItem { Header = "Eliminar" };
        deleteItem.Click += async (s, args) =>
        {
            if (_viewModel == null) return;
            try
            {
                await _viewModel.EliminarSubTareaAsync(dia, parentBloque.Id, sub.IdTarea);
                RenderAllDays();
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[CAL] Eliminar sub-tarea falló: {ex.Message}"); }
        };
        menu.Items.Add(deleteItem);

        TrackContextMenu(menu);
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

        if (_isProcessingDrop) return;

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
        MostrarDragGhost(bloque.NombreMostrar, bloque.ColorHex, e.GetCurrentPoint(this).Position);

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
                if (_viewModel == null) return;
                try
                {
                    await _viewModel.ToggleCompletarTareaCommand.ExecuteAsync(bloque);
                    RenderAllDays();
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[CAL] Completar tarea falló: {ex.Message}"); }
            };
            menu.Items.Add(completarItem);

            var unassignItem = new MenuItem { Header = "Quitar del calendario" };
            unassignItem.Click += async (s, args) =>
            {
                if (_viewModel == null) return;
                try
                {
                    await _viewModel.EliminarBloqueAsync(dia, bloque.Id);
                    RenderAllDays();
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[CAL] Quitar del calendario falló: {ex.Message}"); }
            };
            menu.Items.Add(unassignItem);
        }
        else if (bloque.Tipo == TipoBloqueCalendario.BloqueInteres)
        {
            var unassignItem = new MenuItem { Header = "Quitar del calendario" };
            unassignItem.Click += async (s, args) =>
            {
                if (_viewModel == null) return;
                try
                {
                    await _viewModel.EliminarBloqueAsync(dia, bloque.Id);
                    RenderAllDays();
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[CAL] Quitar del calendario falló: {ex.Message}"); }
            };
            menu.Items.Add(unassignItem);
        }

        var deleteItem = new MenuItem { Header = "Eliminar" };
        deleteItem.Click += async (s, args) =>
        {
            if (_viewModel == null) return;
            try
            {
                await _viewModel.EliminarTareaPermanentementeAsync(dia, bloque.Id);
                RenderAllDays();
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[CAL] Eliminar tarea falló: {ex.Message}"); }
        };
        menu.Items.Add(deleteItem);

        TrackContextMenu(menu);
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

        if (_dragGhost != null) ActualizarDragGhost(e.GetCurrentPoint(this).Position);

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
        QuitarDragGhost();

        if (_isProcessingDrop) { _dragMode = DragMode.None; return; }
        _isProcessingDrop = true;
        try
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
                        var targetDayIdx = GetTargetDayIndex(e);

                        if (onPanel)
                        {
                            await _viewModel.ExtraerSubTareaAsync(_draggedSubDia, _draggedSubParentBloque, _draggedSubBloque.IdTarea, null);
                        }
                        else if (targetDayIdx >= 0 && targetDayIdx != _draggedSubDayIndex && targetDayIdx < _viewModel.Dias.Count)
                        {
                            // C4: soltada en OTRO día → sale del bloque y se coloca en ese día.
                            var targetDia = _viewModel.Dias[targetDayIdx];
                            var posOnTarget = e.GetPosition(_dayPanels[targetDayIdx]);
                            var horaDestino = PixelsToTime(posOnTarget.Y);
                            horaDestino = TimeSpan.FromMinutes(Math.Round(horaDestino.TotalMinutes / SnapMinutes) * SnapMinutes);
                            await _viewModel.MoverSubTareaADiaAsync(_draggedSubDia, _draggedSubParentBloque, _draggedSubBloque.IdTarea, targetDia, horaDestino);
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

                        // Soltar una tarea sobre un bloque de área de SU área (mismo día): se inserta
                        // directamente en un solo paso (el bloque crece y la absorbe), evitando el
                        // mover-recalcular intermedio que antes dejaba la tarea afuera (1ª tarea).
                        BloqueCalendario? areaTarget = null;
                        if (targetDayIdx == _draggedDayIndex
                            && _draggedBloque.Tipo == TipoBloqueCalendario.Tarea
                            && !string.IsNullOrEmpty(_draggedBloque.IdAreaInteresPadre))
                        {
                            areaTarget = targetDia.Bloques.FirstOrDefault(b =>
                                b.Tipo == TipoBloqueCalendario.BloqueInteres
                                && b.Id != _draggedBloque.Id
                                && b.IdAreaInteres == _draggedBloque.IdAreaInteresPadre
                                && nuevaHora >= b.HoraInicio && nuevaHora < b.HoraFin);
                        }

                        // Soltar un bloque de área sobre una tarea de SU área (o su traslado): el
                        // bloque se adapta para cubrir la tarea y la absorbe, heredando el traslado.
                        // Se decide por dónde cae el MOUSE (nuevaHora), no por el rango del bloque.
                        BloqueCalendario? tareaAbsorber = null;
                        if (targetDayIdx == _draggedDayIndex
                            && _draggedBloque.Tipo == TipoBloqueCalendario.BloqueInteres
                            && _draggedBloque.IdAreaInteres != null)
                        {
                            // El mouse cae sobre una tarea de esta área.
                            tareaAbsorber = targetDia.Bloques.FirstOrDefault(b =>
                                b.Tipo == TipoBloqueCalendario.Tarea
                                && b.IdAreaInteresPadre == _draggedBloque.IdAreaInteres
                                && nuevaHora >= b.HoraInicio && nuevaHora < b.HoraFin);

                            // O bien el mouse cae sobre el traslado cuya tarea siguiente es de esta área.
                            if (tareaAbsorber == null)
                            {
                                var traslEnPunto = targetDia.Bloques.FirstOrDefault(b =>
                                    b.Tipo == TipoBloqueCalendario.SeccionTraslado
                                    && nuevaHora >= b.HoraInicio && nuevaHora < b.HoraFin);
                                if (traslEnPunto != null)
                                    tareaAbsorber = targetDia.Bloques.FirstOrDefault(b =>
                                        b.Tipo == TipoBloqueCalendario.Tarea
                                        && b.IdAreaInteresPadre == _draggedBloque.IdAreaInteres
                                        && Math.Abs((b.HoraInicio - traslEnPunto.HoraFin).TotalMinutes) < 1);
                            }
                        }

                        if (areaTarget != null)
                        {
                            _draggedBloque.HoraInicio = nuevaHora;
                            _draggedBloque.HoraFin = nuevaHora + duracion;
                            var insertado = await _viewModel.IntentarInsertarTareaEnBloqueAsync(_draggedDia, _draggedBloque.Id, areaTarget.Id);
                            if (!insertado)
                            {
                                _draggedBloque.HoraInicio = _dragOriginalHoraInicio;
                                _draggedBloque.HoraFin = _dragOriginalHoraFin;
                            }
                        }
                        else if (tareaAbsorber != null)
                        {
                            // Restauramos el inicio original para que el movimiento del bloque (y
                            // sus sub-tareas) calcule el delta correcto antes de absorber la tarea.
                            _draggedBloque.HoraInicio = _dragOriginalHoraInicio;
                            _draggedBloque.HoraFin = _dragOriginalHoraFin;
                            await _viewModel.AbsorberTareaEnBloqueAsync(_draggedDia, _draggedBloque.Id, tareaAbsorber.Id, nuevaHora);
                        }
                        else if (OverlapsWithForeignTraslado(nuevaHora, duracion, targetDia, _draggedBloque, _dragOriginalHoraInicio))
                        {
                            _draggedBloque.HoraInicio = _dragOriginalHoraInicio;
                            _draggedBloque.HoraFin = _dragOriginalHoraFin;
                        }
                        else if (targetDayIdx != _draggedDayIndex)
                        {
                            // Restauramos el inicio original para que MoverBloqueADia calcule el
                            // delta real (final - original) y desplace también las sub-tareas (B2).
                            _draggedBloque.HoraInicio = _dragOriginalHoraInicio;
                            _draggedBloque.HoraFin = _dragOriginalHoraFin;
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
                            // Restauramos el inicio original para que MoverBloque calcule el delta
                            // real (final - original) y desplace también las sub-tareas (B2).
                            var finalHora = _draggedBloque.HoraInicio;
                            _draggedBloque.HoraInicio = _dragOriginalHoraInicio;
                            _draggedBloque.HoraFin = _dragOriginalHoraFin;
                            await _viewModel.MoverBloqueAsync(_draggedDia, _draggedBloque.Id, finalHora);
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
        finally
        {
            _isProcessingDrop = false;
        }
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
            MostrarDragGhost(area.Nombre, area.ColorHex, initiator.GetPosition(this));
        }
        else if (dragType == "Tarea" && dragItem is Tarea tarea)
        {
            data.Add(DataTransferItem.CreateText(
                $"CalendarDrop:Tarea:{tarea.IdTarea}:{tarea.IdAreaInteres ?? ""}:{tarea.TiempoEstimado}"));
            MostrarDragGhost(tarea.Nombre, "#666666", initiator.GetPosition(this));
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
        finally
        {
            QuitarDragGhost();
        }
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

    // C2: asigna columnas a las sub-tareas que se solapan dentro de un bloque de área, para que
    // se dibujen lado a lado en vez de encimarse.
    private static void CalculateSubOverlapColumns(IList<SubBloqueTarea> subs)
    {
        foreach (var s in subs) { s.ColumnIndex = 0; s.ColumnCount = 1; }
        if (subs.Count <= 1) return;

        var sorted = subs
            .OrderBy(s => s.HoraInicio)
            .ThenByDescending(s => s.HoraFin - s.HoraInicio)
            .ToList();

        var columns = new List<List<SubBloqueTarea>>();
        foreach (var s in sorted)
        {
            int placed = -1;
            for (int c = 0; c < columns.Count; c++)
            {
                bool overlaps = columns[c].Any(o => o.HoraInicio < s.HoraFin && o.HoraFin > s.HoraInicio);
                if (!overlaps) { placed = c; break; }
            }
            if (placed == -1) { placed = columns.Count; columns.Add(new List<SubBloqueTarea>()); }
            columns[placed].Add(s);
            s.ColumnIndex = placed;
        }

        // ColumnCount por componente de solape (mismo criterio que CalculateOverlapColumns).
        var assigned = new HashSet<SubBloqueTarea>();
        foreach (var s in sorted)
        {
            if (assigned.Contains(s)) continue;
            var queue = new Queue<SubBloqueTarea>();
            queue.Enqueue(s);
            var group = new List<SubBloqueTarea>();
            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                if (assigned.Contains(cur)) continue;
                assigned.Add(cur);
                group.Add(cur);
                foreach (var o in sorted)
                    if (!assigned.Contains(o) && o.HoraInicio < cur.HoraFin && o.HoraFin > cur.HoraInicio)
                        queue.Enqueue(o);
            }
            var maxCol = group.Max(g => g.ColumnIndex);
            foreach (var g in group) g.ColumnCount = maxCol + 1;
        }
    }

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

    private bool OverlapsWithForeignTraslado(TimeSpan horaInicio, TimeSpan duracion, DiaCalendario dia, BloqueCalendario draggedBloque, TimeSpan originalStart)
    {
        var horaFin = horaInicio + duracion;

        foreach (var bloque in dia.Bloques)
        {
            if (bloque.Tipo != TipoBloqueCalendario.SeccionTraslado) continue;

            // Excluimos el traslado PROPIO del bloque arrastrado (el que llegaba a su posición
            // original; se recalcula al mover). Cualquier OTRO traslado que se solape cancela el
            // movimiento (B4: no se puede colocar una tarea/bloque encima de un traslado).
            if (Math.Abs((bloque.HoraFin - originalStart).TotalMinutes) < 1) continue;

            if (horaInicio < bloque.HoraFin && horaFin > bloque.HoraInicio)
                return true;
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