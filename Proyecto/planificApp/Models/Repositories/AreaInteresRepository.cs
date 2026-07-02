using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using PlanificApp.Models.Repositories.Interfaces;
using PlanificApp.Models.Services;

namespace PlanificApp.Models.Repositories;

public class AreaInteresRepository : IAreaInteresRepository
{
    private readonly IMongoCollection<AreaInteres> _areas;

    public AreaInteresRepository(MongoContext context)
    {
        _areas = context.GetCollection<AreaInteres>("AreasInteres");
    }

    public async Task<List<AreaInteres>> ObtenerAreasPorUsuario(string idUsuario) =>
        await _areas.Find(a => a.IdUsuario == idUsuario).ToListAsync();

    public async Task CrearAreaInteres(AreaInteres nuevaArea) =>
        await _areas.InsertOneAsync(nuevaArea);

    public async Task ActualizarAreaInteres(string id, AreaInteres areaActualizada) =>
        await _areas.ReplaceOneAsync(a => a.IdAreaInteres == id, areaActualizada);

    public async Task EliminarAreaInteres(string id) =>
        await _areas.DeleteOneAsync(a => a.IdAreaInteres == id);
}