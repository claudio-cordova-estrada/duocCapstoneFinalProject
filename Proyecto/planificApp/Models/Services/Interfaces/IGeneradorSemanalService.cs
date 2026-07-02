using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlanificApp.Models.Services.Interfaces;

public interface IGeneradorSemanalService
{
    Task<List<PropuestaGeneracion>> GenerarPropuestasAsync(CondicionesGeneracion condiciones);
}