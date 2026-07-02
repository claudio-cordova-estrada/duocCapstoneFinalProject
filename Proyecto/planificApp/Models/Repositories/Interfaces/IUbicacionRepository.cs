using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlanificApp.Models.Repositories.Interfaces;

public interface IUbicacionRepository
{
    Task CrearUbicacion(UbicacionGuardada nuevaUbicacion);
    Task<List<UbicacionGuardada>> ObtenerUbicacionesPorUsuario(string idUsuario);
    Task ActualizarUbicacion(string id, UbicacionGuardada ubicacionActualizada);
    Task EliminarUbicacion(string id);
}