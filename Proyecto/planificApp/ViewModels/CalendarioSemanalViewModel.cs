using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlanificApp.Models;
using PlanificApp.Models.Repositories.Interfaces;
using PlanificApp.Models.Services.Interfaces;
using planificApp.Data;

namespace planificApp.ViewModels;

public partial class CalendarioSemanalViewModel : PageViewModel
{
    private readonly ICalendarioSemanalService _calendarioService;
    private readonly ITareaRepository _tareaRepo;
    private readonly IAreaInteresRepository _areaRepo;
    private readonly IUbicacionRepository _ubicacionRepo;
    private readonly ISesionService _sesionService;
    private readonly IGeoService _geoService;

    [ObservableProperty] private SemanaCalendario _semanaActual = new();
    [ObservableProperty] private ObservableCollection<DiaCalendario> _dias = new();
    [ObservableProperty] private ObservableCollection<Tarea> _tareasDisponibles = new();
    [ObservableProperty] private ObservableCollection<AreaInteres> _areasDisponibles = new();
    [ObservableProperty] private ObservableCollection<Tarea> _tareasSinAsignar = new();
    [ObservableProperty] private ObservableCollection<AreaConTareas> _areasConTareas = new();
    [ObservableProperty] private string _numeroSemana = string.Empty;
    [ObservableProperty] private string _fechaRango = string.Empty;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private BloqueCalendario? _bloqueSeleccionado;
    [ObservableProperty] private DateTime _fechaLunesActual = GetLunes(DateTime.Today);

    public Dictionary<string, ObservableCollection<Tarea>> TareasPorArea { get; private set; } = new();

    public CalendarioSemanalViewModel(
        ICalendarioSemanalService calendarioService,
        ITareaRepository tareaRepo,
        IAreaInteresRepository areaRepo,
        IUbicacionRepository ubicacionRepo,
        ISesionService sesionService,
        IGeoService geoService)
    {
        PageName = ApplicationPageNames.UserCalendarioSemanal;

        _calendarioService = calendarioService;
        _tareaRepo = tareaRepo;
        _areaRepo = areaRepo;
        _ubicacionRepo = ubicacionRepo;
        _sesionService = sesionService;
        _geoService = geoService;
    }

    public async Task InitializeAsync()
    {
        await CargarSemanaAsync();
    }

    partial void OnFechaLunesActualChanged(DateTime value)
    {
        _ = CargarSemanaAsync();
    }

    [RelayCommand]
    private async Task SemanaAnteriorAsync()
    {
        FechaLunesActual = FechaLunesActual.AddDays(-7);
    }

    [RelayCommand]
    private async Task SemanaSiguienteAsync()
    {
        FechaLunesActual = FechaLunesActual.AddDays(7);
    }

    [RelayCommand]
    private async Task IrHoyAsync()
    {
        FechaLunesActual = GetLunes(DateTime.Today);
    }

    [RelayCommand]
    private void GenerarSemana()
    {
    }

    public async Task AgregarBloqueInteresAsync(AreaInteres area)
    {
        if (area == null) return;

        var dia = Dias.FirstOrDefault(d => d.EsHoy) ?? Dias.FirstOrDefault();
        if (dia == null) return;

        var horaInicio = _sesionService.UsuarioActual?.HoraInicioJornada ?? TimeSpan.FromHours(8);
        var horaFin = horaInicio + TimeSpan.FromHours(1);

        _calendarioService.AgregarBloqueInteres(dia, area, horaInicio, horaFin);
        await RecalcularTrasladosYGuardarAsync(dia);
    }

    public async Task AgregarBloqueInteresEnDiaAsync(AreaInteres area, DiaCalendario dia, TimeSpan horaInicio)
    {
        if (area == null) return;

        var horaFin = horaInicio + TimeSpan.FromHours(1);
        _calendarioService.AgregarBloqueInteres(dia, area, horaInicio, horaFin);
        await RecalcularTrasladosYGuardarAsync(dia);
        Dias = new ObservableCollection<DiaCalendario>(SemanaActual.Dias);
        await CargarTareasYAreasAsync(_sesionService.UsuarioActual?.IdUsuario!);
    }

    public async Task AgregarTareaAsync(Tarea tarea)
    {
        if (tarea == null) return;

        var dia = Dias.FirstOrDefault(d => d.EsHoy) ?? Dias.FirstOrDefault();
        if (dia == null) return;

        var horaInicio = _sesionService.UsuarioActual?.HoraInicioJornada ?? TimeSpan.FromHours(8);
        var horaFin = horaInicio + TimeSpan.FromMinutes(tarea.TiempoEstimado > 0 ? tarea.TiempoEstimado : 60);

        _calendarioService.AgregarTarea(dia, tarea, horaInicio, horaFin);
        await RecalcularTrasladosYGuardarAsync(dia);
        Dias = new ObservableCollection<DiaCalendario>(SemanaActual.Dias);
        await CargarTareasYAreasAsync(_sesionService.UsuarioActual?.IdUsuario!);
    }

