using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PlanificApp.Models.Enums;



namespace PlanificApp.Models
{
    [BsonIgnoreExtraElements]
    public class Usuario
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? IdUsuario { get; set; }

        private string _nombreCompleto = string.Empty;
        public string NombreCompleto
        {
            get => _nombreCompleto;
            set => _nombreCompleto = value;
        }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(NombreCompleto) || NombreCompleto.Length < 3)
                throw new ArgumentException("El nombre debe tener al menos 3 caracteres.");
            if (!Regex.IsMatch(NombreCompleto, @"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$"))
                throw new ArgumentException("El nombre solo puede contener letras y espacios.");
        }

        public string Correo { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string RespuestaSeguridad { get; set; } = string.Empty;

        public Region? Region { get; set; }
        public Comuna? Comuna { get; set; }
        public TimeSpan HoraInicioJornada { get; set; } = TimeSpan.FromHours(7.5);
        public TimeSpan HoraFinJornada { get; set; } = TimeSpan.FromHours(22);

        public bool CuentaConfirmada { get; set; }
        public string? TokenConfirmacion { get; set; }

        public DateTime FecCreacion { get; set; }
        public DateTime FecNacimiento { get; set; }

        public string Ubicacion { get; set; } = string.Empty;

        public string? UbicacionActual { get; set; }

        public MetodoTransporte? TransportePred { get; set; }
        public double HorasSueno { get; set; } = 7.5;
        public List<DayOfWeek> DiasGeneracionSemanal { get; set; } = new() { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };

        public string? FotoPerfil { get; set; }

        public bool EstaActivo { get; set; } = true;
    }
}