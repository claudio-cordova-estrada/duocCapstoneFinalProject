using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using PlanificApp.Models;

namespace planificApp.Views;

public partial class PropuestaPreviewWindow : Window
{
    private const double PixelsPerHour = 48.0;

    public PropuestaPreviewWindow()
    {
        InitializeComponent();
    }

    public void SetPropuesta(PropuestaGeneracion propuesta)
    {
        TxtTitulo.Text = propuesta.Nombre;
        TxtDescripcion.Text = propuesta.Descripcion;
        TxtEstadisticas.Text = $"{propuesta.HorasFuncionalesUsadas:F1} hrs planificadas - {propuesta.TotalBloques} bloques - {propuesta.TotalTareas} tareas";

        DiasPanel.Children.Clear();

        foreach (var dia in propuesta.Semana.Dias)
        {
            var diaBorder = new Border
            {
                Background = dia.EsHoy ? new SolidColorBrush(Color.Parse("#1a1a3a")) : new SolidColorBrush(Color.Parse("#12121e")),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8),
                BorderBrush = dia.EsHoy ? new SolidColorBrush(Color.Parse("#a78bfa")) : new SolidColorBrush(Color.Parse("#252540")),
                BorderThickness = new Thickness(1)
            };

            var diaStack = new StackPanel { Spacing = 4 };

            var header = new TextBlock
            {
                Text = $"{dia.NombreDia} {dia.NumeroDia}",
                FontSize = 12,
                FontWeight = FontWeight.Medium,
                Foreground = dia.EsHoy ? new SolidColorBrush(Color.Parse("#a78bfa")) : new SolidColorBrush(Color.Parse("#999999"))
            };
            diaStack.Children.Add(header);

            if (dia.Bloques.Count == 0)
            {
                diaStack.Children.Add(new TextBlock
                {
                    Text = "Sin actividades",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.Parse("#555555")),
                    Margin = new Thickness(8, 2, 0, 0)
                });
            }
            else
            {
                foreach (var bloque in dia.Bloques)
                {
                    var color = !string.IsNullOrEmpty(bloque.ColorHex) ? bloque.ColorHex : "#a78bfa";

                    var bloqueLabel = bloque.Tipo == TipoBloqueCalendario.SeccionTraslado
                        ? $"{bloque.UbicacionOrigen} \u2192 {bloque.UbicacionDestino}: {bloque.HoraInicio:hh\\:mm}-{bloque.HoraFin:hh\\:mm}"
                        : bloque.Tipo == TipoBloqueCalendario.BloqueInteres
                            ? $"{bloque.NombreArea}: {bloque.HoraInicio:hh\\:mm}-{bloque.HoraFin:hh\\:mm}"
                            : $"{bloque.NombreTarea}: {bloque.HoraInicio:hh\\:mm}-{bloque.HoraFin:hh\\:mm}";

                    var blockBorder = new Border
                    {
                        Background = CreateBrush(color, 0.25),
                        BorderBrush = CreateBrush(color, 0.5),
                        BorderThickness = new Thickness(1, 0, 0, 2),
                        CornerRadius = new CornerRadius(3),
                        Padding = new Thickness(6, 3),
                        Margin = new Thickness(4, 1, 0, 0)
                    };

                    var label = new TextBlock
                    {
                        Text = bloqueLabel,
                        FontSize = 10,
                        Foreground = new SolidColorBrush(Color.Parse("#ffffff"))
                    };
                    blockBorder.Child = label;
                    diaStack.Children.Add(blockBorder);

                    if (bloque.TareasInternas != null)
                    {
                        foreach (var sub in bloque.TareasInternas)
                        {
                            var subLabel = new TextBlock
                            {
                                Text = $"  \u2022 {sub.Nombre} ({sub.HoraInicio:hh\\:mm}-{sub.HoraFin:hh\\:mm})",
                                FontSize = 9,
                                Foreground = new SolidColorBrush(Color.Parse("#bbbbbb")),
                                Margin = new Thickness(12, 0, 0, 0)
                            };
                            diaStack.Children.Add(subLabel);
                        }
                    }
                }
            }

            diaBorder.Child = diaStack;
            DiasPanel.Children.Add(diaBorder);
        }
    }

    private static SolidColorBrush CreateBrush(string hex, double opacity)
    {
        var color = Color.Parse(hex);
        return new SolidColorBrush(color) { Opacity = opacity };
    }
}