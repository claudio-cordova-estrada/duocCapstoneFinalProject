namespace PlanificApp.Models
{
    // Record para coordenadas y nombres de ubicaciones específicas
    public record Ubicacion(string Nombre, double Latitud, double Longitud);

    // Record para la estructura administrativa de Regiones
    public record Region(int IdRegion, string Nombre);

    // Record para Comunas vinculado a su región padre
    public record Comuna(int IdComuna, string Nombre, int IdRegionPadre);
}