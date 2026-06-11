using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using PlanificApp.Models.Services.Interfaces;

namespace PlanificApp.Models.Services
{
    public class RegionesService : IRegionesService
    {
        private readonly HttpClient _httpClient;

        // Usaremos una de las APIs públicas más comunes de Chile para DPA
        private const string ApiUrl = "https://apis.digital.gob.cl/dpa/regiones";

        public RegionesService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<IEnumerable<string>> ObtenerRegionesAsync()
        {
            try
            {
                var response = await _httpClient.GetStringAsync(ApiUrl);
                var regiones = JsonSerializer.Deserialize<List<RegionDpa>>(response, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return regiones?.Select(r => r.Nombre) ?? Enumerable.Empty<string>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al conectar con API: {ex.Message}");
                return Enumerable.Empty<string>();
            }
        }

        public async Task<IEnumerable<string>> ObtenerComunasPorRegionAsync(string nombreRegion)
        {
            // Nota: Para esta API específica del gobierno, primero debes buscar el código de la región 
            // y luego hacer GET a /regiones/{codigo}/comunas. 
            // (La lógica aquí dependerá exactamente de la API pública que decidas usar).

            // Para simplificar, si la API te entregara todo de una vez:
            return new List<string> { "Comuna 1", "Comuna 2 de la API" };
        }

        // Clases auxiliares para leer el JSON de la API
        private class RegionDpa
        {
            public string Codigo { get; set; } = string.Empty;
            public string Nombre { get; set; } = string.Empty;
        }
    }
}