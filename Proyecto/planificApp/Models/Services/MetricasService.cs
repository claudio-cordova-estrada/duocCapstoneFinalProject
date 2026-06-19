using planificApp.Models;
using PlanificApp.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace planificApp.Services;

public class MetricasService : IMetricasService
{
    // En el futuro, aquí inyectarás tu ITareaRepository

    public async Task<MetricasDto> ObtenerMetricasUsuarioAsync(Usuario usuario, int year)
    {
        // Simulación determinista de una consulta a Base de Datos.
        // Aseguramos que 1 usuario devuelva exactamente sus 15 tareas.
        await Task.Delay(10); // Simula el tiempo de red de MongoDB

        return new MetricasDto
        {
            TareasCreadas = 15,
            TareasCompletadas = 12,
            GeneracionesRealizadas = 3,
            GeneracionesModificadas = 0,
            TareasEnGeneracionSemanal = 0,
            TareasModificadasGeneracionSemanal = 5
        };
    }

    public async Task<MetricasDto> ObtenerMetricasGlobalesAsync(IEnumerable<Usuario> usuarios, int year)
    {
        await Task.Delay(10); // Simula el tiempo de red de MongoDB

        int totalUsuarios = usuarios.Count();
        int activos = usuarios.Count(u => u.EstaActivo);

        // Sumamos los datos base por la cantidad de usuarios
        return new MetricasDto
        {
            TotalUsuarios = totalUsuarios,
            UsuariosActivos = activos,
            UsanGeneracionSemanal = (int)(activos * 0.4),
            CambiosPorGeneracion = totalUsuarios > 0 ? 2.4 : 0.0,

            TareasCreadas = totalUsuarios * 15,
            TareasCompletadas = totalUsuarios * 12,
            GeneracionesRealizadas = totalUsuarios * 3,
            GeneracionesModificadas = 0,
            TareasEnGeneracionSemanal = 0,
            TareasModificadasGeneracionSemanal = totalUsuarios * 5
        };
    }
}