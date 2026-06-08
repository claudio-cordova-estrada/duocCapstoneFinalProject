using Microsoft.Extensions.Configuration;
using PlanificApp.Models;
using PlanificApp.Models.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace PlanificApp.Models.Services
{
    public class GeoService : IGeoService
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
        public async Task<string> CalcularTiempoTrasladoAsync(double latOrigen, double lonOrigen, double latDestino, double lonDestino, string transporteApp)
        {
            // 1. Traducimos tu transporte al formato de Google
            string mode = "driving"; // Auto y Taxi por defecto
            if (transporteApp == "A pie") mode = "walking";
            else if (transporteApp == "Bicicleta") mode = "bicycling";
            else if (transporteApp == "Metro" || transporteApp == "Bus") mode = "transit";

            // 2. Armamos la URL para la Distance Matrix API (en español para que devuelva "15 min" o "2 horas")
            var url = $"https://maps.googleapis.com/maps/api/distancematrix/json?origins={latOrigen.ToString(System.Globalization.CultureInfo.InvariantCulture)},{lonOrigen.ToString(System.Globalization.CultureInfo.InvariantCulture)}&destinations={latDestino.ToString(System.Globalization.CultureInfo.InvariantCulture)},{lonDestino.ToString(System.Globalization.CultureInfo.InvariantCulture)}&mode={mode}&key={_apiKey}&language=es";

            try
            {
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (root.GetProperty("status").GetString() == "OK")
                    {
                        var rows = root.GetProperty("rows");
                        if (rows.GetArrayLength() > 0)
                        {
                            var elements = rows[0].GetProperty("elements");
                            if (elements.GetArrayLength() > 0)
                            {
                                var element = elements[0];
                                if (element.GetProperty("status").GetString() == "OK")
                                {
                                    // Extraemos el texto del tiempo (Ej: "14 min")
                                    return element.GetProperty("duration").GetProperty("text").GetString() ?? "Desconocido";
                                }
                            }
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[GEO_SERVICE] Error Matrix: {root.GetProperty("status").GetString()}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GEO_SERVICE] Excepción al calcular ruta: {ex.Message}");
            }

            return "No disponible";
        }
        // 1. Método para obtener la ruta y el tiempo
        public async Task<(string Tiempo, List<(double Latitud, double Longitud)> Ruta)> CalcularRutaConTrazadoAsync(double latOrigen, double lonOrigen, double latDestino, double lonDestino, string transporteApp)
        {
            string mode = "driving";
            if (transporteApp == "A pie") mode = "walking";
            else if (transporteApp == "Bicicleta") mode = "bicycling";
            else if (transporteApp == "Metro" || transporteApp == "Bus") mode = "transit";

            // Usamos Directions API en lugar de Matrix API para obtener la geometría de la ruta
            var url = $"https://maps.googleapis.com/maps/api/directions/json?origin={latOrigen.ToString(System.Globalization.CultureInfo.InvariantCulture)},{lonOrigen.ToString(System.Globalization.CultureInfo.InvariantCulture)}&destination={latDestino.ToString(System.Globalization.CultureInfo.InvariantCulture)},{lonDestino.ToString(System.Globalization.CultureInfo.InvariantCulture)}&mode={mode}&key={_apiKey}&language=es";

            try
            {
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (root.GetProperty("status").GetString() == "OK")
                    {
                        var route = root.GetProperty("routes")[0];
                        var leg = route.GetProperty("legs")[0];

                        string tiempo = leg.GetProperty("duration").GetProperty("text").GetString() ?? "Desconocido";
                        string polyline = route.GetProperty("overview_polyline").GetProperty("points").GetString() ?? "";

                        return (tiempo, DecodificarPolyline(polyline));
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GEO_SERVICE] Error ruta: {ex.Message}");
            }
            return ("No disponible", new List<(double, double)>());
        }

        // 2. Método para Reverse Geocoding (Clic en el mapa)
        public async Task<string> ObtenerDireccionDesdeCoordenadasAsync(double lat, double lon)
        {
            var url = $"https://maps.googleapis.com/maps/api/geocode/json?latlng={lat.ToString(System.Globalization.CultureInfo.InvariantCulture)},{lon.ToString(System.Globalization.CultureInfo.InvariantCulture)}&key={_apiKey}&language=es";
            try
            {
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    if (root.GetProperty("status").GetString() == "OK")
                    {
                        return root.GetProperty("results")[0].GetProperty("formatted_address").GetString() ?? "Dirección desconocida";
                    }
                }
            }
            catch { }
            return "Dirección desconocida";
        }

        // 3. Algoritmo estándar de Google para decodificar trazados
        private List<(double Latitud, double Longitud)> DecodificarPolyline(string polyline)
        {
            var points = new List<(double Latitud, double Longitud)>();
            int index = 0, len = polyline.Length;
            int lat = 0, lng = 0;
            while (index < len)
            {
                int b, shift = 0, result = 0;
                do { b = polyline[index++] - 63; result |= (b & 0x1f) << shift; shift += 5; } while (b >= 0x20);
                int dlat = ((result & 1) != 0 ? ~(result >> 1) : (result >> 1));
                lat += dlat;
                shift = 0; result = 0;
                do { b = polyline[index++] - 63; result |= (b & 0x1f) << shift; shift += 5; } while (b >= 0x20);
                int dlng = ((result & 1) != 0 ? ~(result >> 1) : (result >> 1));
                lng += dlng;
                points.Add((lat / 1E5, lng / 1E5));
            }
            return points;
        }
    }
}