    public async Task AgregarTareaEnDiaAsync(Tarea tarea, DiaCalendario dia, TimeSpan horaInicio)
    {
        if (tarea == null) return;

        var duracion = tarea.TiempoEstimado > 0 ? tarea.TiempoEstimado : 60;
        var horaFin = horaInicio + TimeSpan.FromMinutes(duracion);

        if (!string.IsNullOrEmpty(tarea.IdAreaInteres))
        {
            var bloqueInteres = _calendarioService.FindInteresBlockAtTime(dia, horaInicio);
            if (bloqueInteres != null && bloqueInteres.IdAreaInteres == tarea.IdAreaInteres)
            {
                _calendarioService.AgregarTareaEnBloque(dia, bloqueInteres.Id, tarea, horaInicio, horaFin);
                await RecalcularTrasladosYGuardarAsync(dia);
                Dias = new ObservableCollection<DiaCalendario>(SemanaActual.Dias);
                await CargarTareasYAreasAsync(_sesionService.UsuarioActual?.IdUsuario!);
                return;
            }
        }

        _calendarioService.AgregarTarea(dia, tarea, horaInicio, horaFin, AreasDisponibles.ToList());
        await RecalcularTrasladosYGuardarAsync(dia);
        Dias = new ObservableCollection<DiaCalendario>(SemanaActual.Dias);
        await CargarTareasYAreasAsync(_sesionService.UsuarioActual?.IdUsuario!);
    }

    public async Task MoverBloqueAsync(DiaCalendario dia, string bloqueId, TimeSpan nuevaHora)
    {
        _calendarioService.MoverBloque(dia, bloqueId, nuevaHora);
        await RecalcularTrasladosYGuardarAsync(dia);
        Dias = new ObservableCollection<DiaCalendario>(SemanaActual.Dias);
    }

    public async Task<bool> IntentarInsertarTareaEnBloqueAsync(DiaCalendario dia, string tareaBloqueId, string bloqueInteresId)
    {
        var tareaBloque = dia.Bloques.FirstOrDefault(b => b.Id == tareaBloqueId);
        if (tareaBloque == null || tareaBloque.IdTarea == null) return false;

        var bloqueInteres = dia.Bloques.FirstOrDefault(b => b.Id == bloqueInteresId
            && b.Tipo == TipoBloqueCalendario.BloqueInteres);
        if (bloqueInteres == null) return false;

        if (tareaBloque.IdAreaInteresPadre != bloqueInteres.IdAreaInteres) return false;

        _calendarioService.InsertarTareaEnBloqueInteres(
            dia, bloqueInteresId, tareaBloque.IdTarea,
            tareaBloque.NombreTarea ?? "Tarea",
            tareaBloque.HoraInicio, tareaBloque.HoraFin, tareaBloque.Completada);

        await RecalcularTrasladosYGuardarAsync(dia);
        Dias = new ObservableCollection<DiaCalendario>(SemanaActual.Dias);
        await CargarTareasYAreasAsync(_sesionService.UsuarioActual?.IdUsuario!);
        return true;
    }

    public async Task MoverBloqueADiaAsync(DiaCalendario diaOrigen, DiaCalendario diaDestino, string bloqueId, TimeSpan nuevaHoraInicio)
    {
        var bloque = diaOrigen.Bloques.FirstOrDefault(b => b.Id == bloqueId);
        if (bloque == null) return;

        var duracion = bloque.HoraFin - bloque.HoraInicio;
        var nuevaHoraFin = nuevaHoraInicio + duracion;

        diaOrigen.Bloques.Remove(bloque);

        bloque.HoraInicio = nuevaHoraInicio;
        bloque.HoraFin = nuevaHoraFin;

        diaDestino.Bloques.Add(bloque);
        var ordenados = diaDestino.Bloques.OrderBy(b => b.HoraInicio).ToList();
        diaDestino.Bloques.Clear();
        foreach (var b in ordenados) diaDestino.Bloques.Add(b);

        var ordenadosOrigen = diaOrigen.Bloques.OrderBy(b => b.HoraInicio).ToList();
        diaOrigen.Bloques.Clear();
        foreach (var b in ordenadosOrigen) diaOrigen.Bloques.Add(b);

        await RecalcularTrasladosYGuardarAsync(diaOrigen);
        await RecalcularTrasladosYGuardarAsync(diaDestino);

        Dias = new ObservableCollection<DiaCalendario>(SemanaActual.Dias);
    }

