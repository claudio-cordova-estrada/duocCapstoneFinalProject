using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Platform; // NUEVO: Importamos la herramienta nativa de Avalonia
using PlanificApp.Models.Services.Interfaces;

namespace PlanificApp.Models.Services;

public class RegionesService : IRegionesService
{
    private class RegionData
    {
        public string Region { get; set; } = string.Empty;
        public List<string> Comunas { get; set; } = new();
    }

    private List<RegionData>? _cache;

    private async Task CargarDatosAsync()
    {
        if (_cache != null) return;

        try
        {
            // NUEVO: La forma nativa y segura de leer archivos en Avalonia
            var uri = new Uri("avares://planificApp/Assets/regiones_comunas.json");

            // Abrimos el archivo usando el AssetLoader
            using var stream = AssetLoader.Open(uri);
            using var reader = new StreamReader(stream);
            string json = await reader.ReadToEndAsync();

            _cache = JsonSerializer.Deserialize<List<RegionData>>(json) ?? new List<RegionData>();
        }
        catch (Exception ex)
        {
            // Si algo falla, lo imprimimos en la consola de Visual Studio para saber qué pasó
            System.Diagnostics.Debug.WriteLine($"Error leyendo JSON de regiones: {ex.Message}");
            _cache = new List<RegionData>();
        }
    }

    public async Task<IEnumerable<string>> ObtenerRegionesAsync()
    {
        await CargarDatosAsync();
        return _cache?.Select(r => r.Region) ?? Enumerable.Empty<string>();
    }

    public async Task<IEnumerable<string>> ObtenerComunasPorRegionAsync(string regionBuscada)
    {
        await CargarDatosAsync();
        var regionEncontrada = _cache?.FirstOrDefault(r => r.Region == regionBuscada);
        return regionEncontrada?.Comunas ?? Enumerable.Empty<string>();
    }
}