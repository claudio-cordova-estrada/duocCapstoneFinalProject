using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlanificApp.Models.Repositories.Interfaces;

public interface IBloqueAreaInteresScheduleRepository
{
    Task<List<BloqueAreaInteresSchedule>> ObtenerPorUsuarioYRango(string idUsuario, DateTime desde, DateTime hasta);
    Task CrearBloque(BloqueAreaInteresSchedule bloque);
    Task ActualizarBloque(string id, BloqueAreaInteresSchedule bloque);
    Task EliminarBloque(string id);
    Task EliminarBloquesPorDia(string idUsuario, DateTime fecha);
    Task EliminarBloquesPorSemana(string idUsuario, DateTime desde, DateTime hasta);
}