    public async Task RedimensionarBloqueAsyncResult(DiaCalendario dia, string bloqueId, TimeSpan nuevaHoraInicio, TimeSpan nuevaHoraFin)
    {
        _calendarioService.RedimensionarBloque(dia, bloqueId, nuevaHoraInicio, nuevaHoraFin);
        await RecalcularTrasladosYGuardarAsync(dia);
        Dias = new ObservableCollection<DiaCalendario>(SemanaActual.Dias);
    }

    public async Task EliminarBloqueAsync(DiaCalendario dia, string bloqueId)
    {
        var bloque = _calendarioService.EliminarBloque(dia, bloqueId);
        if (bloque == null) return;

        if (bloque.Tipo == TipoBloqueCalendario.Tarea && bloque.IdTarea != null)
        {
            var tarea = await _tareaRepo.ObtenerTareaPorId(bloque.IdTarea);
            if (tarea != null)
            {
                tarea.HoraInicio = null;
                tarea.HoraFin = null;
                tarea.FecInicio = null;
                tarea.UsoGeneracion = false;
                await _tareaRepo.ActualizarTarea(tarea.IdTarea!, tarea);
            }
        }
        else if (bloque.Tipo == TipoBloqueCalendario.BloqueInteres && bloque.TareasInternas != null)
        {
            foreach (var sub in bloque.TareasInternas)
            {
                var tarea = await _tareaRepo.ObtenerTareaPorId(sub.IdTarea);
                if (tarea != null)
                {
                    tarea.HoraInicio = null;
                    tarea.HoraFin = null;
                    tarea.FecInicio = null;
                    tarea.UsoGeneracion = false;
                    await _tareaRepo.ActualizarTarea(tarea.IdTarea!, tarea);
                }
            }
        }

        await RecalcularTrasladosYGuardarAsync(dia);
        Dias = new ObservableCollection<DiaCalendario>(SemanaActual.Dias);
        await CargarTareasYAreasAsync(_sesionService.UsuarioActual?.IdUsuario!);
    }

    public async Task MoverSubTareaAsync(DiaCalendario dia, string parentBloqueId, string idTarea, TimeSpan nuevaInicio, TimeSpan nuevaFin)
    {
        _calendarioService.MoverSubTarea(dia, parentBloqueId, idTarea, nuevaInicio, nuevaFin);
        await RecalcularTrasladosYGuardarAsync(dia);
        Dias = new ObservableCollection<DiaCalendario>(SemanaActual.Dias);
    }

    public async Task ExtraerSubTareaAsync(DiaCalendario dia, BloqueCalendario parentBloque, string idTarea, TimeSpan? nuevaHoraInicio)
    {
        var sub = _calendarioService.ExtraerSubTarea(dia, parentBloque.Id, idTarea);
        if (sub == null) return;

        if (nuevaHoraInicio.HasValue)
        {
            var tarea = await _tareaRepo.ObtenerTareaPorId(idTarea);
            if (tarea != null)
            {
                var duracion = sub.HoraFin - sub.HoraInicio;
                var newBloque = _calendarioService.AgregarTarea(dia, tarea, nuevaHoraInicio.Value, nuevaHoraInicio.Value + duracion);
                if (newBloque.ColorHex == null) newBloque.ColorHex = parentBloque.ColorHex ?? "#666666";
            }
        }
        else
        {
            var tarea = await _tareaRepo.ObtenerTareaPorId(idTarea);
            if (tarea != null)
            {
                tarea.HoraInicio = null;
                tarea.HoraFin = null;
                tarea.FecInicio = null;
                tarea.UsoGeneracion = false;
                await _tareaRepo.ActualizarTarea(tarea.IdTarea!, tarea);
            }
        }

        await RecalcularTrasladosYGuardarAsync(dia);
        Dias = new ObservableCollection<DiaCalendario>(SemanaActual.Dias);
        await CargarTareasYAreasAsync(_sesionService.UsuarioActual?.IdUsuario!);
    }

    public async Task EliminarSubTareaAsync(DiaCalendario dia, string parentBloqueId, string idTarea)
    {
        _calendarioService.EliminarSubTarea(dia, parentBloqueId, idTarea);
        await _tareaRepo.EliminarTarea(idTarea);
        await RecalcularTrasladosYGuardarAsync(dia);
        Dias = new ObservableCollection<DiaCalendario>(SemanaActual.Dias);
        await CargarTareasYAreasAsync(_sesionService.UsuarioActual?.IdUsuario!);
    }

