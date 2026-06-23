using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
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
    private readonly IUbicacionRepository _ubicacionRepo;

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
    [ObservableProperty] private string _ubicacionDisplay = "— Sin ubicación —";

    public ObservableCollection<string> MisUbicaciones { get; } = new() { "— Sin ubicación —" };

    private bool esModoEdicion;
    private string? IdAreaEditando;

    public AreaInteresViewModel(ITareaRepository tareaRepo, IAreaInteresRepository areaRepo, ISesionService sesion, IUbicacionRepository ubicacionRepo)
        : base(tareaRepo, areaRepo, sesion)
    {
        _ubicacionRepo = ubicacionRepo;
        PageName = ApplicationPageNames.UserAreaInteres;
    }

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName == nameof(TareaSeleccionada) && TareaSeleccionada != null)
        {
            UbicacionDisplay = string.IsNullOrEmpty(TareaSeleccionada.Ubicacion) || !MisUbicaciones.Contains(TareaSeleccionada.Ubicacion)
                               ? "— Sin ubicación —"
                               : TareaSeleccionada.Ubicacion;
        }
    }

    partial void OnUbicacionDisplayChanged(string value) => DetalleUbicacion = (value == "— Sin ubicación —") ? null : value;

    public override async Task CargarTareasAsync()
    {
        if (Sesion.UsuarioActual == null || AreaSeleccionada?.IdAreaInteres == null) return;

        var idUsuario = Sesion.UsuarioActual.IdUsuario!;

        AreasInteres = new ObservableCollection<AreaInteres>(
            await AreaRepo.ObtenerAreasPorUsuario(idUsuario));

        SincronizarUbicaciones(await _ubicacionRepo.ObtenerUbicacionesPorUsuario(idUsuario));
        ReaplicarUbicacionSeleccionada();

        var tareas = await TareaRepo.ObtenerTareasPorArea(AreaSeleccionada.IdAreaInteres);

        // Más recientes arriba (la tarea recién creada aparece al tope de la lista).
        var pendientes = tareas.Where(t => t.FecCompletado == null).OrderByDescending(t => t.FecCreacion).ToList();
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

    // Tras reconstruir MisUbicaciones, el ComboBox del detalle puede resetear su SelectedItem a
    // null y propagarlo por el binding TwoWay (dejando "— Sin ubicación —" vacío). Volvemos a
    // aplicar la selección correcta una vez que el control procesó el cambio de items.
    private void ReaplicarUbicacionSeleccionada()
    {
        if (TareaSeleccionada == null) return;

        var objetivo = string.IsNullOrEmpty(TareaSeleccionada.Ubicacion) || !MisUbicaciones.Contains(TareaSeleccionada.Ubicacion)
            ? "— Sin ubicación —"
            : TareaSeleccionada.Ubicacion;

        Dispatcher.UIThread.Post(() =>
        {
            if (TareaSeleccionada != null && UbicacionDisplay != objetivo)
                UbicacionDisplay = objetivo;
        }, DispatcherPriority.Background);
    }

    private void SincronizarUbicaciones(IEnumerable<UbicacionGuardada> ubicacionesDb)
    {
        var nombresNuevos = ubicacionesDb
            .Select(u => u.Nombre)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct()
            .ToList();

        var listaFinal = new List<string> { "— Sin ubicación —" };
        listaFinal.AddRange(nombresNuevos);

        for (int i = MisUbicaciones.Count - 1; i >= 0; i--)
        {
            if (!listaFinal.Contains(MisUbicaciones[i]))
                MisUbicaciones.RemoveAt(i);
        }

        for (int i = 0; i < listaFinal.Count; i++)
        {
            var item = listaFinal[i];
            int idx = MisUbicaciones.IndexOf(item);
            if (idx == -1)
                MisUbicaciones.Insert(i, item);
            else if (idx != i)
                MisUbicaciones.Move(idx, i);
        }
    }

    protected override bool ShouldDeselectAfterSave(Tarea tarea)
    {
        return tarea.IdAreaInteres != AreaSeleccionada?.IdAreaInteres;
    }
}