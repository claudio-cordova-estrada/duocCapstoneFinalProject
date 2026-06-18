using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlanificApp.Models;

namespace PlanificApp.Models.Services.Interfaces;

public interface IGeneradorSemanalService
{
    Task<List<PropuestaGeneracion>> GenerarPropuestasAsync(CondicionesGeneracion condiciones);
}