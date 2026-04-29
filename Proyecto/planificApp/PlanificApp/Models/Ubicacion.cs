namespace PlanificApp.Models
{
    // Ubicacion, Region y Comuna son registros que representan ubicaciones geográficas con nombre y coordenadas. Todas obtenidas desde una API.
    public record Ubicacion(string Nombre, double Latitud, double Longitud);
    public record Region();
    public record Comuna();
}