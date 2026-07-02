using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using PlanificApp.Models.Repositories.Interfaces;
using PlanificApp.Models.Services;

namespace PlanificApp.Models.Repositories;

public class BloqueAreaInteresScheduleRepository : IBloqueAreaInteresScheduleRepository
{
    private readonly IMongoCollection<BloqueAreaInteresSchedule> _bloques;

    public BloqueAreaInteresScheduleRepository(MongoContext context)
    {
        _bloques = context.GetCollection<BloqueAreaInteresSchedule>("BloquesAreaInteres");
    }

    public async Task<List<BloqueAreaInteresSchedule>> ObtenerPorUsuarioYRango(string idUsuario, DateTime desde, DateTime hasta) =>
        await _bloques.Find(b => b.IdUsuario == idUsuario && b.Fecha >= desde && b.Fecha < hasta).ToListAsync();

    public async Task CrearBloque(BloqueAreaInteresSchedule bloque) =>
        await _bloques.InsertOneAsync(bloque);

    public async Task ActualizarBloque(string id, BloqueAreaInteresSchedule bloque) =>
        await _bloques.ReplaceOneAsync(b => b.Id == id, bloque);

    public async Task EliminarBloque(string id) =>
        await _bloques.DeleteOneAsync(b => b.Id == id);

    public async Task EliminarBloquesPorDia(string idUsuario, DateTime fecha)
    {
        var inicioDia = fecha.Date;
        var finDia = fecha.Date.AddDays(1);
        await _bloques.DeleteManyAsync(b => b.IdUsuario == idUsuario && b.Fecha >= inicioDia && b.Fecha < finDia);
    }

    public async Task EliminarBloquesPorSemana(string idUsuario, DateTime desde, DateTime hasta)
    {
        await _bloques.DeleteManyAsync(b => b.IdUsuario == idUsuario && b.Fecha >= desde && b.Fecha < hasta);
    }
}