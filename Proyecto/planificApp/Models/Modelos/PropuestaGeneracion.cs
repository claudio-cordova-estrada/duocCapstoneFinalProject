using System.Collections.Generic;
using PlanificApp.Models;

namespace PlanificApp.Models;

public enum TipoPropuesta { Equilibrio, Rushear, Relajado }

public class PropuestaGeneracion
{
    public TipoPropuesta Tipo { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public SemanaCalendario Semana { get; set; } = new();
    public bool EsValida { get; set; } = true;
    public string MensajeInvalidacion { get; set; } = string.Empty;
    public int MinimoDiasRequeridos { get; set; }
    public int DiasSeleccionadosCount { get; set; }
    public double HorasFuncionalesUsadas { get; set; }
    public double HorasLibresUsadas { get; set; }
    public int TotalBloques { get; set; }
    public int TotalTareas { get; set; }
}