using System;
using System.Collections.Generic;

namespace PlanificApp.Models;

public class CondicionesGeneracion
{
    public List<AreaInteres> AreasConsiderar { get; set; } = new();
    public List<AreaPriorizada> AreasPriorizadas { get; set; } = new();
    public double MaxHorasGeneracionSemanal { get; set; }
    public double HorasFuncionales { get; set; }
    public double HorasLibres { get; set; }
    public List<DayOfWeek> DiasSeleccionados { get; set; } = new();
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
}

public class AreaPriorizada
{
    public AreaInteres Area { get; set; } = null!;
    public int TareasCercanas { get; set; }
    public int TareasSemanaSiguiente { get; set; }
    public int TareasTotales { get; set; }
    public int NivelPriorizacion => (int)Area.Prioridad;
}