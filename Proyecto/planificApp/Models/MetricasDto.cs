namespace planificApp.Models;

public class MetricasDto
{
    public int TotalUsuarios { get; set; }
    public int UsuariosActivos { get; set; }
    public int UsanGeneracionSemanal { get; set; }
    public double CambiosPorGeneracion { get; set; }

    public int TareasCreadas { get; set; }
    public int TareasCompletadas { get; set; }
    public int GeneracionesRealizadas { get; set; }
    public int GeneracionesModificadas { get; set; }
    public int TareasEnGeneracionSemanal { get; set; }
    public int TareasModificadasGeneracionSemanal { get; set; }
}