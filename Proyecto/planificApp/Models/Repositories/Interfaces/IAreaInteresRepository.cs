using System.Collections.Generic;
using System.Threading.Tasks;
using PlanificApp.Models;

namespace PlanificApp.Models.Repositories.Interfaces;

public interface IAreaInteresRepository
{
    Task<List<AreaInteres>> ObtenerAreasPorUsuario(string idUsuario);
    Task CrearAreaInteres(AreaInteres nuevaArea);
    Task ActualizarAreaInteres(string id, AreaInteres areaActualizada);
    Task EliminarAreaInteres(string id);
}