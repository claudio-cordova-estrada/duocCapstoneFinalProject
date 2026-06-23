using System;

namespace PlanificApp.Models.Services.Interfaces;

public enum AppTheme
{
    Dark,
    Light
}

/// <summary>
/// Maneja las preferencias de apariencia de la app (tema claro/oscuro y escala de fuente),
/// las aplica en vivo y las persiste localmente.
/// </summary>
public interface IAppearanceService
{
    AppTheme Theme { get; }
    double FontScale { get; }

    /// <summary>Se dispara cuando el tema cambió (para animar la transición en la vista).</summary>
    event Action? ThemeChanged;

    void SetTheme(AppTheme theme);
    void SetFontScale(double scale);

    /// <summary>Aplica las preferencias guardadas al arrancar la app.</summary>
    void ApplyOnStartup();
}
