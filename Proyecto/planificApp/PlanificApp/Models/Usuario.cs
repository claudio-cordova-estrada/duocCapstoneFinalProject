using System;
using MongoDB.Bson.Serialization.Attributes;

namespace PlanificApp.Models
{
    public class Usuario
    {
        [BsonId] // averiguar que es
        public int IdUsuario { get; set; } // int(10)
        public string NombreCompleto { get; set; }  // establecer validacion de caracteres numéricos y especiales.
        public string Correo { get; set; } // establecer validacion de correspondientes a email valido.
        public DateTime FecNacimiento { get; set; }
        public Ubicacion Region { get; set; }  // crear clase Region para validar que sea una region valida de chile.
        public Ubicacion Comuna { get; set; }  // crear clase Comuna para validar que sea una comuna valida de chile.

        // Horas funcionales
        public TimeSpan HoraInicioJornada { get; set; }
        public TimeSpan HoraFinJornada { get; set; }
    }
}