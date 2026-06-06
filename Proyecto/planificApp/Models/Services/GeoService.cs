using Microsoft.Extensions.Configuration;
using PlanificApp.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace PlanificApp.Services
{
    public class GeoService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        // Inyectamos la configuración para leer el appsettings.json
        public GeoService(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _apiKey = configuration["GoogleMaps:ApiKey"]
                      ?? throw new ArgumentNullException("No se encontró la API Key de Google");
        }


        /// <summary>
        /// Valida una dirección en texto y devuelve la ubicación con Latitud y Longitud reales.
        /// </summary>
        public async Task<Ubicacion?> ValidarDireccionAsync(string nombreLugar, string direccionCompleta)
        {
            // Formateamos la URL para la Geocoding API
            var url = $"https://maps.googleapis.com/maps/api/geocode/json?address={Uri.EscapeDataString(direccionCompleta)}&key={_apiKey}";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            var jsonString = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(jsonString);
            var root = document.RootElement;

            // Validamos que Google haya encontrado resultados
            if (root.GetProperty("status").GetString() != "OK")
            {
                return null; // La dirección no existe o es inválida
            }

            // Navegamos por el JSON para extraer las coordenadas exactas
            var location = root.GetProperty("results")[0]
                               .GetProperty("geometry")
                               .GetProperty("location");

            double lat = location.GetProperty("lat").GetDouble();
            double lng = location.GetProperty("lng").GetDouble();

            // Usamos tu modelo Record
            return new Ubicacion(nombreLugar, lat, lng);
        }

        public async Task<List<string>> GetPredictionsAsync(string input)
        {
            var resultados = new List<string>();
            if (string.IsNullOrWhiteSpace(input)) return resultados;

            var url = $"https://maps.googleapis.com/maps/api/place/autocomplete/json?input={Uri.EscapeDataString(input)}&key={_apiKey}&language=es&components=country:cl";

            try
            {
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    var status = root.GetProperty("status").GetString();

                    if (status == "OK")
                    {
                        var predictions = root.GetProperty("predictions");
                        foreach (var p in predictions.EnumerateArray())
                        {
                            var direccionTexto = p.GetProperty("description").GetString();
                            if (!string.IsNullOrEmpty(direccionTexto))
                            {
                                resultados.Add(direccionTexto);
                            }
                        }
                    }
                    else
                    {
                        // ¡AQUÍ ESTÁ LA MAGIA! Si Google falla, nos dirá por qué.
                        System.Diagnostics.Debug.WriteLine($"[GEO_SERVICE] Google rechazó la petición. Estado: {status}");
                        if (root.TryGetProperty("error_message", out var errorMsg))
                        {
                            System.Diagnostics.Debug.WriteLine($"[GEO_SERVICE] Detalle del error: {errorMsg.GetString()}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GEO_SERVICE] Error de red o código: {ex.Message}");
            }

            return resultados;
        }
    }
}