using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlanificApp.Models;
using PlanificApp.Models.Services.Interfaces;
using PlanificApp.Models.Repositories.Interfaces;

namespace planificApp.ViewModels;

public enum ModoVista { Inbox, Hoy, Semana, Mes }

public partial class InboxViewModel : TareaDetailViewModelBase
{
    [ObservableProperty] private ModoVista _modoActual = ModoVista.Inbox;
    [ObservableProperty] private string _tituloVista = "Inbox";
    [ObservableProperty] private string _subtituloVista = string.Empty;

    [ObservableProperty] private ObservableCollection<Tarea> _tareas = new();
    [ObservableProperty] private ObservableCollection<Tarea> _tareasPendientesFiltradas = new();
    [ObservableProperty] private ObservableCollection<Tarea> _tareasCompletadasFiltradas = new();
    [ObservableProperty] private int _tareasPendientes;
    [ObservableProperty] private int _tareasCompletadasCount;
    [ObservableProperty] private bool _hayCompletadas;
    [ObservableProperty] private bool _completadasVisibles = true;
    [ObservableProperty] private string _quickAddNombre = string.Empty;

    private DispatcherTimer? _refreshTimer;

    public InboxViewModel(ITareaRepository tareaRepo, IAreaInteresRepository areaRepo, ISesionService sesion)
        : base(tareaRepo, areaRepo, sesion)
    {
        PageName = Data.ApplicationPageNames.UserInbox;

        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _refreshTimer.Tick += async (_, _) => await RefreshTareasAsync();
        _refreshTimer.Start();
    }

    public void SetModo(ModoVista modo)
    {
        ModoActual = modo;
        PageName = modo switch
        {
            ModoVista.Hoy => Data.ApplicationPageNames.UserHoy,
            ModoVista.Semana => Data.ApplicationPageNames.UserSemana,
            ModoVista.Mes => Data.ApplicationPageNames.UserMes,
            _ => Data.ApplicationPageNames.UserInbox
        };

        TituloVista = modo switch
        {
            ModoVista.Hoy => "Hoy",
            ModoVista.Semana => "Esta semana",
            ModoVista.Mes => "Mes",
            _ => "Inbox"
        };

        OnPropertyChanged(nameof(PageName));
    }

    private async Task RefreshTareasAsync()
    {
        if (Sesion.UsuarioActual == null) return;
        await CargarTareasAsync();
    }

    public override async Task CargarTareasAsync()
    {
        if (Sesion.UsuarioActual == null) return;

        var idUsuario = Sesion.UsuarioActual.IdUsuario!;

        AreasInteres = new ObservableCollection<AreaInteres>(
            await AreaRepo.ObtenerAreasPorUsuario(idUsuario));

        var tareas = ModoActual switch
        {
            ModoVista.Hoy => await TareaRepo.ObtenerTareasPorRango(idUsuario, DateTime.Today, DateTime.Today.AddDays(1)),
            ModoVista.Semana => await ObtenerTareasSemana(idUsuario),
            ModoVista.Mes => await ObtenerTareasMes(idUsuario),
            _ => (await TareaRepo.ObtenerTareasPorUsuario(idUsuario)).Where(t => t.IdAreaInteres == null).ToList()
        };

        Tareas = new ObservableCollection<Tarea>(tareas);

        SubtituloVista = ModoActual switch
        {
            ModoVista.Hoy => DateTime.Today.ToString("dddd d 'de' MMMM, yyyy"),
            ModoVista.Semana => ObtenerLabelSemana(),
            ModoVista.Mes => DateTime.Today.ToString("MMMM yyyy"),
            _ => $"{Tareas.Count(t => t.FecCompletado == null)} tareas activas"
        };

        AplicarFiltro();
    }

    private async Task<List<Tarea>> ObtenerTareasSemana(string idUsuario)
    {
        var hoy = DateTime.Today;
        var inicioSemana = hoy.AddDays(-(int)hoy.DayOfWeek + 1);
        if (hoy.DayOfWeek == DayOfWeek.Sunday) inicioSemana = inicioSemana.AddDays(-7);
        var finSemana = inicioSemana.AddDays(7);
        return await TareaRepo.ObtenerTareasPorRango(idUsuario, inicioSemana, finSemana);
    }

    private async Task<List<Tarea>> ObtenerTareasMes(string idUsuario)
    {
        var hoy = DateTime.Today;
        var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
        var finMes = inicioMes.AddMonths(1);
        return await TareaRepo.ObtenerTareasPorRango(idUsuario, inicioMes, finMes);
    }

    private static string ObtenerLabelSemana()
    {
        var hoy = DateTime.Today;
        var inicioSemana = hoy.AddDays(-(int)hoy.DayOfWeek + 1);
        if (hoy.DayOfWeek == DayOfWeek.Sunday) inicioSemana = inicioSemana.AddDays(-7);
        var finSemana = inicioSemana.AddDays(6);
        return $"{inicioSemana:dd} al {finSemana:dd} de {inicioSemana:MMMM}, {inicioSemana:yyyy}";
    }

    private void AplicarFiltro()
    {
        var pendientes = Tareas.Where(t => t.FecCompletado == null).ToList();
        var completadas = Tareas.Where(t => t.FecCompletado != null).ToList();
        TareasPendientes = pendientes.Count;
        TareasCompletadasCount = completadas.Count;
        HayCompletadas = completadas.Count > 0;

        TareasPendientesFiltradas = new ObservableCollection<Tarea>(pendientes);
        TareasCompletadasFiltradas = new ObservableCollection<Tarea>(completadas);
    }

    [RelayCommand]
    private void ToggleCompletadas()
    {
        CompletadasVisibles = !CompletadasVisibles;
    }

    [RelayCommand]
    private async Task CompletarTareaAsync(Tarea tarea)
    {
        if (tarea.IdTarea == null) return;

        await TareaRepo.CompletarTarea(tarea.IdTarea);
        if (TareaSeleccionada?.IdTarea == tarea.IdTarea)
            TareaSeleccionada = null;
        await CargarTareasAsync();
    }

    [RelayCommand]
    private async Task QuickAddAsync()
    {
        if (string.IsNullOrWhiteSpace(QuickAddNombre)) return;
        if (Sesion.UsuarioActual == null) return;

        var tarea = new Tarea
        {
            Nombre = QuickAddNombre.Trim(),
            IdUsuario = Sesion.UsuarioActual.IdUsuario,
            Prioridad = 1,
            FecCreacion = DateTime.Now
        };

        await TareaRepo.CrearTarea(tarea);
        QuickAddNombre = string.Empty;
        await CargarTareasAsync();
    }

    protected override bool ShouldDeselectAfterSave(Tarea tarea)
    {
        return ModoActual == ModoVista.Inbox && tarea.IdAreaInteres != null;
    }
}