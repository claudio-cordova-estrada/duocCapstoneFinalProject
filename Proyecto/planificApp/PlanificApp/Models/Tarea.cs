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
        public string? IdTarea { get; set; } // Consistente con el estándar de MongoDB

        private string _nombre = string.Empty;
        public string Nombre
        {
            get => _nombre;
            set
            {
                // Validación para evitar caracteres especiales dañinos en el nombre de la tarea
                if (Regex.IsMatch(value, @"^[a-zA-Z0-9áéíóúÁÉÍÓÚñÑ\s\-\.]+$") || string.IsNullOrEmpty(value))
                    _nombre = value;
                else
                    throw new ArgumentException("El nombre de la tarea contiene caracteres no permitidos.");
            }
        }

        public DateTime? FecLimite { get; set; }
        public PrioridadTarea Prioridad { get; set; } = PrioridadTarea.Baja;

        // ID de AreaInteres como string para coincidir con el nuevo estándar
        public string? IdAreaInteres { get; set; }
    }
}