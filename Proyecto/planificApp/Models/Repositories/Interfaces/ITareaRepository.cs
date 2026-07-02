using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlanificApp.Models.Repositories.Interfaces;

public interface ITareaRepository
{
    Task CrearTarea(Tarea nuevaTarea);
    Task<List<Tarea>> ObtenerTareasPorUsuario(string idUsuario);
    Task<List<Tarea>> ObtenerTareasPorFecha(string idUsuario, DateTime fecha);
    Task<List<Tarea>> ObtenerTareasPorRango(string idUsuario, DateTime desde, DateTime hasta);
    Task<List<Tarea>> ObtenerTareasActivas(string idUsuario);
    Task<List<Tarea>> ObtenerTareasCompletadas(string idUsuario);
    Task<List<Tarea>> ObtenerTareasVencidas(string idUsuario);
    Task<List<Tarea>> ObtenerTareasPorArea(string areaId);
    Task<Tarea?> ObtenerTareaPorId(string idTarea);
    Task CompletarTarea(string idTarea);
    Task DescompletarTarea(string idTarea);
    Task ActualizarTarea(string id, Tarea tareaActualizada);
    Task EliminarTarea(string id);
}