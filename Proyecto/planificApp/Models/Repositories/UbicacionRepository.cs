using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using PlanificApp.Models.Repositories.Interfaces;
using PlanificApp.Models.Services;

namespace PlanificApp.Models.Repositories;

public class UbicacionRepository : IUbicacionRepository
{
    private readonly IMongoCollection<UbicacionGuardada> _ubicaciones;

    public UbicacionRepository(MongoContext context)
    {
        _ubicaciones = context.GetCollection<UbicacionGuardada>("Ubicaciones");
    }

    public async Task CrearUbicacion(UbicacionGuardada nuevaUbicacion) =>
        await _ubicaciones.InsertOneAsync(nuevaUbicacion);

    public async Task<List<UbicacionGuardada>> ObtenerUbicacionesPorUsuario(string idUsuario) =>
        await _ubicaciones.Find(u => u.IdUsuario == idUsuario).ToListAsync();

    public async Task ActualizarUbicacion(string id, UbicacionGuardada ubicacionActualizada) =>
        await _ubicaciones.ReplaceOneAsync(u => u.IdUbicacion == id, ubicacionActualizada);

    public async Task EliminarUbicacion(string id) =>
        await _ubicaciones.DeleteOneAsync(u => u.IdUbicacion == id);

    public async Task EliminarUbicacionesPorUsuario(string idUsuario) =>
        await _ubicaciones.DeleteManyAsync(u => u.IdUsuario == idUsuario);
}