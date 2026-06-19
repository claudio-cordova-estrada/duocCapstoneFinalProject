using planificApp.Models;
using PlanificApp.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace planificApp.Services;

public interface IMetricasService
{
    // Obtiene las métricas para un solo usuario (Vista de Detalles)
    Task<MetricasDto> ObtenerMetricasUsuarioAsync(Usuario usuario, int year);

    // Obtiene las métricas sumadas de un grupo de usuarios (Vista Global)
    Task<MetricasDto> ObtenerMetricasGlobalesAsync(IEnumerable<Usuario> usuarios, int year);
}