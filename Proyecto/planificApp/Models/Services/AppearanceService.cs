using System;
using System.IO;
using System.Text.Json;
using Avalonia;
using Avalonia.Styling;
using PlanificApp.Models.Services.Interfaces;

namespace PlanificApp.Models.Services;

public class AppearanceService : IAppearanceService
{
    public const double MinScale = 0.8;
    public const double MaxScale = 1.4;

    // Factor base: el "100%" del usuario equivale a este zoom real (antes el cómodo era 120%).
    // Slider 0.8–1.4 (80%–140%) → zoom real 0.96–1.68, con default 100% = 1.2.
    private const double BaseScale = 1.2;

    private readonly string _filePath;
    private readonly Preferences _prefs;

    public AppearanceService()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "planificApp");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "preferences.json");
        _prefs = Cargar();
    }

    public event Action? ThemeChanged;

    public AppTheme Theme => _prefs.Theme;
    public double FontScale => _prefs.FontScale;

    public void SetTheme(AppTheme theme)
    {
        if (_prefs.Theme == theme) return;
        _prefs.Theme = theme;
        AplicarTema();
        Guardar();
        ThemeChanged?.Invoke();
    }

    public void SetFontScale(double scale)
    {
        _prefs.FontScale = Math.Clamp(scale, MinScale, MaxScale);
        AplicarFontScale();
        Guardar();
    }

    public void ApplyOnStartup()
    {
        AplicarTema();
        AplicarFontScale();
    }

    private void AplicarFontScale()
    {
        if (Application.Current is { } app)
            app.Resources["AppFontScale"] = _prefs.FontScale * BaseScale;
    }

    private void AplicarTema()
    {
        if (Application.Current is { } app)
            app.RequestedThemeVariant = _prefs.Theme == AppTheme.Light ? ThemeVariant.Light : ThemeVariant.Dark;
    }

    private Preferences Cargar()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var p = JsonSerializer.Deserialize<Preferences>(File.ReadAllText(_filePath));
                if (p != null)
                {
                    p.FontScale = Math.Clamp(p.FontScale, MinScale, MaxScale);
                    return p;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[APPEARANCE] Error al cargar preferencias: {ex.Message}");
        }
        return new Preferences();
    }

    private void Guardar()
    {
        try
        {
            var json = JsonSerializer.Serialize(_prefs, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[APPEARANCE] Error al guardar preferencias: {ex.Message}");
        }
    }

    private class Preferences
    {
        public AppTheme Theme { get; set; } = AppTheme.Dark;
        public double FontScale { get; set; } = 1.0;
    }
}
