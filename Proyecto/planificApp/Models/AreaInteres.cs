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
        public string? IdAreaInteres { get; set; }

        private string _nombre = string.Empty;
        public string Nombre
        {
            get => _nombre;
            set => _nombre = value;
        }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Nombre))
                throw new ArgumentException("El nombre del área no puede estar vacío.");
            if (!Regex.IsMatch(Nombre, @"^[a-zA-Z0-9áéíóúÁÉÍÓÚñÑ\s]+$"))
                throw new ArgumentException("El nombre del área no puede contener caracteres especiales.");
        }

        public bool GeneracionSemanal { get; set; } // Determina si el generador debe considerar esta área

        public PrioridadAreaInteres Prioridad { get; set; } = PrioridadAreaInteres.Predeterminado;

        public int HorasSemanales { get; set; } = 0; // Meta de tiempo para el algoritmo de generacion semanal

        // Herencia lógica de preferencias para las tareas que pertenezcan a esta área
        public string? UbicacionPred { get; set; }
        public MetodoTransporte? MetodoTransportePred { get; set; }
        public string? TipoActividadFisicaPred { get; set; }
        public string? TipoActividadMentalPred { get; set; }
        
        public string? IdUsuario { get; set; }
        
        // Violeta por defecto
        public string ColorHex { get; set; } = "#a78bfa"; 

    }
}