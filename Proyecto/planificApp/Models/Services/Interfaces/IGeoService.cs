using System.Collections.Generic;
using System.Threading.Tasks;
using PlanificApp.Models;

namespace PlanificApp.Models.Services.Interfaces;

public interface IGeoService
{
    Task<Ubicacion?> ValidarDireccionAsync(string nombreLugar, string direccionCompleta);
    Task<List<string>> GetPredictionsAsync(string input);
    Task<string> CalcularTiempoTrasladoAsync(double latOrigen, double lonOrigen, double latDestino, double lonDestino, string transporteApp);
    Task<(string Tiempo, List<(double Latitud, double Longitud)> Ruta)> CalcularRutaConTrazadoAsync(double latOrigen, double lonOrigen, double latDestino, double lonDestino, string transporteApp);
    Task<string> ObtenerDireccionDesdeCoordenadasAsync(double lat, double lon);
}