    public async Task EliminarTareaPermanentementeAsync(DiaCalendario dia, string bloqueId)
    {
        var bloque = _calendarioService.EliminarBloque(dia, bloqueId);
        if (bloque == null) return;

        if (bloque.Tipo == TipoBloqueCalendario.Tarea && bloque.IdTarea != null)
        {
            await _tareaRepo.EliminarTarea(bloque.IdTarea);
        }
        else if (bloque.Tipo == TipoBloqueCalendario.BloqueInteres && bloque.TareasInternas != null)
        {
            foreach (var sub in bloque.TareasInternas)
            {
                await _tareaRepo.EliminarTarea(sub.IdTarea);
            }
        }

        await RecalcularTrasladosYGuardarAsync(dia);
        Dias = new ObservableCollection<DiaCalendario>(SemanaActual.Dias);
        await CargarTareasYAreasAsync(_sesionService.UsuarioActual?.IdUsuario!);
    }

    [RelayCommand]
    private async Task CompletarTareaBloqueAsync(SubBloqueTarea sub)
    {
        if (sub == null || string.IsNullOrEmpty(sub.IdTarea)) return;

        var tarea = await _tareaRepo.ObtenerTareasPorUsuario(_sesionService.UsuarioActual?.IdUsuario!);
        var found = tarea.FirstOrDefault(t => t.IdTarea == sub.IdTarea);
        if (found == null) return;

        if (found.FecCompletado == null)
            await _tareaRepo.CompletarTarea(found.IdTarea!);
        else
            await _tareaRepo.DescompletarTarea(found.IdTarea!);

        sub.Completada = !sub.Completada;
        await CargarSemanaAsync();
    }

    public async Task CargarSemanaAsync()
    {
        if (_sesionService.UsuarioActual == null) return;

        IsLoading = true;
        try
        {
            var usuarioId = _sesionService.UsuarioActual.IdUsuario!;

            var semana = await _calendarioService.CargarSemanaAsync(FechaLunesActual, usuarioId);

            SemanaActual = semana;
            Dias = semana.Dias;

            NumeroSemana = $"Semana {semana.NumeroSemana}";
            FechaRango = $"{semana.FechaInicio:dd} \u2013 {semana.FechaFin:dd MMM yyyy}";

            await CargarTareasYAreasAsync(usuarioId);
        }
        catch (Exception ex)
        {
            NumeroSemana = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task CargarTareasYAreasAsync(string usuarioId)
    {
        var areas = await _areaRepo.ObtenerAreasPorUsuario(usuarioId);
        AreasDisponibles = new ObservableCollection<AreaInteres>(areas);

        var todasLasTareas = await _tareaRepo.ObtenerTareasPorUsuario(usuarioId);
        var tareasPanel = todasLasTareas
            .Where(t => t.FecCompletado == null
                     && !t.HoraInicio.HasValue
                     && !t.HoraFin.HasValue
                     && !t.FecInicio.HasValue)
            .ToList();

        TareasDisponibles = new ObservableCollection<Tarea>(tareasPanel);
        TareasSinAsignar = new ObservableCollection<Tarea>(tareasPanel.Where(t => string.IsNullOrEmpty(t.IdAreaInteres)));

        TareasPorArea.Clear();
        AreasConTareas.Clear();
        foreach (var area in areas)
        {
            var tareasArea = tareasPanel.Where(t => t.IdAreaInteres == area.IdAreaInteres).ToList();
            TareasPorArea[area.IdAreaInteres!] = new ObservableCollection<Tarea>(tareasArea);
            AreasConTareas.Add(new AreaConTareas
            {
                Area = area,
                Tareas = new ObservableCollection<Tarea>(tareasArea)
            });
        }

        var sinArea = tareasPanel.Where(t => string.IsNullOrEmpty(t.IdAreaInteres)).ToList();
        TareasPorArea["_sinArea"] = new ObservableCollection<Tarea>(sinArea);
        AreasConTareas.Add(new AreaConTareas
        {
            Area = new AreaInteres { Nombre = "Inbox", ColorHex = "#666666" },
            Tareas = new ObservableCollection<Tarea>(sinArea)
        });
    }

    private async Task RecalcularTrasladosYGuardarAsync(DiaCalendario dia)
    {
        if (_sesionService.UsuarioActual == null) return;

        await _calendarioService.GuardarCambiosAsync(SemanaActual, _sesionService.UsuarioActual.IdUsuario!);
    }

    private static DateTime GetLunes(DateTime fecha)
    {
        var diff = (7 + (fecha.DayOfWeek - DayOfWeek.Monday)) % 7;
        return fecha.AddDays(-diff);
    }
}