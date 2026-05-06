using System;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PlanificApp.Models
{
    public class Usuario
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? IdUsuario { get; set; } // Identificador estándar de MongoDB

        private string _nombreCompleto = string.Empty;
        public string NombreCompleto
        {
            get => _nombreCompleto;
            set
            {
                // Validación: Solo letras y espacios. Rechaza números y símbolos
                if (Regex.IsMatch(value, @"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$") || string.IsNullOrEmpty(value))
                    _nombreCompleto = value;
                else
                    throw new ArgumentException("El nombre no es válido.");
            }
        }

        public string Correo { get; set; } = string.Empty;

        // Integración de ubicación administrativa
        public Region? Region { get; set; }
        public Comuna? Comuna { get; set; }
        public TimeSpan HoraInicioJornada { get; set; }
        public TimeSpan HoraFinJornada { get; set; }
    }
}