using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlanificApp.Models.Services.Interfaces
{
    public interface IRegionesService
    {
        // Ahora devuelven Task porque deben esperar a que internet responda
        Task<IEnumerable<string>> ObtenerRegionesAsync();
        Task<IEnumerable<string>> ObtenerComunasPorRegionAsync(string region);
    }
}