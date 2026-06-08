using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PlanificApp.Models
{
    // --- TUS RECORDS ORIGINALES (Intactos) ---
    public record Ubicacion(string Nombre, double Latitud, double Longitud);
    public record Region(int IdRegion, string Nombre);
    public record Comuna(int IdComuna, string Nombre, int IdRegionPadre);

    // --- NUEVO MODELO PARA MONGODB ---
    public class UbicacionGuardada
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string IdUbicacion { get; set; } = string.Empty;

        public string IdUsuario { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string DireccionExacta { get; set; } = string.Empty;
        public string AreaInteres { get; set; } = string.Empty;
        public string ColorHex { get; set; } = string.Empty;
        public string TransportePreferido { get; set; } = string.Empty;
        public double Latitud { get; set; }
        public double Longitud { get; set; }
    }
}