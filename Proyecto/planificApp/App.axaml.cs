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
        collection.AddTransient<TodayViewModel>();

        collection.AddSingleton<Func<ApplicationPageNames, PageViewModel>>(x => name => name switch
        {
            ApplicationPageNames.Inbox => x.GetRequiredService<InboxViewModel>(),
            ApplicationPageNames.Today => x.GetRequiredService<TodayViewModel>(),
            ApplicationPageNames.CalendarioSemanal => x.GetRequiredService<CalendarioSemanalViewModel>(),
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