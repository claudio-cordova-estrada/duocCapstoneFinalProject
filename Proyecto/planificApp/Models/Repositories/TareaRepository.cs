using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using PlanificApp.Models.Repositories.Interfaces;
using PlanificApp.Models.Services;

namespace PlanificApp.Models.Repositories;

public class TareaRepository : ITareaRepository
{
    private readonly IMongoCollection<Tarea> _tareas;

    public TareaRepository(MongoContext context)
    {
        _tareas = context.GetCollection<Tarea>("Tareas");
    }

    public async Task CrearTarea(Tarea nuevaTarea) =>
        await _tareas.InsertOneAsync(nuevaTarea);

    public async Task<List<Tarea>> ObtenerTareasPorUsuario(string idUsuario) =>
        await _tareas.Find(t => t.IdUsuario == idUsuario).ToListAsync();

    public async Task<List<Tarea>> ObtenerTareasPorFecha(string idUsuario, DateTime fecha)
    {
        var inicioDia = fecha.Date;
        var finDia = fecha.Date.AddDays(1);
        return await _tareas.Find(t =>
            t.IdUsuario == idUsuario &&
            t.FecCompletado == null &&
            (t.FecInicio >= inicioDia && t.FecInicio < finDia ||
             t.FecLimite >= inicioDia && t.FecLimite < finDia)).ToListAsync();
    }

    public async Task<List<Tarea>> ObtenerTareasPorRango(string idUsuario, DateTime desde, DateTime hasta) =>
        await _tareas.Find(t =>
            t.IdUsuario == idUsuario &&
            (t.FecInicio >= desde && t.FecInicio < hasta ||
             t.FecLimite >= desde && t.FecLimite < hasta ||
             t.FecCompletado >= desde && t.FecCompletado < hasta)).ToListAsync();

    public async Task<List<Tarea>> ObtenerTareasActivas(string idUsuario) =>
        await _tareas.Find(t => t.IdUsuario == idUsuario && t.FecCompletado == null).ToListAsync();

    public async Task<List<Tarea>> ObtenerTareasCompletadas(string idUsuario) =>
        await _tareas.Find(t => t.IdUsuario == idUsuario && t.FecCompletado != null).ToListAsync();

    public async Task<List<Tarea>> ObtenerTareasVencidas(string idUsuario)
    {
        var ahora = DateTime.Now;
        return await _tareas.Find(t =>
            t.IdUsuario == idUsuario &&
            t.FecCompletado == null &&
            t.FecLimite < ahora).ToListAsync();
    }

    public async Task<List<Tarea>> ObtenerTareasPorArea(string areaId) =>
        await _tareas.Find(t => t.IdAreaInteres == areaId).ToListAsync();

    public async Task<Tarea?> ObtenerTareaPorId(string idTarea) =>
        await _tareas.Find(t => t.IdTarea == idTarea).FirstOrDefaultAsync();

    public async Task CompletarTarea(string idTarea)
    {
        var filter = Builders<Tarea>.Filter.Eq(t => t.IdTarea, idTarea);
        var tarea = await _tareas.Find(filter).FirstOrDefaultAsync();
        if (tarea == null) return;
        tarea.FecCompletado = DateTime.Now;
        tarea.CompletadoEnTiempo = tarea.FecLimite == null || tarea.FecCompletado <= tarea.FecLimite;
        await _tareas.ReplaceOneAsync(filter, tarea);
    }

    public async Task DescompletarTarea(string idTarea)
    {
        var filter = Builders<Tarea>.Filter.Eq(t => t.IdTarea, idTarea);
        var tarea = await _tareas.Find(filter).FirstOrDefaultAsync();
        if (tarea == null) return;
        tarea.FecCompletado = null;
        tarea.CompletadoEnTiempo = false;
        await _tareas.ReplaceOneAsync(filter, tarea);
    }

    public async Task ActualizarTarea(string id, Tarea tareaActualizada) =>
        await _tareas.ReplaceOneAsync(t => t.IdTarea == id, tareaActualizada);

    public async Task EliminarTarea(string id) =>
        await _tareas.DeleteOneAsync(t => t.IdTarea == id);

    public async Task EliminarTareasPorUsuario(string idUsuario) =>
        await _tareas.DeleteManyAsync(t => t.IdUsuario == idUsuario);

    public async Task CrearTareasEnLote(IEnumerable<Tarea> tareas)
    {
        var lista = tareas.ToList();
        if (lista.Count == 0) return;
        await _tareas.InsertManyAsync(lista);
    }

    public async Task<List<Tarea>> ObtenerTodasLasTareas() =>
        await _tareas.Find(_ => true).ToListAsync();
}