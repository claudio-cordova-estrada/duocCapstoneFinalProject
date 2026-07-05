using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PlanificApp.Models
{
    // Registro (log) de cada generación semanal que un usuario acepta. Sirve para métricas reales:
    // conteo de generaciones por usuario/mes y el objetivo de "uso continuo del motor de generación".
    public class Generacion
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string IdUsuario { get; set; } = string.Empty;

        // Momento en que se aceptó la generación (para métricas por mes/año).
        public DateTime FecGeneracion { get; set; }

        // Lunes de la semana que se generó.
        public DateTime FechaSemana { get; set; }

        public string Estrategia { get; set; } = string.Empty; // Equilibrio / Intensiva / Relajado
        public int TotalBloques { get; set; }
        public int TotalTareas { get; set; }
    }
}
