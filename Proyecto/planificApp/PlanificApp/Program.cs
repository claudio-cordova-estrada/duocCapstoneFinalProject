using Avalonia;
using Avalonia.ReactiveUI;
using System;

namespace PlanificApp
{
    class Program
    {
        // El punto de entrada principal de la aplicación
        [STAThread]
        public static void Main(string[] args) => BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

        // Configuración de Avalonia, necesaria para que el diseñador visual funcione
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace()
                .UseReactiveUI();
    }
}