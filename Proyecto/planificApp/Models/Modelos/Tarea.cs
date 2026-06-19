using System;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PlanificApp.Models.Enums;

namespace PlanificApp.Models
{
    public class Tarea
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? IdTarea { get; set; }

        private string _nombre = string.Empty;
        public string Nombre
        {
            get => _nombre;
            set => _nombre = value;
        }

        public void Validate()
        {
            if (string.IsNullOrEmpty(Nombre))
                throw new ArgumentException("El nombre de la tarea no puede estar vacío.");
            if (Nombre.Length > 500)
                throw new ArgumentException("El nombre de la tarea no puede superar los 500 caracteres.");
            if (!Regex.IsMatch(Nombre, @"^[a-zA-Z0-9áéíóúÁÉÍÓÚñÑ\s\-\.]+$"))
                throw new ArgumentException("El nombre de la tarea contiene caracteres no permitidos.");
        }

        public DateTime? FecLimite { get; set; }
        public DateTime? FecInicio { get; set; }
        public TimeSpan? HoraInicio { get; set; }
        public TimeSpan? HoraFin { get; set; }
        public string? Ubicacion { get; set; }
        public MetodoTransporte? MetodoTransporte { get; set; }
        public int TiempoEstimado { get; set; }
        public string? IdAreaInteres { get; set; }
        public DateTime? Recordatorio { get; set; }

        public int Prioridad { get; set; } = 1;

        public bool? UsoGeneracion { get; set; }
        public bool ModificacionGeneracion { get; set; }
        public bool CompletadoEnTiempo { get; set; }

        public DateTime FecCreacion { get; set; }
        public DateTime? FecCompletado { get; set; }

        public string? IdUsuario { get; set; }
        
        public string? TipoActividadFisica { get; set; }
        public string? TipoActividadMental { get; set; }

        public string? IdUbicacion { get; set; }

    }
}
