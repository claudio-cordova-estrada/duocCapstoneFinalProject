using System;
using MongoDB.Bson.Serialization.Attributes;
using PlanificApp.Models.Enums;

namespace PlanificApp.Models
{
    public class Tarea
    {
        [BsonId]
        public int IdTarea { get; set; } // int(15)
        public string Nombre { get; set; } = string.Empty; // necesita logica de validacion de caracteres numéricos y especiales.

        public DateTime? FecLimite { get; set; }
        public DateTime? FecInicio { get; set; }
        public TimeSpan? HoraInicio { get; set; }
        public TimeSpan? HoraFin { get; set; }

        public Ubicacion? Ubicacion { get; set; }
        public MetodoTransporte? MetodoTransporte { get; set; }

        public int? TiempoEstimadoTransporte { get; set; } // hace referencia a los minutos 

        public int? IdAreaInteres { get; set; } // establecer el id para obtenerlo desde la clase AreaInteres.
        public TimeSpan? Recordatorio { get; set; } // deberia ser clase aparte(?

        public PrioridadTarea Prioridad { get; set; } // int(1)
        public bool UsoGeneracion { get; set; }
        public bool ModificacionGeneracion { get; set; }

        public bool CompletadoEnTiempo { get; set; }
        public DateTime FecCreacion { get; set; } = DateTime.Now;
        public DateTime? FecCompletado { get; set; }
    }
}