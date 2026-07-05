using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlanificApp.Models.Repositories.Interfaces;

public interface IGeneracionRepository
{
    Task Registrar(Generacion generacion);
    Task<List<Generacion>> ObtenerPorUsuario(string idUsuario);
    Task<List<Generacion>> ObtenerTodas();
    Task EliminarPorUsuario(string idUsuario);
    Task CrearEnLote(IEnumerable<Generacion> generaciones);
}
