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
    private readonly IUbicacionRepository _ubicacionRepo;

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

    // --- PROPIEDADES PARA EL PANEL DE DETALLES ---
    [ObservableProperty] private string _prioridadDisplay = "1 - Baja";
    [ObservableProperty] private string _tiempoDisplay = "— Sin definir —";
    [ObservableProperty] private string _actividadFisicaDisplay = "— Ninguna —";
    [ObservableProperty] private string _actividadMentalDisplay = "— Ninguna —";
    [ObservableProperty] private string _ubicacionDisplay = "— Sin ubicación —"; // NUEVO

    // LISTAS
    public ObservableCollection<string> Prioridades { get; } = new() { "1 - Baja", "2 - Media", "3 - Alta", "4 - Urgente", "5 - Fija" };
    public ObservableCollection<string> Tiempos { get; } = new() { "— Sin definir —", "5 minutos", "10 minutos", "15 minutos", "30 minutos", "45 minutos", "1 hora", "1.5 horas", "2 horas", "3 horas", "4 horas" };
    public ObservableCollection<string> NivelesActividadFisica { get; } = new() { "— Ninguna —", "Activa", "Pasiva" };
    public ObservableCollection<string> NivelesActividadMental { get; } = new() { "— Ninguna —", "Obligación", "Recreación" };
    public ObservableCollection<string> MisUbicaciones { get; } = new() { "— Sin ubicación —" }; // NUEVA LISTA DINÁMICA

    private DispatcherTimer? _refreshTimer;

    // AÑADIMOS IUbicacionRepository al constructor
    public InboxViewModel(ITareaRepository tareaRepo, IAreaInteresRepository areaRepo, ISesionService sesion, IUbicacionRepository ubicacionRepo)
        : base(tareaRepo, areaRepo, sesion)
    {
        _ubicacionRepo = ubicacionRepo;
        PageName = Data.ApplicationPageNames.UserInbox;

        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _refreshTimer.Tick += async (_, _) => await RefreshTareasAsync();
        _refreshTimer.Start();
    }

    // --- LÓGICA DE SINCRONIZACIÓN AUTOMÁTICA ---
    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        // Pausamos el refresco automático mientras se edita una tarea para evitar
        // que el ComboBox de ubicación pierda su selección al limpiar la lista.
        if (e.PropertyName == nameof(TareaSeleccionada))
        {
            if (TareaSeleccionada != null)
                _refreshTimer?.Stop();
            else
                _refreshTimer?.Start();
        }

        if (e.PropertyName == nameof(TareaSeleccionada) && TareaSeleccionada != null)
        {
            PrioridadDisplay = Prioridades.FirstOrDefault(p => p.StartsWith(TareaSeleccionada.Prioridad.ToString())) ?? "1 - Baja";

            TiempoDisplay = TareaSeleccionada.TiempoEstimado switch
            {
                0 => "— Sin definir —",
                5 => "5 minutos",
                10 => "10 minutos",
                15 => "15 minutos",
                30 => "30 minutos",
                45 => "45 minutos",
                60 => "1 hora",
                90 => "1.5 horas",
                120 => "2 horas",
                180 => "3 horas",
                240 => "4 horas",
                _ => "— Sin definir —"
            };

            ActividadFisicaDisplay = string.IsNullOrEmpty(TareaSeleccionada.TipoActividadFisica) ? "— Ninguna —" : TareaSeleccionada.TipoActividadFisica;
            ActividadMentalDisplay = string.IsNullOrEmpty(TareaSeleccionada.TipoActividadMental) ? "— Ninguna —" : TareaSeleccionada.TipoActividadMental;

            // Si la ubicación existe en la base de datos, la mostramos; si no, ponemos "Sin ubicación"
            UbicacionDisplay = string.IsNullOrEmpty(TareaSeleccionada.Ubicacion) || !MisUbicaciones.Contains(TareaSeleccionada.Ubicacion)
                               ? "— Sin ubicación —"
                               : TareaSeleccionada.Ubicacion;
        }
    }

    partial void OnPrioridadDisplayChanged(string value)
    {
        if (!string.IsNullOrEmpty(value)) DetallePrioridad = int.Parse(value.Split(' ')[0]);
    }

    partial void OnTiempoDisplayChanged(string value)
    {
        DetalleTiempoEstimado = value switch
        {
            "5 minutos" => 5,
            "10 minutos" => 10,
            "15 minutos" => 15,
            "30 minutos" => 30,
            "45 minutos" => 45,
            "1 hora" => 60,
            "1.5 horas" => 90,
            "2 horas" => 120,
            "3 horas" => 180,
            "4 horas" => 240,
            _ => 0
        };
    }

    partial void OnActividadFisicaDisplayChanged(string value) => DetalleTipoActividadFisica = (value == "— Ninguna —") ? null : value;
    partial void OnActividadMentalDisplayChanged(string value) => DetalleTipoActividadMental = (value == "— Ninguna —") ? null : value;
    partial void OnUbicacionDisplayChanged(string value) => DetalleUbicacion = (value == "— Sin ubicación —") ? null : value;

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

        // 1. SINCRONIZAMOS ÁREAS DE INTERÉS (sin reemplazar la colección entera)
        SincronizarAreasInteres(await AreaRepo.ObtenerAreasPorUsuario(idUsuario));

        // 2. SINCRONIZAMOS UBICACIONES REALES DESDE MONGODB (sin Clear())
        SincronizarUbicaciones(await _ubicacionRepo.ObtenerUbicacionesPorUsuario(idUsuario));
        ReaplicarUbicacionSeleccionada();

        // 3. CARGAMOS TAREAS
        var tareas = ModoActual switch
        {
            ModoVista.Hoy => await TareaRepo.ObtenerTareasPorRango(idUsuario, DateTime.Today, DateTime.Today.AddDays(1)),
            ModoVista.Semana => await ObtenerTareasSemana(idUsuario),
            ModoVista.Mes => await ObtenerTareasMes(idUsuario),
            _ => (await TareaRepo.ObtenerTareasPorUsuario(idUsuario)).Where(t => t.IdAreaInteres == null).ToList()
        };

        SincronizarTareas(tareas);

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

    // --- SINCRONIZACIÓN DIFERENCIAL DE COLECCIONES ---
    // Evitamos Clear()/recrear colecciones para que los ComboBoxes no pierdan
    // su SelectedItem mientras el usuario está interactuando con ellos.

    private void SincronizarAreasInteres(List<AreaInteres> areasNuevas)
    {
        var idsNuevos = new HashSet<string?>(areasNuevas.Select(a => a.IdAreaInteres));

        for (int i = AreasInteres.Count - 1; i >= 0; i--)
        {
            if (!idsNuevos.Contains(AreasInteres[i].IdAreaInteres))
                AreasInteres.RemoveAt(i);
        }

        foreach (var nueva in areasNuevas)
        {
            var existente = AreasInteres.FirstOrDefault(a => a.IdAreaInteres == nueva.IdAreaInteres);
            if (existente == null)
            {
                AreasInteres.Add(nueva);
            }
            else
            {
                int idx = AreasInteres.IndexOf(existente);
                AreasInteres[idx] = nueva;
            }
        }
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

    private void SincronizarTareas(List<Tarea> tareasNuevas)
    {
        var idsNuevos = new HashSet<string?>(tareasNuevas.Select(t => t.IdTarea));

        // Eliminamos las que ya no existen en la base de datos.
        for (int i = Tareas.Count - 1; i >= 0; i--)
        {
            if (!idsNuevos.Contains(Tareas[i].IdTarea))
                Tareas.RemoveAt(i);
        }

        // Agregamos las nuevas o reemplazamos las existentes, pero preservamos
        // la instancia de la tarea seleccionada para no desincronizar el detalle.
        foreach (var nueva in tareasNuevas)
        {
            var existente = Tareas.FirstOrDefault(t => t.IdTarea == nueva.IdTarea);
            if (existente == null)
            {
                Tareas.Add(nueva);
            }
            else if (existente != TareaSeleccionada)
            {
                int idx = Tareas.IndexOf(existente);
                Tareas[idx] = nueva;
            }
        }
    }

    protected override bool ShouldDeselectAfterSave(Tarea tarea)
    {
        return ModoActual == ModoVista.Inbox && tarea.IdAreaInteres != null;
    }
}