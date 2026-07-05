using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using planificApp.Data;
using planificApp.Factories;
using planificApp.Services;
using planificApp.ViewModels;
using PlanificApp.Models.Services;
using PlanificApp.Models.Services.Interfaces;
using PlanificApp.Models.Repositories;
using PlanificApp.Models.Repositories.Interfaces;
using System;

[assembly: XmlnsDefinition("https://github.com/avaloniaui", "planificApp.StyleControl")]
namespace planificApp;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        DataTemplates.Add(new ViewLocator());
    }

    public static void LogCrash(string origen, Exception? ex)
    {
        try
        {
            var dir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "planificApp");
            System.IO.Directory.CreateDirectory(dir);
            System.IO.File.AppendAllText(System.IO.Path.Combine(dir, "crash.log"),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ({origen})\n{ex}\n\n");
        }
        catch { /* nunca dejamos que el logger cause otra excepción */ }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Diagnóstico: registra cualquier excepción no controlada en un archivo (varios flujos usan
        // 'async void', cuyas excepciones tiran la app entera). Ver %APPDATA%/planificApp/crash.log
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            LogCrash("AppDomain", e.ExceptionObject as Exception);
        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            LogCrash("UnobservedTask", e.Exception);
            e.SetObserved();
        };

        var collection = new ServiceCollection();
        
        // Main ViewModels
        collection.AddSingleton<MainViewModel>();
        
        // Auth ViewModels
        collection.AddTransient<LoginViewModel>();
        collection.AddTransient<RegistroViewModel>();
        collection.AddTransient<RecuperarContraViewModel>();
        
        
        // User - Tareas ViewModels
        collection.AddTransient<InboxViewModel>();
        collection.AddTransient<NewTaskViewModel>();
        collection.AddTransient<AreaInteresViewModel>();
        collection.AddTransient<NewAreaViewModel>();
        
        // User - Calendario ViewModels
        collection.AddTransient<CalendarioSemanalViewModel>();
        collection.AddTransient<CalendarioMensualViewModel>();
        collection.AddSingleton<PropuestasSemanalesViewModel>();
        collection.AddTransient<CondicionesGeneracionViewModel>();
        
        // User - Otras ViewModels
        collection.AddTransient<UbicacionesViewModel>();
        collection.AddTransient<ConfigViewModel>();
        collection.AddTransient<DatosViewModel>();
        collection.AddTransient<SoporteViewModel>();
        collection.AddTransient<SobreViewModel>();
        
        // Admin ViewModels
        collection.AddTransient<EstadisticasViewModel>();
        collection.AddTransient<UsuarioDetalleViewModel>();
        collection.AddTransient<UsuariosViewModel>();

        // ⚠️ TEMP DEV: generador de datos de prueba para la entrega. Quitar antes de entregar.
        collection.AddTransient<PlanificApp.Models.Services.DatosPruebaSeeder>();

        // Configuration
        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .SetBasePath(System.AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
        collection.AddSingleton<Microsoft.Extensions.Configuration.IConfiguration>(config);

        // Infrastructure
        collection.AddSingleton<MongoContext>();
        
        // Repositories
        collection.AddSingleton<IUsuarioRepository, UsuarioRepository>();
        collection.AddSingleton<ITareaRepository, TareaRepository>();
        collection.AddSingleton<IAreaInteresRepository, AreaInteresRepository>();
        collection.AddSingleton<IUbicacionRepository, UbicacionRepository>();
        collection.AddSingleton<IBloqueAreaInteresScheduleRepository, BloqueAreaInteresScheduleRepository>();
        collection.AddSingleton<IGeneracionRepository, GeneracionRepository>();

        // Services
        collection.AddSingleton<IAuthenticationService, AuthenticationService>();
        collection.AddSingleton<ISesionService>(sp => new SesionService(sp.GetRequiredService<IUsuarioRepository>()));
        collection.AddSingleton<IGeoService, GeoService>();
		collection.AddSingleton<ICalendarioSemanalService, CalendarioSemanalService>();
        collection.AddSingleton<IGeneradorSemanalService, GeneradorSemanalService>();
        collection.AddSingleton<IDialogService, DialogService>();
        collection.AddSingleton<INavigationService>(sp => sp.GetRequiredService<MainViewModel>());
        collection.AddSingleton<IRegionesService, RegionesService>();
        collection.AddSingleton<IMetricasService, MetricasService>();
        collection.AddSingleton<IAppearanceService, AppearanceService>();
        collection.AddSingleton<Func<ApplicationPageNames, PageViewModel>>(x => name => name switch

        {
            // Auth
            ApplicationPageNames.Login => x.GetRequiredService<LoginViewModel>(),
            ApplicationPageNames.Registro => x.GetRequiredService<RegistroViewModel>(),
            ApplicationPageNames.RecuperarContra => x.GetRequiredService<RecuperarContraViewModel>(),
            // User - Tareas
            ApplicationPageNames.UserInbox => x.GetRequiredService<InboxViewModel>(),
            ApplicationPageNames.UserHoy => x.GetRequiredService<InboxViewModel>(),
            ApplicationPageNames.UserSemana => x.GetRequiredService<InboxViewModel>(),
            ApplicationPageNames.UserMes => x.GetRequiredService<InboxViewModel>(),
            ApplicationPageNames.UserAreaInteres => x.GetRequiredService<AreaInteresViewModel>(),
            // User - Calendario
            ApplicationPageNames.UserCalendarioSemanal => x.GetRequiredService<CalendarioSemanalViewModel>(),
            ApplicationPageNames.UserCalendarioMensual => x.GetRequiredService<CalendarioMensualViewModel>(),
            ApplicationPageNames.UserPropuestasSemanales => x.GetRequiredService<PropuestasSemanalesViewModel>(),
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
        Services = services;

        // Aplica el tema guardado (claro/oscuro) antes de mostrar la ventana.
        services.GetRequiredService<IAppearanceService>().ApplyOnStartup();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainVm = services.GetRequiredService<MainViewModel>();
            mainVm.Initialize();
            desktop.MainWindow = new MainView
            {
                DataContext = mainVm
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
