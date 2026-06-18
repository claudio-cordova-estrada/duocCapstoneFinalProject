using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using PlanificApp.Models.Enums;
using PlanificApp.Models.Repositories.Interfaces;
using PlanificApp.Models.Services.Interfaces;

namespace PlanificApp.Models.Services;

public class CalendarioSemanalService : ICalendarioSemanalService
{
    private readonly ITareaRepository _tareaRepo;
    private readonly IAreaInteresRepository _areaRepo;
    private readonly IUbicacionRepository _ubicacionRepo;
    private readonly ISesionService _sesionService;
    private readonly IGeoService _geoService;
    private readonly IBloqueAreaInteresScheduleRepository _bloqueAreaRepo;

    private static readonly TimeSpan DuracionMinima = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan MaxGapContiguos = TimeSpan.FromMinutes(120);
    private const int DefaultTravelMinutes = 15;

    private static readonly string[] NombresDias = { "Lun", "Mar", "Mi\u00e9", "Jue", "Vie", "S\u00e1b", "Dom" };

    public CalendarioSemanalService(
        ITareaRepository tareaRepo,
        IAreaInteresRepository areaRepo,
        IUbicacionRepository ubicacionRepo,
        ISesionService sesionService,
        IGeoService geoService,
        IBloqueAreaInteresScheduleRepository bloqueAreaRepo)
    {
        _tareaRepo = tareaRepo;
        _areaRepo = areaRepo;
        _ubicacionRepo = ubicacionRepo;
        _sesionService = sesionService;
        _geoService = geoService;
        _bloqueAreaRepo = bloqueAreaRepo;
    }

    public async Task<SemanaCalendario> CargarSemanaAsync(DateTime fechaLunes, string usuarioId)
    {
        var semana = new SemanaCalendario
        {
            NumeroSemana = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                fechaLunes, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday),
            FechaInicio = fechaLunes,
            FechaFin = fechaLunes.AddDays(6)
        };

        var semanaFin = fechaLunes.AddDays(7);

        var tareas = await _tareaRepo.ObtenerTareasPorRango(
            usuarioId, fechaLunes, semanaFin);
        var areas = await _areaRepo.ObtenerAreasPorUsuario(usuarioId);
        var ubicaciones = await _ubicacionRepo.ObtenerUbicacionesPorUsuario(usuarioId);
        var tareasDict = tareas.Where(t => t.IdTarea != null).ToDictionary(t => t.IdTarea!);
        var areaDict = areas.ToDictionary(a => a.IdAreaInteres!, a => a);
        var bloquesArea = await _bloqueAreaRepo.ObtenerPorUsuarioYRango(
            usuarioId, fechaLunes, semanaFin);
        var hoy = DateTime.Today;

        for (int i = 0; i < 7; i++)
        {
            var fecha = fechaLunes.AddDays(i);
            var dia = new DiaCalendario
            {
                Fecha = fecha,
                NombreDia = NombresDias[i],
                NumeroDia = fecha.Day,
                EsHoy = fecha.Date == hoy
            };

            var bloquesAreaDia = bloquesArea
                .Where(b => b.Fecha.Date == fecha.Date)
                .ToList();

            foreach (var ba in bloquesAreaDia)
            {
                var area = areas.FirstOrDefault(a => a.IdAreaInteres == ba.IdAreaInteres);
                dia.Bloques.Add(new BloqueCalendario
                {
                    Tipo = TipoBloqueCalendario.BloqueInteres,
                    Id = ba.Id!,
                    IdAreaInteres = ba.IdAreaInteres,
                    NombreArea = area?.Nombre ?? "\u00c1rea",
                    ColorHex = area?.ColorHex,
                    HoraInicio = ba.HoraInicio,
                    HoraFin = ba.HoraFin
                });
            }

            var tareasDia = tareas
                .Where(t => t.FecInicio.HasValue && t.FecInicio.Value.Date == fecha.Date
                         && t.HoraInicio.HasValue && t.HoraFin.HasValue)
                .OrderBy(t => t.HoraInicio)
                .ToList();

            foreach (var tarea in tareasDia)
            {
                if (tarea.IdTarea == null) continue;

                if (!string.IsNullOrEmpty(tarea.IdAreaInteres))
                {
                    var bloqueInteres = dia.Bloques.FirstOrDefault(b =>
                        b.Tipo == TipoBloqueCalendario.BloqueInteres
                        && b.IdAreaInteres == tarea.IdAreaInteres
                        && tarea.HoraInicio >= b.HoraInicio
                        && tarea.HoraFin <= b.HoraFin);

                    if (bloqueInteres != null)
                    {
                        bloqueInteres.TareasInternas ??= new ObservableCollection<SubBloqueTarea>();
                        bloqueInteres.TareasInternas.Add(new SubBloqueTarea
                        {
                            IdTarea = tarea.IdTarea,
                            Nombre = tarea.Nombre,
                            HoraInicio = tarea.HoraInicio!.Value,
                            HoraFin = tarea.HoraFin!.Value,
                            Completada = tarea.FecCompletado != null
                        });
                        continue;
                    }
                }

                var area = !string.IsNullOrEmpty(tarea.IdAreaInteres)
                    ? areaDict.GetValueOrDefault(tarea.IdAreaInteres)
                    : null;

                dia.Bloques.Add(new BloqueCalendario
                {
                    Tipo = TipoBloqueCalendario.Tarea,
                    IdTarea = tarea.IdTarea,
                    NombreTarea = tarea.Nombre,
                    HoraInicio = tarea.HoraInicio!.Value,
                    HoraFin = tarea.HoraFin!.Value,
                    IdAreaInteresPadre = tarea.IdAreaInteres,
                    ColorHex = area?.ColorHex ?? "#666666",
                    Completada = tarea.FecCompletado != null
                });
            }

            OrdenarBloques(dia);
            await CalcularTrasladosAsync(dia, usuarioId, ubicaciones, areas, tareasDict);
            semana.Dias.Add(dia);
        }

