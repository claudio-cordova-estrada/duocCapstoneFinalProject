using System;
using System.Collections.ObjectModel;
using PlanificApp.Models.Enums;

namespace PlanificApp.Models
{
    public enum TipoBloqueCalendario
    {
        BloqueInteres,
        Tarea,
        SeccionTraslado
    }

    public class SubBloqueTarea
    {
        public string IdTarea { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFin { get; set; }
        public bool Completada { get; set; }
    }

    public class BloqueCalendario
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public TipoBloqueCalendario Tipo { get; set; }
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFin { get; set; }

        public string? ColorHex { get; set; }

        public string? IdAreaInteres { get; set; }
        public string? NombreArea { get; set; }
        public ObservableCollection<SubBloqueTarea>? TareasInternas { get; set; }

        public string? IdTarea { get; set; }
        public string? NombreTarea { get; set; }
        public string? IdAreaInteresPadre { get; set; }
        public bool Completada { get; set; }

        public string? UbicacionOrigen { get; set; }
        public string? UbicacionDestino { get; set; }
        public MetodoTransporte? MetodoTransporte { get; set; }
        public int? DuracionMinutos { get; set; }
        public bool EsEstimado { get; set; }

        public double TopPx => HoraInicio.TotalMinutes * (64.0 / 60.0);
        public double HeightPx => (HoraFin - HoraInicio).TotalMinutes * (64.0 / 60.0);
        public int ColumnIndex { get; set; }
        public int ColumnCount { get; set; } = 1;
    }

    public class DiaCalendario
    {
        public DateTime Fecha { get; set; }
        public string NombreDia { get; set; } = string.Empty;
        public int NumeroDia { get; set; }
        public bool EsHoy { get; set; }
        public ObservableCollection<BloqueCalendario> Bloques { get; set; } = new();
    }

    public class SemanaCalendario
    {
        public int NumeroSemana { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public ObservableCollection<DiaCalendario> Dias { get; set; } = new();
    }

    public class AreaConTareas
    {
        public AreaInteres Area { get; set; } = null!;
        public ObservableCollection<Tarea> Tareas { get; set; } = new();
    }
}