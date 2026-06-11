using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlanificApp.Models.Services.Interfaces;

public interface IRegionesService
{
    Task<IEnumerable<string>> ObtenerRegionesAsync();
    Task<IEnumerable<string>> ObtenerComunasPorRegionAsync(string region);
}