        return semana;
    }

    public BloqueCalendario AgregarBloqueInteres(DiaCalendario dia, AreaInteres area,
        TimeSpan horaInicio, TimeSpan horaFin)
    {
        var bloque = new BloqueCalendario
        {
            Tipo = TipoBloqueCalendario.BloqueInteres,
            IdAreaInteres = area.IdAreaInteres,
            NombreArea = area.Nombre,
            ColorHex = area.ColorHex,
            HoraInicio = horaInicio,
            HoraFin = horaFin,
            TareasInternas = new ObservableCollection<SubBloqueTarea>()
        };

        dia.Bloques.Add(bloque);
        OrdenarBloques(dia);
        return bloque;
    }

    public BloqueCalendario AgregarTarea(DiaCalendario dia, Tarea tarea,
        TimeSpan horaInicio, TimeSpan horaFin, List<AreaInteres>? areas = null)
    {
        string? colorHex = null;
        if (!string.IsNullOrEmpty(tarea.IdAreaInteres) && areas != null)
        {
            var area = areas.FirstOrDefault(a => a.IdAreaInteres == tarea.IdAreaInteres);
            colorHex = area?.ColorHex;
        }

        var bloque = new BloqueCalendario
        {
            Tipo = TipoBloqueCalendario.Tarea,
            IdTarea = tarea.IdTarea,
            NombreTarea = tarea.Nombre,
            HoraInicio = horaInicio,
            HoraFin = horaFin,
            IdAreaInteresPadre = tarea.IdAreaInteres,
            ColorHex = colorHex
        };

        dia.Bloques.Add(bloque);
        OrdenarBloques(dia);
        return bloque;
    }

    public BloqueCalendario AgregarTareaEnBloque(DiaCalendario dia, string bloqueInteresId,
        Tarea tarea, TimeSpan horaInicio, TimeSpan horaFin)
    {
        var bloque = dia.Bloques.FirstOrDefault(b => b.Id == bloqueInteresId
            && b.Tipo == TipoBloqueCalendario.BloqueInteres);
        if (bloque == null) return null!;

        if (horaInicio < bloque.HoraInicio) horaInicio = bloque.HoraInicio;

        if (horaFin > bloque.HoraFin)
            bloque.HoraFin = horaFin;

        if (horaFin - horaInicio < DuracionMinima) horaFin = horaInicio + DuracionMinima;

        var sub = new SubBloqueTarea
        {
            IdTarea = tarea.IdTarea!,
            Nombre = tarea.Nombre,
            HoraInicio = horaInicio,
            HoraFin = horaFin,
            Completada = tarea.FecCompletado != null
        };

        bloque.TareasInternas ??= new ObservableCollection<SubBloqueTarea>();
        bloque.TareasInternas.Add(sub);
        return bloque;
    }

    public BloqueCalendario? InsertarTareaEnBloqueInteres(DiaCalendario dia, string bloqueId,
        string idTarea, string nombre, TimeSpan horaInicio, TimeSpan horaFin, bool completada)
    {
        var bloque = dia.Bloques.FirstOrDefault(b => b.Id == bloqueId
            && b.Tipo == TipoBloqueCalendario.BloqueInteres);
        if (bloque == null) return null;

        var tareaBloque = dia.Bloques.FirstOrDefault(b => b.IdTarea == idTarea);
        if (tareaBloque != null)
            dia.Bloques.Remove(tareaBloque);

        if (horaInicio < bloque.HoraInicio) horaInicio = bloque.HoraInicio;
        if (horaFin > bloque.HoraFin) bloque.HoraFin = horaFin;
        if (horaFin - horaInicio < DuracionMinima) horaFin = horaInicio + DuracionMinima;

        var sub = new SubBloqueTarea
        {
            IdTarea = idTarea,
            Nombre = nombre,
            HoraInicio = horaInicio,
            HoraFin = horaFin,
            Completada = completada
        };

        bloque.TareasInternas ??= new ObservableCollection<SubBloqueTarea>();
        bloque.TareasInternas.Add(sub);
        OrdenarBloques(dia);
        return bloque;
    }

    public void MoverBloque(DiaCalendario dia, string bloqueId, TimeSpan nuevaHoraInicio)
    {
        var bloque = dia.Bloques.FirstOrDefault(b => b.Id == bloqueId);
        if (bloque == null) return;

        var duracion = bloque.HoraFin - bloque.HoraInicio;
        bloque.HoraInicio = nuevaHoraInicio;
        bloque.HoraFin = nuevaHoraInicio + duracion;
        OrdenarBloques(dia);
    }

    public void RedimensionarBloque(DiaCalendario dia, string bloqueId,
        TimeSpan nuevaHoraInicio, TimeSpan nuevaHoraFin)
    {
        var bloque = dia.Bloques.FirstOrDefault(b => b.Id == bloqueId);
        if (bloque == null) return;

        if (nuevaHoraFin - nuevaHoraInicio < DuracionMinima) return;

        bloque.HoraInicio = nuevaHoraInicio;
        bloque.HoraFin = nuevaHoraFin;

        if (bloque.Tipo == TipoBloqueCalendario.BloqueInteres && bloque.TareasInternas != null)
        {
            foreach (var sub in bloque.TareasInternas)
            {
                if (sub.HoraInicio < bloque.HoraInicio) sub.HoraInicio = bloque.HoraInicio;
                if (sub.HoraFin > bloque.HoraFin) sub.HoraFin = bloque.HoraFin;
                if (sub.HoraFin - sub.HoraInicio < DuracionMinima)
                    sub.HoraFin = sub.HoraInicio + DuracionMinima;
            }
        }

        OrdenarBloques(dia);
    }

    public BloqueCalendario? EliminarBloque(DiaCalendario dia, string bloqueId)
    {
        var bloque = dia.Bloques.FirstOrDefault(b => b.Id == bloqueId);
        if (bloque == null) return null;
        dia.Bloques.Remove(bloque);
        return bloque;
    }

    public async Task CalcularTrasladosAsync(DiaCalendario dia, string usuarioId)
    {
        var ubicaciones = await _ubicacionRepo.ObtenerUbicacionesPorUsuario(usuarioId);
        var areas = await _areaRepo.ObtenerAreasPorUsuario(usuarioId);
        var tareas = await _tareaRepo.ObtenerTareasPorUsuario(usuarioId);
        var tareasDict = tareas.Where(t => t.IdTarea != null).ToDictionary(t => t.IdTarea!);
        await CalcularTrasladosAsync(dia, usuarioId, ubicaciones, areas, tareasDict);
    }

    public async Task GuardarCambiosAsync(SemanaCalendario semana, string usuarioId)
    {
        var todasLasTareas = await _tareaRepo.ObtenerTareasPorUsuario(usuarioId);
        var tareasDict = todasLasTareas
            .Where(t => t.IdTarea != null)
            .ToDictionary(t => t.IdTarea!);

        await _bloqueAreaRepo.EliminarBloquesPorSemana(usuarioId, semana.FechaInicio, semana.FechaFin.AddDays(1));

        foreach (var dia in semana.Dias)
        {
            foreach (var bloque in dia.Bloques)
            {
                if (bloque.Tipo == TipoBloqueCalendario.BloqueInteres)
                {
                    var schedule = new BloqueAreaInteresSchedule
                    {
                        Id = bloque.Id,
                        IdAreaInteres = bloque.IdAreaInteres,
                        IdUsuario = usuarioId,
                        Fecha = dia.Fecha,
                        HoraInicio = bloque.HoraInicio,
                        HoraFin = bloque.HoraFin
                    };
                    await _bloqueAreaRepo.CrearBloque(schedule);

                    if (bloque.TareasInternas != null)
                    {
                        foreach (var sub in bloque.TareasInternas)
                        {
                            if (tareasDict.TryGetValue(sub.IdTarea, out var tarea))
                            {
                                tarea.HoraInicio = sub.HoraInicio;
                                tarea.HoraFin = sub.HoraFin;
                                tarea.FecInicio = dia.Fecha;
                                await _tareaRepo.ActualizarTarea(tarea.IdTarea!, tarea);
                            }
                        }
                    }
                }
                else if (bloque.Tipo == TipoBloqueCalendario.Tarea
                    && bloque.IdTarea != null)
                {
                    if (tareasDict.TryGetValue(bloque.IdTarea, out var tarea))
                    {
                        tarea.HoraInicio = bloque.HoraInicio;
                        tarea.HoraFin = bloque.HoraFin;
                        tarea.FecInicio = dia.Fecha;
                        await _tareaRepo.ActualizarTarea(tarea.IdTarea!, tarea);
                    }
                }
            }
        }
    }

    private async Task CalcularTrasladosAsync(DiaCalendario dia, string usuarioId,
        List<UbicacionGuardada> ubicaciones, List<AreaInteres> areas,
        Dictionary<string, Tarea>? tareasDict = null)
    {
        var existingTraslados = dia.Bloques
            .Where(b => b.Tipo == TipoBloqueCalendario.SeccionTraslado)
            .ToDictionary(
                b => $"{b.UbicacionOrigen}|{b.UbicacionDestino}|{b.MetodoTransporte}",
                b => (b.DuracionMinutos ?? DefaultTravelMinutes, b.EsEstimado));

        var trasladosExistentes = dia.Bloques
            .Where(b => b.Tipo == TipoBloqueCalendario.SeccionTraslado)
            .ToList();
        foreach (var t in trasladosExistentes)
            dia.Bloques.Remove(t);

        var bloquesOrdenados = dia.Bloques
            .Where(b => b.Tipo != TipoBloqueCalendario.SeccionTraslado)
            .OrderBy(b => b.HoraInicio)
            .ToList();

        if (!bloquesOrdenados.Any()) return;

        var ubicacionActual = _sesionService.UsuarioActual?.UbicacionActual?.Trim() ?? "Casa";
        var areaDict = areas.ToDictionary(a => a.IdAreaInteres!, a => a);
        var ubicaDict = ubicaciones
            .GroupBy(u => u.Nombre, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First());
        var areaNombreToUbicacionNombre = ubicaciones
            .Where(u => !string.IsNullOrEmpty(u.AreaInteres) && !string.IsNullOrEmpty(u.Nombre))
            .GroupBy(u => u.AreaInteres, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Nombre, StringComparer.OrdinalIgnoreCase);

        UbicacionGuardada? origenCache = null;
        var firstUbicacion = FindIgnoreCase(ubicaDict, ubicacionActual);
        if (firstUbicacion != null)
            origenCache = firstUbicacion;

        for (int i = 0; i < bloquesOrdenados.Count; i++)
        {
            var bloque = bloquesOrdenados[i];
            var ubicacionBloque = ObtenerUbicacionBloque(bloque, areaDict, tareasDict, areaNombreToUbicacionNombre);

            if (ubicacionBloque == null)
            {
                System.Diagnostics.Debug.WriteLine($"[TRASLADO] Bloque '{bloque.NombreTarea ?? bloque.NombreArea}' sin ubicación, skip");
                continue;
            }

            ubicacionBloque = ubicacionBloque.Trim();

            if (ubicacionBloque.Equals(ubicacionActual, StringComparison.OrdinalIgnoreCase))
            {
                System.Diagnostics.Debug.WriteLine($"[TRASLADO] Bloque '{bloque.NombreTarea ?? bloque.NombreArea}' en ubicación actual '{ubicacionActual}', skip");
                ubicacionActual = ubicacionBloque;
                continue;
            }

            var metodoTransporte = ObtenerTransporteBloque(bloque, areaDict, tareasDict, ubicaDict, areaNombreToUbicacionNombre);

            if (!metodoTransporte.HasValue)
            {
                System.Diagnostics.Debug.WriteLine($"[TRASLADO] Bloque '{bloque.NombreTarea ?? bloque.NombreArea}' sin método de transporte, skip");
                ubicacionActual = ubicacionBloque;
                continue;
            }

            System.Diagnostics.Debug.WriteLine($"[TRASLADO] Bloque '{bloque.NombreTarea ?? bloque.NombreArea}': '{ubicacionActual}' -> '{ubicacionBloque}', transporte={metodoTransporte.Value}");

            var origenUbic = FindIgnoreCase(ubicaDict, ubicacionActual);
            var destinoUbic = FindIgnoreCase(ubicaDict, ubicacionBloque);

            if (origenUbic == null && destinoUbic != null && origenCache != null)
                origenUbic = origenCache;

            if (origenUbic == null && destinoUbic != null)
            {
                var firstSaved = ubicaciones.FirstOrDefault();
                if (firstSaved != null)
                    origenUbic = firstSaved;
            }

            if (origenUbic == null || destinoUbic == null)
            {
                System.Diagnostics.Debug.WriteLine($"[TRASLADO] No se encontraron coordenadas: origen={ubicacionActual} ({origenUbic?.Nombre ?? "null"}) destino={ubicacionBloque} ({destinoUbic?.Nombre ?? "null"}), usando estimado {DefaultTravelMinutes}min");
                InsertarTrasladoEstimado(dia, bloque, ubicacionActual, ubicacionBloque, metodoTransporte.Value);
                ubicacionActual = ubicacionBloque;
                continue;
            }

            var transporteStr = MetodoTransporteToGeoString(metodoTransporte.Value);
            var modoGoogle = transporteStr switch
            {
                "A pie" => "WALK",
                "Bus" => "TRANSIT",
                _ => "DRIVE"
            };

            var cacheKey = $"{ubicacionActual}|{ubicacionBloque}|{metodoTransporte.Value}";
            int duracionMin;
            bool estimado;

            if (existingTraslados.TryGetValue(cacheKey, out var cached))
            {
                duracionMin = cached.Item1;
                estimado = cached.Item2;
            }
            else
            {
                try
                {
                    var resultado = await _geoService.CalcularRutaGoogleAsync(
                        origenUbic.Latitud, origenUbic.Longitud,
                        destinoUbic.Latitud, destinoUbic.Longitud,
                        modoGoogle);

                    if (resultado.HasValue)
                    {
                        duracionMin = ParseDurationMinutes(resultado.Value.Tiempo);
                        estimado = duracionMin <= 0;
                        if (estimado) duracionMin = DefaultTravelMinutes;
                    }
                    else
                    {
                        duracionMin = DefaultTravelMinutes;
                        estimado = true;
                    }
                }
                catch
                {
                    duracionMin = DefaultTravelMinutes;
                    estimado = true;
                }
            }

            var horaFinAnterior = i == 0
                ? TimeSpan.Zero
                : bloquesOrdenados[i - 1].HoraFin + TimeSpan.FromMinutes(15);

            var horaInicioTraslado = bloque.HoraInicio - TimeSpan.FromMinutes(duracionMin);
            if (horaInicioTraslado < horaFinAnterior)
                horaInicioTraslado = horaFinAnterior;

            if (bloque.HoraInicio - horaInicioTraslado < TimeSpan.FromMinutes(DuracionMinima.TotalMinutes))
            {
                horaInicioTraslado = bloque.HoraInicio - TimeSpan.FromMinutes(DuracionMinima.TotalMinutes);
                if (horaInicioTraslado < TimeSpan.Zero) horaInicioTraslado = TimeSpan.Zero;
                duracionMin = (int)(bloque.HoraInicio - horaInicioTraslado).TotalMinutes;
            }

            var traslado = new BloqueCalendario
            {
                Tipo = TipoBloqueCalendario.SeccionTraslado,
                HoraInicio = horaInicioTraslado,
                HoraFin = bloque.HoraInicio,
                UbicacionOrigen = ubicacionActual,
                UbicacionDestino = ubicacionBloque,
                MetodoTransporte = metodoTransporte,
                DuracionMinutos = duracionMin,
                EsEstimado = estimado,
                ColorHex = "#ef4444"
            };

            dia.Bloques.Add(traslado);
            ubicacionActual = ubicacionBloque;
            origenCache = destinoUbic;
        }

        OrdenarBloques(dia);
        ResolveTrasladoOverlaps(dia);
    }

    private string? ObtenerUbicacionBloque(BloqueCalendario bloque,
        Dictionary<string, AreaInteres> areaDict,
        Dictionary<string, Tarea>? tareasDict = null,
        Dictionary<string, string>? areaNombreToUbicacionNombre = null)
    {
        if (bloque.Tipo == TipoBloqueCalendario.BloqueInteres && bloque.IdAreaInteres != null)
        {
            if (areaDict.TryGetValue(bloque.IdAreaInteres, out var area)
                && !string.IsNullOrEmpty(area.UbicacionPred))
                return area.UbicacionPred;

            if (bloque.TareasInternas != null)
            {
                foreach (var sub in bloque.TareasInternas)
                {
                    if (sub.IdTarea != null && tareasDict != null
                        && tareasDict.TryGetValue(sub.IdTarea, out var tarea)
                        && !string.IsNullOrEmpty(tarea.Ubicacion))
                        return tarea.Ubicacion;

                    if (sub.IdTarea != null && tareasDict != null
                        && tareasDict.TryGetValue(sub.IdTarea, out var tarea2)
                        && !string.IsNullOrEmpty(tarea2.IdAreaInteres)
                        && areaDict.TryGetValue(tarea2.IdAreaInteres, out var subArea)
                        && !string.IsNullOrEmpty(subArea.UbicacionPred))
                        return subArea.UbicacionPred;
                }
            }

            if (areaDict.TryGetValue(bloque.IdAreaInteres, out var area2))
            {
                if (areaNombreToUbicacionNombre != null
                    && areaNombreToUbicacionNombre.TryGetValue(area2.Nombre, out var ubicNombre))
                    return ubicNombre;
                return area2.Nombre;
            }

            return null;
        }

        if (bloque.Tipo == TipoBloqueCalendario.Tarea)
        {
            if (bloque.IdAreaInteresPadre != null
                && areaDict.TryGetValue(bloque.IdAreaInteresPadre, out var area)
                && !string.IsNullOrEmpty(area.UbicacionPred))
                return area.UbicacionPred;

            if (bloque.IdTarea != null && tareasDict != null
                && tareasDict.TryGetValue(bloque.IdTarea, out var tarea)
                && !string.IsNullOrEmpty(tarea.Ubicacion))
                return tarea.Ubicacion;

            if (bloque.IdAreaInteresPadre != null
                && areaDict.TryGetValue(bloque.IdAreaInteresPadre, out var area3))
            {
                if (areaNombreToUbicacionNombre != null
                    && areaNombreToUbicacionNombre.TryGetValue(area3.Nombre, out var ubicNombre2))
                    return ubicNombre2;
                return area3.Nombre;
            }
        }

        return null;
    }

    private MetodoTransporte? ObtenerTransporteBloque(BloqueCalendario bloque,
        Dictionary<string, AreaInteres> areaDict,
        Dictionary<string, Tarea>? tareasDict = null,
        Dictionary<string, UbicacionGuardada>? ubicaDict = null,
        Dictionary<string, string>? areaNombreToUbicacionNombre = null)
    {
        var ubicacion = ObtenerUbicacionBloque(bloque, areaDict, tareasDict, areaNombreToUbicacionNombre);

        if (ubicacion != null && ubicaDict != null
            && ubicaDict.TryGetValue(ubicacion, out var ubicacionGuardada)
            && !string.IsNullOrEmpty(ubicacionGuardada.TransportePreferido))
        {
            var fromUbicacion = TransportePreferidoToEnum(ubicacionGuardada.TransportePreferido);
            if (fromUbicacion.HasValue)
                return fromUbicacion;
        }

        if (bloque.Tipo == TipoBloqueCalendario.BloqueInteres && bloque.IdAreaInteres != null)
        {
            if (areaDict.TryGetValue(bloque.IdAreaInteres, out var area)
                && area.MetodoTransportePred.HasValue)
                return area.MetodoTransportePred;
        }

        if (bloque.Tipo == TipoBloqueCalendario.Tarea)
        {
            if (bloque.IdAreaInteresPadre != null
                && areaDict.TryGetValue(bloque.IdAreaInteresPadre, out var area)
                && area.MetodoTransportePred.HasValue)
                return area.MetodoTransportePred;

            if (bloque.IdTarea != null && tareasDict != null
                && tareasDict.TryGetValue(bloque.IdTarea, out var tarea)
                && tarea.MetodoTransporte.HasValue)
                return tarea.MetodoTransporte;
        }

        return null;
    }

    private static MetodoTransporte? TransportePreferidoToEnum(string transporte)
    {
        return transporte?.Trim().ToLowerInvariant() switch
        {
            "a pie" => MetodoTransporte.Caminar,
            "caminar" => MetodoTransporte.Caminar,
            "auto" => MetodoTransporte.Auto,
            "automovil" => MetodoTransporte.Auto,
            "bus" => MetodoTransporte.Bus,
            "micro" => MetodoTransporte.Bus,
            "metro" => MetodoTransporte.Bus,
            "taxi" => MetodoTransporte.Auto,
            "bicicleta" => MetodoTransporte.Caminar,
            _ => null
        };
    }

    private void InsertarTrasladoEstimado(DiaCalendario dia, BloqueCalendario bloqueSiguiente,
        string origen, string destino, MetodoTransporte metodoTransporte)
    {
        var duracion = TimeSpan.FromMinutes(DefaultTravelMinutes);
        if (duracion < DuracionMinima) duracion = DuracionMinima;

        var horaInicio = bloqueSiguiente.HoraInicio - duracion;
        if (horaInicio < TimeSpan.Zero) horaInicio = TimeSpan.Zero;

        if (bloqueSiguiente.HoraInicio - horaInicio < DuracionMinima)
        {
            horaInicio = bloqueSiguiente.HoraInicio - DuracionMinima;
            if (horaInicio < TimeSpan.Zero) horaInicio = TimeSpan.Zero;
        }

        dia.Bloques.Add(new BloqueCalendario
        {
            Tipo = TipoBloqueCalendario.SeccionTraslado,
            HoraInicio = horaInicio,
            HoraFin = bloqueSiguiente.HoraInicio,
            UbicacionOrigen = origen,
            UbicacionDestino = destino,
            DuracionMinutos = (int)(bloqueSiguiente.HoraInicio - horaInicio).TotalMinutes,
            EsEstimado = true,
            MetodoTransporte = metodoTransporte,
            ColorHex = "#ef4444"
        });
    }

    private List<List<Tarea>> AgruparContiguas(List<Tarea> tareas)
    {
        if (!tareas.Any()) return new List<List<Tarea>>();

        var ordenadas = tareas.OrderBy(t => t.HoraInicio).ToList();
        var grupos = new List<List<Tarea>>();
        var grupoActual = new List<Tarea> { ordenadas[0] };

        for (int i = 1; i < ordenadas.Count; i++)
        {
            var anterior = grupoActual.Last();
            var actual = ordenadas[i];

            if (actual.HoraInicio.HasValue && anterior.HoraFin.HasValue
                && actual.HoraInicio.Value - anterior.HoraFin.Value <= MaxGapContiguos)
            {
                grupoActual.Add(actual);
            }
            else
            {
                grupos.Add(grupoActual);
                grupoActual = new List<Tarea> { actual };
            }
        }

        grupos.Add(grupoActual);
        return grupos;
    }

    private int ParseDurationMinutes(string? durationText)
    {
        if (string.IsNullOrEmpty(durationText)) return -1;
        if (durationText.Contains("No disponible")) return -1;

        var totalMinutes = 0;
        var lower = durationText.ToLowerInvariant();
        var tokens = lower.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < tokens.Length - 1; i++)
        {
            if (!int.TryParse(tokens[i], out int value)) continue;
            var next = tokens[i + 1];

            if (next.StartsWith("hour") || next.StartsWith("h"))
                totalMinutes += value * 60;
            else if (next.StartsWith("min"))
                totalMinutes += value;
            else if (next.StartsWith("hora"))
                totalMinutes += value * 60;
            else if (next.StartsWith("minuto"))
                totalMinutes += value;
        }

        if (totalMinutes == 0 && int.TryParse(tokens.FirstOrDefault(), out int singleValue))
        {
            if (lower.Contains("hour") || lower.Contains("hora"))
                totalMinutes = singleValue * 60;
            else if (lower.Contains("min"))
                totalMinutes = singleValue;
        }

        return totalMinutes > 0 ? totalMinutes : -1;
    }

    private static string MetodoTransporteToGeoString(MetodoTransporte metodo) => metodo switch
    {
        MetodoTransporte.Caminar => "A pie",
        MetodoTransporte.Auto => "Auto",
        MetodoTransporte.Bus => "Bus",
        _ => "Auto"
    };

    public BloqueCalendario? FindInteresBlockAtTime(DiaCalendario dia, TimeSpan horaInicio)
    {
        return dia.Bloques
            .FirstOrDefault(b => b.Tipo == TipoBloqueCalendario.BloqueInteres
                && horaInicio >= b.HoraInicio && horaInicio < b.HoraFin);
    }

    public void MoverSubTarea(DiaCalendario dia, string parentBloqueId, string idTarea, TimeSpan nuevaInicio, TimeSpan nuevaFin)
    {
        var parent = dia.Bloques.FirstOrDefault(b => b.Id == parentBloqueId);
        if (parent?.TareasInternas == null) return;

        var sub = parent.TareasInternas.FirstOrDefault(s => s.IdTarea == idTarea);
        if (sub == null) return;

        if (nuevaInicio < parent.HoraInicio) nuevaInicio = parent.HoraInicio;
        if (nuevaFin > parent.HoraFin) nuevaFin = parent.HoraFin;
        if (nuevaFin - nuevaInicio < DuracionMinima) nuevaFin = nuevaInicio + DuracionMinima;

        sub.HoraInicio = nuevaInicio;
        sub.HoraFin = nuevaFin;
    }

    public SubBloqueTarea? ExtraerSubTarea(DiaCalendario dia, string parentBloqueId, string idTarea)
    {
        var parent = dia.Bloques.FirstOrDefault(b => b.Id == parentBloqueId);
        if (parent?.TareasInternas == null) return null;

        var sub = parent.TareasInternas.FirstOrDefault(s => s.IdTarea == idTarea);
        if (sub == null) return null;

        parent.TareasInternas.Remove(sub);
        return sub;
    }

    public void EliminarSubTarea(DiaCalendario dia, string parentBloqueId, string idTarea)
    {
        var parent = dia.Bloques.FirstOrDefault(b => b.Id == parentBloqueId);
        if (parent?.TareasInternas == null) return;

        var sub = parent.TareasInternas.FirstOrDefault(s => s.IdTarea == idTarea);
        if (sub != null) parent.TareasInternas.Remove(sub);
    }

    private static void OrdenarBloques(DiaCalendario dia)
    {
        var ordenados = dia.Bloques.OrderBy(b => b.HoraInicio).ToList();
        dia.Bloques.Clear();
        foreach (var b in ordenados) dia.Bloques.Add(b);
    }

    private void ResolveTrasladoOverlaps(DiaCalendario dia)
    {
        var traslados = dia.Bloques
            .Where(b => b.Tipo == TipoBloqueCalendario.SeccionTraslado)
            .OrderBy(b => b.HoraInicio)
            .ToList();

        foreach (var traslado in traslados)
        {
            foreach (var bloque in dia.Bloques.ToList())
            {
                if (bloque.Tipo == TipoBloqueCalendario.SeccionTraslado) continue;
                if (bloque.HoraFin <= traslado.HoraInicio) continue;
                if (bloque.HoraInicio >= traslado.HoraFin) continue;

                if (bloque.Tipo == TipoBloqueCalendario.BloqueInteres)
                {
                    bloque.HoraInicio = traslado.HoraFin;
                }
                else
                {
                    var duracion = bloque.HoraFin - bloque.HoraInicio;
                    bloque.HoraInicio = traslado.HoraFin;
                    bloque.HoraFin = traslado.HoraFin + duracion;
                }
            }
        }

        OrdenarBloques(dia);
    }

private static UbicacionGuardada? FindIgnoreCase(Dictionary<string, UbicacionGuardada> dict, string key)
    {
        if (string.IsNullOrEmpty(key)) return null;

        if (dict.TryGetValue(key, out var exact)) return exact;

        var found = dict.Keys.FirstOrDefault(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
        if (found != null) return dict[found];

        var partial = dict.Keys.FirstOrDefault(k =>
            k.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0 ||
            key.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);
        return partial != null ? dict[partial] : null;
    }
}