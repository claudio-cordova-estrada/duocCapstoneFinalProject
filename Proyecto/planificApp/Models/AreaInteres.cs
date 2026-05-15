using System;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PlanificApp.Models.Enums;

namespace PlanificApp.Models
{
    public class AreaInteres
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? IdAreaInteres { get; set; } // Estándar de MongoDB para IDs

        private string _nombre = string.Empty;
        public string Nombre
        {
            get => _nombre;
            set
            {
                // Validación: Solo letras, números y espacios para nombres de categorías
                if (Regex.IsMatch(value, @"^[a-zA-Z0-9áéíóúÁÉÍÓÚñÑ\s]+$") || string.IsNullOrEmpty(value))
                    _nombre = value;
                else
                    throw new ArgumentException("El nombre del área no puede contener caracteres especiales.");
            }
        }

        public bool GeneracionSemanal { get; set; } // Determina si el generador debe considerar esta área

        public PrioridadAreaInteres Prioridad { get; set; } = PrioridadAreaInteres.Predeterminado;

        public int HorasSemanales { get; set; } = 0; // Meta de tiempo para el algoritmo

        // Herencia lógica de preferencias para las tareas que pertenezcan a esta área
        public string? UbicacionPred { get; set; }
        public MetodoTransporte? MetodoTransportePred { get; set; }


        // Relación con el usuario dueño del área
        public string? IdUsuario { get; set; }

    }
}