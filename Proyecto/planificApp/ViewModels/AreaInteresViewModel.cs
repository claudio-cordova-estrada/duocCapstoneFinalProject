using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using planificApp.Data;
using PlanificApp.Models;
using PlanificApp.Models.Services.Interfaces;
using PlanificApp.Models.Repositories.Interfaces;
using planificApp.Helpers;

namespace planificApp.ViewModels;

public partial class AreaInteresViewModel : TareaDetailViewModelBase
{
    [ObservableProperty] private AreaInteres? _areaSeleccionada;
    [ObservableProperty] private string _tituloVista = string.Empty;
    [ObservableProperty] private string _subtituloVista = string.Empty;
    [ObservableProperty] private ObservableCollection<Tarea> _tareasPendientesFiltradas = new();
    [ObservableProperty] private ObservableCollection<Tarea> _tareasCompletadasFiltradas = new();
    [ObservableProperty] private int _tareasPendientes;
    [ObservableProperty] private int _tareasCompletadasCount;
    [ObservableProperty] private int _tareasVencidasCount;
    [ObservableProperty] private bool _hayCompletadas;
    [ObservableProperty] private bool _completadasVisibles = true;
    [ObservableProperty] private string _quickAddNombre = string.Empty;

    private bool esModoEdicion;
    private string? IdAreaEditando;

    public AreaInteresViewModel(ITareaRepository tareaRepo, IAreaInteresRepository areaRepo, ISesionService sesion)
        : base(tareaRepo, areaRepo, sesion)
    {
        PageName = ApplicationPageNames.UserAreaInteres;
    }

    public override async Task CargarTareasAsync()
    {
        if (Sesion.UsuarioActual == null || AreaSeleccionada?.IdAreaInteres == null) return;

        var idUsuario = Sesion.UsuarioActual.IdUsuario!;

        AreasInteres = new ObservableCollection<AreaInteres>(
            await AreaRepo.ObtenerAreasPorUsuario(idUsuario));

        var tareas = await TareaRepo.ObtenerTareasPorArea(AreaSeleccionada.IdAreaInteres);

        var pendientes = tareas.Where(t => t.FecCompletado == null).ToList();
        var completadas = tareas.Where(t => t.FecCompletado != null).ToList();
        var vencidas = pendientes.Count(t => (t.FecLimite != null && t.FecLimite < DateTime.Now) ||
                                             (t.FecLimite == null && t.FecInicio != null && t.FecInicio < DateTime.Now));

        TareasPendientesFiltradas = new ObservableCollection<Tarea>(pendientes);
        TareasCompletadasFiltradas = new ObservableCollection<Tarea>(completadas);

        TareasPendientes = pendientes.Count(t => !((t.FecLimite != null && t.FecLimite < DateTime.Now) ||
                                                  (t.FecLimite == null && t.FecInicio != null &&
                                                   t.FecInicio < DateTime.Now)));
        TareasCompletadasCount = completadas.Count;
        TareasVencidasCount = vencidas;
        HayCompletadas = completadas.Count > 0;

        SubtituloVista = $"{TareasPendientes} tareas activas";
    }

    public void SetArea(AreaInteres area)
    {
        AreaSeleccionada = area;
        TituloVista = area.Nombre ?? "\u00C1rea";
        SubtituloVista = "Cargando...";
        _ = CargarTareasAsync();
    }

    [RelayCommand]
    private void ToggleCompletadas()
    {
        CompletadasVisibles = !CompletadasVisibles;
    }

    [RelayCommand]
    private async Task QuickAddAsync()
    {
        if (string.IsNullOrWhiteSpace(QuickAddNombre)) return;
        if (Sesion.UsuarioActual == null) return;
        if (AreaSeleccionada?.IdAreaInteres == null) return;

        var tarea = new Tarea
        {
            Nombre = QuickAddNombre.Trim(),
            IdUsuario = Sesion.UsuarioActual.IdUsuario,
            IdAreaInteres = AreaSeleccionada.IdAreaInteres,
            Prioridad = 1,
            FecCreacion = DateTime.Now
        };

        DetalleTareaHelper.AplicarDefaultsArea(tarea, AreaSeleccionada);

        await TareaRepo.CrearTarea(tarea);
        QuickAddNombre = string.Empty;
        await CargarTareasAsync();
    }

    protected override bool ShouldDeselectAfterSave(Tarea tarea)
    {
        return tarea.IdAreaInteres != AreaSeleccionada?.IdAreaInteres;
    }
}