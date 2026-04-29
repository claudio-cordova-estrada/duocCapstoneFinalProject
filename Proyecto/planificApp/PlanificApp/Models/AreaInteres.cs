using MongoDB.Bson.Serialization.Attributes;
using PlanificApp.Models.Enums;

namespace PlanificApp.Models
{
    public class AreaInteres
    {
        [BsonId]
        public int IdAreaInteres { get; set; } // int(5)
        public string Nombre { get; set; } // necesita logica de validacion de caracteres numéricos y especiales.
        public bool GeneracionSemanal { get; set; } // si es true, las horas semanales deben ser > 0.
        public PrioridadAreaInteres Prioridad { get; set; } = PrioridadAreaInteres.Predeterminado;
        public int HorasSemanales { get; set; } = 0;

        // Atributos de herencia lógica
        public string? UbicacionPred { get; set; } // debe referirse a una clase Ubicacion.
        public MetodoTransporte? MetodoTransportePred { get; set; } 
    }
}