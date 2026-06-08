using PlanificApp.Models;
using Avalonia.Controls;

namespace planificApp.Helpers;

public static class DetalleTareaHelper
{
    public static int UbicacionToIndex(string? ubicacion) => ubicacion switch
    {
        "Casa" => 1, "Trabajo" => 2, "Universidad" => 3,
        "Gimnasio" => 4, "Supermercado" => 5, "Otro" => 6,
        _ => 0
    };

    public static string? UbicacionFromIndex(int index) => index switch
    {
        1 => "Casa", 2 => "Trabajo", 3 => "Universidad",
        4 => "Gimnasio", 5 => "Supermercado", 6 => "Otro",
        _ => null
    };

    public static int TiempoEstimadoToIndex(int minutos) => minutos switch
    {
        5 => 1, 10 => 2, 15 => 3, 30 => 4, 45 => 5,
        60 => 6, 90 => 7, 120 => 8, 180 => 9, 240 => 10,
        _ => 0
    };

    public static int TiempoEstimadoFromIndex(int index) => index switch
    {
        1 => 5, 2 => 10, 3 => 15, 4 => 30, 5 => 45,
        6 => 60, 7 => 90, 8 => 120, 9 => 180, 10 => 240,
        _ => 0
    };
    
    public static string? TipoActividadFisicaFromIndex(int index) => index switch
    {
        1 => "Activa", 
        2 => "Pasiva",
        _ => null
    };
    
    public static string? TipoActividadMentalFromIndex(int index) => index switch
    {
        1 => "Obligacion", 
        2 => "Recreacion",
        _ => null
    };
    
    public static int TipoActividadFisicaToIndex(string? tipo) => tipo switch
    {
        "Activa" => 1, 
        "Pasiva" => 2,
        _ => 0
    };
    
    public static int TipoActividadMentalToIndex(string? tipo) => tipo switch
    {
        "Obligacion" => 1, 
        "Recreacion" => 2,
        _ => 0
    };

    public static void AplicarDefaultsArea(Tarea tarea, AreaInteres area)
    {
        if (area.UbicacionPred != null)
            tarea.Ubicacion = area.UbicacionPred;
        if (area.TipoActividadFisicaPred != null)
            tarea.TipoActividadFisica = area.TipoActividadFisicaPred;
        if (area.TipoActividadMentalPred != null)
            tarea.TipoActividadMental = area.TipoActividadMentalPred;
        if (area.MetodoTransportePred != null)
            tarea.MetodoTransporte = area.MetodoTransportePred;
    }

    public static string CalcularEstado(Tarea tarea) =>
        tarea.FecCompletado != null ? "Completada" :
        TareaAtrasada.EsAtrasada(tarea) ? "Vencida" : "Activa";

    public static void PopulateAreaComboBox(ComboBox comboBox, System.Collections.Generic.IEnumerable<AreaInteres> areas, string? selectedId)
    {
        comboBox.Items.Clear();
        var sinDefinir = new ComboBoxItem { Content = "— Sin definir —", Tag = null };
        comboBox.Items.Add(sinDefinir);
        foreach (var area in areas)
        {
            var item = new ComboBoxItem { Content = area.Nombre, Tag = area.IdAreaInteres };
            comboBox.Items.Add(item);
            if (area.IdAreaInteres == selectedId)
                comboBox.SelectedItem = item;
        }
        if (comboBox.SelectedItem == null)
            comboBox.SelectedIndex = 0;
    }

    public static string? GetSelectedAreaId(ComboBox comboBox)
    {
        if (comboBox.SelectedItem is ComboBoxItem item && item.Tag != null)
            return item.Tag.ToString();
        return null;
    }
}