using System;
using System.Reflection;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Metadata;
using Microsoft.Extensions.DependencyInjection;
using planificApp.Data;
using planificApp.Factories;
using planificApp.ViewModels;
using PlanificApp.Models.Services;

[assembly: XmlnsDefinition("https://github.com/avaloniaui", "planificApp.StyleControl")]
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
        
        // Main ViewModels
        collection.AddSingleton<MainViewModel>();
        collection.AddSingleton<AdminMainViewModel>();
        
        // Auth ViewModels
        collection.AddTransient<LoginViewModel>();
        collection.AddTransient<RegistroViewModel>();
        collection.AddTransient<RecuperarContraViewModel>();
        
        // User - Tareas ViewModels
        collection.AddTransient<InboxViewModel>();
        collection.AddTransient<HoyViewModel>();
        collection.AddTransient<SemanaViewModel>();
        collection.AddTransient<MesViewModel>();
        collection.AddTransient<AreaInteresViewModel>();
        
        // User - Calendario ViewModels
        collection.AddTransient<CalendarioSemanalViewModel>();
        collection.AddTransient<CalendarioMensualViewModel>();
        collection.AddTransient<SugerenciasViewModel>();
        collection.AddTransient<PropuestasSemanalesViewModel>();
        
        // User - Otras ViewModels
        collection.AddTransient<UbicacionesViewModel>();
        collection.AddTransient<ConfigViewModel>();
        collection.AddTransient<DatosViewModel>();
        collection.AddTransient<SoporteViewModel>();
        collection.AddTransient<SobreViewModel>();
        
        // Admin ViewModels
        collection.AddTransient<EstadisticasViewModel>();
        collection.AddTransient<UsuariosViewModel>();
        collection.AddTransient<UsuarioDetalleViewModel>();

        // Services
        collection.AddSingleton<MongoService>();
        collection.AddSingleton<SesionService>();

        collection.AddSingleton<Func<ApplicationPageNames, PageViewModel>>(x => name => name switch
        {
            // Auth
            ApplicationPageNames.Login => x.GetRequiredService<LoginViewModel>(),
            ApplicationPageNames.Registro => x.GetRequiredService<RegistroViewModel>(),
            ApplicationPageNames.RecuperarContra => x.GetRequiredService<RecuperarContraViewModel>(),
            // User - Tareas
            ApplicationPageNames.UserInbox => x.GetRequiredService<InboxViewModel>(),
            ApplicationPageNames.UserHoy => x.GetRequiredService<HoyViewModel>(),
            ApplicationPageNames.UserSemana => x.GetRequiredService<SemanaViewModel>(),
            ApplicationPageNames.UserMes => x.GetRequiredService<MesViewModel>(),
            ApplicationPageNames.UserAreaInteres => x.GetRequiredService<AreaInteresViewModel>(),
            // User - Calendario
            ApplicationPageNames.UserCalendarioSemanal => x.GetRequiredService<CalendarioSemanalViewModel>(),
            ApplicationPageNames.UserCalendarioMensual => x.GetRequiredService<CalendarioMensualViewModel>(),
            ApplicationPageNames.UserPropuestasSemanales => x.GetRequiredService<PropuestasSemanalesViewModel>(),
            ApplicationPageNames.UserSugerencias => x.GetRequiredService<SugerenciasViewModel>(),
            // User - Otras
            ApplicationPageNames.UserUbicaciones => x.GetRequiredService<UbicacionesViewModel>(),
            ApplicationPageNames.UserConfig => x.GetRequiredService<ConfigViewModel>(),
            ApplicationPageNames.UserDatos => x.GetRequiredService<DatosViewModel>(),
            ApplicationPageNames.UserSoporte => x.GetRequiredService<SoporteViewModel>(),
            ApplicationPageNames.UserSobre => x.GetRequiredService<SobreViewModel>(),
            // Admin
            ApplicationPageNames.AdminEstadisticas => x.GetRequiredService<EstadisticasViewModel>(),
            ApplicationPageNames.AdminUsuarios => x.GetRequiredService<UsuariosViewModel>(),
            ApplicationPageNames.AdminUsuarioDetalle => x.GetRequiredService<UsuarioDetalleViewModel>(),
            _ => throw new ArgumentOutOfRangeException(nameof(name), name, null)
        });
        
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
