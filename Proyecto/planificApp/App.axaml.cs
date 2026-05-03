using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using planificApp.Data;
using planificApp.Factories;
using planificApp.ViewModels;

namespace planificApp;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        DataTemplates.Add(new ViewLocator());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var collection = new ServiceCollection();
        collection.AddSingleton<MainViewModel>();
        
        collection.AddTransient<InboxViewModel>();
        collection.AddTransient<CalendarioSemanalViewModel>();
        collection.AddTransient<TodayTaskViewModel>();
        collection.AddTransient<SettingsViewModel>();
        collection.AddTransient<AreaInteresViewModel>();
        collection.AddTransient<LocationViewModel>();
        collection.AddTransient<CalendarioMensualViewModel>();
        collection.AddTransient<SugerenciasSemanalesViewModel>();
        collection.AddTransient<CuentaViewModel>();
        collection.AddTransient<SoporteViewModel>();
        collection.AddTransient<AboutViewModel>();
        collection.AddTransient<EstadisticaUsuarioViewModel>();
        collection.AddTransient<ModificacionUsuarioViewModel>();
        collection.AddTransient<BusquedaUsuarioViewModel>();
        collection.AddTransient<DetalleUsuarioViewModel>();
        collection.AddTransient<LoginViewModel>();
        collection.AddTransient<RegistroViewModel>();
        collection.AddTransient<RecuperarContraViewModel>();

        collection.AddSingleton<Func<ApplicationPageNames, PageViewModel>>(x => name => name switch
        {
            ApplicationPageNames.Inbox => x.GetRequiredService<InboxViewModel>(),
            ApplicationPageNames.TodayTask => x.GetRequiredService<TodayTaskViewModel>(),
            ApplicationPageNames.CalendarioSemanal => x.GetRequiredService<CalendarioSemanalViewModel>(),
            ApplicationPageNames.Settings => x.GetRequiredService<SettingsViewModel>(),
            ApplicationPageNames.AreaInteres => x.GetRequiredService<AreaInteresViewModel>(),
            ApplicationPageNames.Location => x.GetRequiredService<LocationViewModel>(),
            ApplicationPageNames.CalendarioMensual => x.GetRequiredService<CalendarioMensualViewModel>(),
            ApplicationPageNames.SugerenciasSemanales => x.GetRequiredService<SugerenciasSemanalesViewModel>(),
            ApplicationPageNames.Cuenta => x.GetRequiredService<CuentaViewModel>(),
            ApplicationPageNames.Soporte => x.GetRequiredService<SoporteViewModel>(),
            ApplicationPageNames.About => x.GetRequiredService<AboutViewModel>(),
            ApplicationPageNames.EstadisticaUsuario => x.GetRequiredService<EstadisticaUsuarioViewModel>(),
            ApplicationPageNames.ModificacionUsuario => x.GetRequiredService<ModificacionUsuarioViewModel>(),
            ApplicationPageNames.BusquedaUsuario => x.GetRequiredService<BusquedaUsuarioViewModel>(),
            ApplicationPageNames.DetalleUsuario => x.GetRequiredService<DetalleUsuarioViewModel>(),
            ApplicationPageNames.Login => x.GetRequiredService<LoginViewModel>(),
            ApplicationPageNames.Registro => x.GetRequiredService<RegistroViewModel>(),
            ApplicationPageNames.RecuperarContra => x.GetRequiredService<RecuperarContraViewModel>(),
            _ => throw new ArgumentOutOfRangeException(nameof(name), name, null)
        } );
        
        collection.AddSingleton<PageFactory>();

        var services = collection.BuildServiceProvider();
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainView
            {
                DataContext = services.GetRequiredService<MainViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}