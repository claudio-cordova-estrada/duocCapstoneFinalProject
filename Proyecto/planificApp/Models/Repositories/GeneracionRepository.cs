using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using PlanificApp.Models.Repositories.Interfaces;
using PlanificApp.Models.Services;

namespace PlanificApp.Models.Repositories;

public class GeneracionRepository : IGeneracionRepository
{
    private readonly IMongoCollection<Generacion> _generaciones;

    public GeneracionRepository(MongoContext context)
    {
        _generaciones = context.GetCollection<Generacion>("Generaciones");
    }

    public async Task Registrar(Generacion generacion) =>
        await _generaciones.InsertOneAsync(generacion);

    public async Task<List<Generacion>> ObtenerPorUsuario(string idUsuario) =>
        await _generaciones.Find(g => g.IdUsuario == idUsuario).ToListAsync();

    public async Task<List<Generacion>> ObtenerTodas() =>
        await _generaciones.Find(_ => true).ToListAsync();

    public async Task EliminarPorUsuario(string idUsuario) =>
        await _generaciones.DeleteManyAsync(g => g.IdUsuario == idUsuario);

    public async Task CrearEnLote(IEnumerable<Generacion> generaciones)
    {
        var lista = generaciones.ToList();
        if (lista.Count == 0) return;
        await _generaciones.InsertManyAsync(lista);
    }
}
