using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PlanificApp.Models;

public class BloqueAreaInteresSchedule
{
    [BsonId]
    public string? Id { get; set; }

    public string? IdAreaInteres { get; set; }
    public string? IdUsuario { get; set; }
    public DateTime Fecha { get; set; }
    public TimeSpan HoraInicio { get; set; }
    public TimeSpan HoraFin { get; set; }
}