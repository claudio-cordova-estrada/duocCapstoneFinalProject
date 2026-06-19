using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using PlanificApp.Models.Enums;
using PlanificApp.Models.Repositories.Interfaces;
using PlanificApp.Models.Services.Interfaces;

namespace PlanificApp.Models.Services;

public class GeneradorSemanalService : IGeneradorSemanalService
{
    private readonly ITareaRepository _tareaRepo;
    private readonly ISesionService _sesionService;
    private readonly ICalendarioSemanalService _calendarioService;

    public GeneradorSemanalService(ITareaRepository tareaRepo, ISesionService sesionService,
        ICalendarioSemanalService calendarioService)
    {
        _tareaRepo = tareaRepo;
        _sesionService = sesionService;
        _calendarioService = calendarioService;
    }

    private static readonly string[] NombresDias = { "Lun", "Mar", "Mi\u00e9", "Jue", "Vie", "S\u00e1b", "Dom" };
    private const double MinBloqueHoras = 0.5;
    private const double MinTareaHoras = 0.5;
    private static readonly TimeSpan GapEntreBloques = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan GapEntreTareas = TimeSpan.FromMinutes(15);

    public async Task<List<PropuestaGeneracion>> GenerarPropuestasAsync(CondicionesGeneracion condiciones)
    {
        var tareasElegibles = await ObtenerTareasElegiblesAsync(condiciones);
        var random = new Random();

        var usuarioId = _sesionService.UsuarioActual?.IdUsuario ?? "";
        var ubicacionInicio = _sesionService.UsuarioActual?.UbicacionActual?.Trim() ?? "Casa";

        var propuestaEq = await GenerarEquilibrio(condiciones, random, tareasElegibles, usuarioId, ubicacionInicio);
        var propuestaRush = await GenerarIntensiva(condiciones, random, tareasElegibles, usuarioId, ubicacionInicio);
        var propuestaRel = await GenerarRelajado(condiciones, random, tareasElegibles, usuarioId, ubicacionInicio);

        return new List<PropuestaGeneracion> { propuestaEq, propuestaRush, propuestaRel };
    }

    private async Task<List<Tarea>> ObtenerTareasElegiblesAsync(CondicionesGeneracion condiciones)
    {
        var usuarioId = _sesionService.UsuarioActual?.IdUsuario;
        if (string.IsNullOrEmpty(usuarioId)) return new List<Tarea>();

        var idsAreas = condiciones.AreasConsiderar
            .Where(a => a.IdAreaInteres != null)
            .Select(a => a.IdAreaInteres!).ToHashSet();

        var todasLasTareas = await _tareaRepo.ObtenerTareasPorUsuario(usuarioId);

        return todasLasTareas
            .Where(t => !string.IsNullOrEmpty(t.IdAreaInteres) && idsAreas.Contains(t.IdAreaInteres))
            .Where(t => !t.FecCompletado.HasValue)
            .Where(t => t.HoraInicio == null && t.HoraFin == null)
            .Where(t => !t.FecLimite.HasValue || t.FecLimite.Value >= condiciones.FechaInicio)
            .Where(t => t.UsoGeneracion != true)
            .ToList();
    }

    private TimeSpan CalcularHoraInicioDia(DateTime fecha, TimeSpan horaInicioJornada)
    {
        if (fecha.Date < DateTime.Today)
            return horaInicioJornada;

        if (fecha.Date == DateTime.Today)
        {
            var minInicio = DateTime.Now.TimeOfDay + TimeSpan.FromHours(1);
            var redondeado = TimeSpan.FromMinutes(Math.Ceiling(minInicio.TotalMinutes / 15) * 15);
            return redondeado < horaInicioJornada ? horaInicioJornada : redondeado;
        }

        return horaInicioJornada;
    }

    private List<DiaCalendario> FiltrarDiasFuturos(List<DiaCalendario> todosLosDias)
    {
        var hoy = DateTime.Today;
        var resultado = todosLosDias.Where(d => d.Fecha.Date >= hoy).ToList();
        if (resultado.Count == 0)
            resultado = todosLosDias.Take(1).ToList();
        return resultado;
    }

    /// After placing a block, advance the cursor by the gap time.
    private TimeSpan AvanzarCursorConGap(TimeSpan cursor, TimeSpan duracionBloque)
    {
        return cursor + duracionBloque + GapEntreBloques;
    }

#region Equilibrio — spread across ALL selected days, 1-4h per area per day

    private async Task<PropuestaGeneracion> GenerarEquilibrio(CondicionesGeneracion condiciones, Random random,
        List<Tarea> tareasElegibles, string usuarioId, string ubicacionInicio)
    {
        var propuesta = CrearPropuestaBase(TipoPropuesta.Equilibrio, "Equilibrio",
            "Distribuye las actividades de forma pareja entre los d\u00edas seleccionados",
            condiciones, 1);

        if (condiciones.DiasSeleccionados.Count < 1)
        {
            propuesta.EsValida = false;
            propuesta.MensajeInvalidacion = "Se requiere al menos 1 d\u00eda seleccionado";
            return propuesta;
        }
        if (condiciones.HorasFuncionales <= 0)
        {
            propuesta.EsValida = false;
            propuesta.MensajeInvalidacion = "No hay horas funcionales disponibles";
            return propuesta;
        }

        var todosLosDias = ObtenerDiasGeneracion(condiciones);
        var diasGeneracion = FiltrarDiasFuturos(todosLosDias);
        var horaInicioJornada = ObtenerHoraInicioJornada(condiciones);
        var horaFin = ObtenerHoraFinJornada(condiciones);
        double horasJornada = (horaFin - horaInicioJornada).TotalHours;
        double horasFuncPorDia = Math.Max(MinBloqueHoras, condiciones.HorasFuncionales / diasGeneracion.Count);
        // Equilibrio: allow multiple areas per day, cap total at functional hours * 2 or jornada * 0.85
        double maxHorasPorDia = Math.Min(horasFuncPorDia * 2.0, horasJornada * 0.85);
        if (maxHorasPorDia < horasFuncPorDia) maxHorasPorDia = horasFuncPorDia;
        var areasOrdenadas = condiciones.AreasPriorizadas.ToList();

        // Track per-day cursor and which areas already placed
        var cursorPorDia = diasGeneracion.ToDictionary(d => d.Fecha,
            d => CalcularHoraInicioDia(d.Fecha, horaInicioJornada));
        var ubicacionPorDia = diasGeneracion.ToDictionary(d => d.Fecha, d => ubicacionInicio);
        var horasUsadasPorDia = diasGeneracion.ToDictionary(d => d.Fecha, d => 0.0);
        var areasPorDia = diasGeneracion.ToDictionary(d => d.Fecha, d => new HashSet<string>());
        var tareasUsadasPorArea = condiciones.AreasPriorizadas
            .Where(aP => aP.Area.IdAreaInteres != null)
            .ToDictionary(aP => aP.Area.IdAreaInteres!, _ => new HashSet<string>());

        foreach (var areaP in areasOrdenadas)
        {
            var area = areaP.Area;
            double horasSemanales = area.HorasSemanales > 0 ? area.HorasSemanales : 3.0;
            double horasAreaRestantes = horasSemanales;

            // Each area spreads across all days: block = weeklyHours / numDays, clamped 1-4h
            double objetivoPorDia = horasSemanales / diasGeneracion.Count;
            if (objetivoPorDia > 4.0) objetivoPorDia = 4.0;
            if (objetivoPorDia < 1.0) objetivoPorDia = 1.0;

            var tareasArea = tareasElegibles.Where(t => t.IdAreaInteres == area.IdAreaInteres).ToList();

            foreach (var dia in diasGeneracion)
            {
                if (horasAreaRestantes < MinBloqueHoras) break;
                // Skip if this area already has a block on this day
                if (areasPorDia[dia.Fecha].Contains(area.IdAreaInteres!)) continue;
                if (horasUsadasPorDia[dia.Fecha] >= maxHorasPorDia) continue;

                double disponibleEnDia = maxHorasPorDia - horasUsadasPorDia[dia.Fecha];
                double bloqueHoras = Math.Min(objetivoPorDia, disponibleEnDia);
                bloqueHoras = Math.Min(bloqueHoras, horasAreaRestantes);

                // If the remaining area time is too small for a full block, place it all
                if (bloqueHoras < 1.0 && horasAreaRestantes >= MinBloqueHoras)
                    bloqueHoras = horasAreaRestantes;

                // Reservamos el viaje real desde la ubicación actual del día hasta esta área,
                // para que el bloque arranque después del traslado (que A2 dibujará en ese hueco).
                var viajeMin = await _calendarioService.ObtenerMinutosTrasladoAsync(
                    usuarioId, ubicacionPorDia[dia.Fecha], area.UbicacionPred, area.MetodoTransportePred);
                var horaDisponible = cursorPorDia[dia.Fecha] + TimeSpan.FromMinutes(viajeMin);

                // Don't place if doesn't fit before end of day + gap
                if (horaDisponible + TimeSpan.FromHours(bloqueHoras) + GapEntreBloques > horaFin)
                {
                    bloqueHoras = (horaFin - horaDisponible).TotalHours - GapEntreBloques.TotalHours;
                    if (bloqueHoras < MinBloqueHoras) continue;
                }

                if (bloqueHoras < MinBloqueHoras) continue;

                // El bloque se coloca: la ubicación actual del día pasa a ser la de esta área.
                if (!string.IsNullOrEmpty(area.UbicacionPred))
                    ubicacionPorDia[dia.Fecha] = area.UbicacionPred;

                var bloque = new BloqueCalendario
                {
                    Tipo = TipoBloqueCalendario.BloqueInteres,
                    IdAreaInteres = area.IdAreaInteres,
                    NombreArea = area.Nombre,
                    ColorHex = area.ColorHex,
                    HoraInicio = horaDisponible,
                    HoraFin = horaDisponible + TimeSpan.FromHours(bloqueHoras),
                    TareasInternas = new ObservableCollection<SubBloqueTarea>()
                };

                LlenarBloqueConTareas(bloque, tareasArea, random, GapEntreTareas,
                    tareasUsadasPorArea.GetValueOrDefault(area.IdAreaInteres!));

                var diaSemana = propuesta.Semana.Dias.FirstOrDefault(d => d.Fecha.Date == dia.Fecha.Date);
                if (diaSemana != null)
                {
                    diaSemana.Bloques.Add(bloque);
                    OrdenarBloques(diaSemana);
                }

                cursorPorDia[dia.Fecha] = AvanzarCursorConGap(horaDisponible, TimeSpan.FromHours(bloqueHoras));
                horasUsadasPorDia[dia.Fecha] += bloqueHoras;
                horasAreaRestantes -= bloqueHoras;
                areasPorDia[dia.Fecha].Add(area.IdAreaInteres!);
            }
        }

        ContarEstadisticas(propuesta);
        return propuesta;
    }

    #endregion

    #region Intensiva

    private async Task<PropuestaGeneracion> GenerarIntensiva(CondicionesGeneracion condiciones, Random random,
        List<Tarea> tareasElegibles, string usuarioId, string ubicacionInicio)
    {
        var propuesta = CrearPropuestaBase(TipoPropuesta.Rushear, "Intensiva",
            "Concentra las actividades en la menor cantidad de d\u00edas posible",
            condiciones, 2);

        if (condiciones.DiasSeleccionados.Count < 2)
        {
            propuesta.EsValida = false;
            propuesta.MensajeInvalidacion = "Se requieren al menos 2 d\u00edas seleccionados";
            return propuesta;
        }
        if (condiciones.HorasFuncionales <= 0)
        {
            propuesta.EsValida = false;
            propuesta.MensajeInvalidacion = "No hay horas funcionales disponibles";
            return propuesta;
        }

        var todosLosDias = ObtenerDiasGeneracion(condiciones);
        var diasFuturos = FiltrarDiasFuturos(todosLosDias);
        var horaInicioJornada = ObtenerHoraInicioJornada(condiciones);
        var horaFin = ObtenerHoraFinJornada(condiciones);

        int diasAUsar = Math.Min(2, diasFuturos.Count);
        var diasSeleccionados = diasFuturos.Take(diasAUsar).ToList();

        var cursorPorDia = diasSeleccionados.ToDictionary(d => d.Fecha,
            d => CalcularHoraInicioDia(d.Fecha, horaInicioJornada));
        var ubicacionPorDia = diasSeleccionados.ToDictionary(d => d.Fecha, d => ubicacionInicio);
        var horasUsadasPorDia = diasSeleccionados.ToDictionary(d => d.Fecha, d => 0.0);
        var areasPorDia = diasSeleccionados.ToDictionary(d => d.Fecha, d => new HashSet<string>());
        var tareasUsadasPorArea = condiciones.AreasPriorizadas
            .Where(aP => aP.Area.IdAreaInteres != null)
            .ToDictionary(aP => aP.Area.IdAreaInteres!, _ => new HashSet<string>());
        double horasJornada = (horaFin - horaInicioJornada).TotalHours;
        double maxHorasPorDia = Math.Min(condiciones.HorasFuncionales, horasJornada * 0.85);
        if (maxHorasPorDia < 1) maxHorasPorDia = horasJornada * 0.85;

        var areasOrdenadas = condiciones.AreasPriorizadas.ToList();

        foreach (var areaP in areasOrdenadas)
        {
            var area = areaP.Area;
            double horasSemanales = area.HorasSemanales > 0 ? area.HorasSemanales : 3.0;
            double horasAreaRestantes = horasSemanales;

            var tareasArea = tareasElegibles.Where(t => t.IdAreaInteres == area.IdAreaInteres).ToList();

            foreach (var dia in diasSeleccionados)
            {
                if (horasAreaRestantes < MinBloqueHoras) break;
                // Only one block per area per day
                if (areasPorDia[dia.Fecha].Contains(area.IdAreaInteres!)) continue;
                if (horasUsadasPorDia[dia.Fecha] >= maxHorasPorDia) continue;

                double disponible = maxHorasPorDia - horasUsadasPorDia[dia.Fecha];
                double bloqueHoras = Math.Min(horasAreaRestantes, disponible);

                // Reservamos el viaje real desde la ubicación actual del día hasta esta área.
                var viajeMin = await _calendarioService.ObtenerMinutosTrasladoAsync(
                    usuarioId, ubicacionPorDia[dia.Fecha], area.UbicacionPred, area.MetodoTransportePred);
                var horaDisponible = cursorPorDia[dia.Fecha] + TimeSpan.FromMinutes(viajeMin);
                if (horaDisponible + TimeSpan.FromHours(bloqueHoras) + GapEntreBloques > horaFin)
                {
                    bloqueHoras = (horaFin - horaDisponible).TotalHours - GapEntreBloques.TotalHours;
                    if (bloqueHoras < MinBloqueHoras) continue;
                }

                if (bloqueHoras < MinBloqueHoras) continue;

                // Cap at 3h per block
                if (bloqueHoras > 3.0) bloqueHoras = 3.0;

                // El bloque se coloca: la ubicación actual del día pasa a ser la de esta área.
                if (!string.IsNullOrEmpty(area.UbicacionPred))
                    ubicacionPorDia[dia.Fecha] = area.UbicacionPred;

                var bloque = new BloqueCalendario
                {
                    Tipo = TipoBloqueCalendario.BloqueInteres,
                    IdAreaInteres = area.IdAreaInteres,
                    NombreArea = area.Nombre,
                    ColorHex = area.ColorHex,
                    HoraInicio = horaDisponible,
                    HoraFin = horaDisponible + TimeSpan.FromHours(bloqueHoras),
                    TareasInternas = new ObservableCollection<SubBloqueTarea>()
                };

                LlenarBloqueConTareas(bloque, tareasArea, random, GapEntreTareas,
                    tareasUsadasPorArea.GetValueOrDefault(area.IdAreaInteres!));

                var diaSemana = propuesta.Semana.Dias.FirstOrDefault(d => d.Fecha.Date == dia.Fecha.Date);
                if (diaSemana != null)
                {
                    diaSemana.Bloques.Add(bloque);
                    OrdenarBloques(diaSemana);
                }

                cursorPorDia[dia.Fecha] = AvanzarCursorConGap(horaDisponible, TimeSpan.FromHours(bloqueHoras));
                horasUsadasPorDia[dia.Fecha] += bloqueHoras;
                horasAreaRestantes -= bloqueHoras;
                areasPorDia[dia.Fecha].Add(area.IdAreaInteres!);
            }
        }

        ContarEstadisticas(propuesta);
        return propuesta;
    }

    #endregion

    #region Relajado — max 2h/d\u00eda, 1h per \u00e1rea, day-first, gaps between blocks

    private async Task<PropuestaGeneracion> GenerarRelajado(CondicionesGeneracion condiciones, Random random,
        List<Tarea> tareasElegibles, string usuarioId, string ubicacionInicio)
    {
        var propuesta = CrearPropuestaBase(TipoPropuesta.Relajado, "Relajado",
            "M\u00e1ximo 2 horas por d\u00eda, 1 hora por \u00e1rea, repartido en todos los d\u00edas",
            condiciones, 4);

        if (condiciones.DiasSeleccionados.Count < 4)
        {
            propuesta.EsValida = false;
            propuesta.MensajeInvalidacion = "Se requieren al menos 4 d\u00edas seleccionados";
            return propuesta;
        }
        if (condiciones.HorasFuncionales <= 0)
        {
            propuesta.EsValida = false;
            propuesta.MensajeInvalidacion = "No hay horas funcionales disponibles";
            return propuesta;
        }

        var todosLosDias = ObtenerDiasGeneracion(condiciones);
        var diasGeneracion = FiltrarDiasFuturos(todosLosDias);
        var horaInicioJornada = ObtenerHoraInicioJornada(condiciones);
        var horaFin = ObtenerHoraFinJornada(condiciones);

        double maxHorasPorDia = 2.0;
        double maxHorasPorAreaPorDia = 1.0;
        var areasOrdenadas = condiciones.AreasPriorizadas.ToList();

        var cursorPorDia = diasGeneracion.ToDictionary(d => d.Fecha,
            d => CalcularHoraInicioDia(d.Fecha, horaInicioJornada));
        var ubicacionPorDia = diasGeneracion.ToDictionary(d => d.Fecha, d => ubicacionInicio);
        var horasUsadasPorDia = diasGeneracion.ToDictionary(d => d.Fecha, d => 0.0);
        var horasAreaPorDia = new Dictionary<string, Dictionary<DateTime, double>>();
        var horasAreaRestantes = new Dictionary<string, double>();
        var areasPorDia = diasGeneracion.ToDictionary(d => d.Fecha, d => new HashSet<string>());
        var tareasUsadasPorArea = condiciones.AreasPriorizadas
            .Where(aP => aP.Area.IdAreaInteres != null)
            .ToDictionary(aP => aP.Area.IdAreaInteres!, _ => new HashSet<string>());

        foreach (var areaP in areasOrdenadas)
        {
            horasAreaPorDia[areaP.Area.IdAreaInteres!] = diasGeneracion.ToDictionary(d => d.Fecha, d => 0.0);
            horasAreaRestantes[areaP.Area.IdAreaInteres!] = areaP.Area.HorasSemanales > 0 ? areaP.Area.HorasSemanales : 2.0;
        }

        var tareasPorArea = areasOrdenadas
            .ToDictionary(aP => aP.Area.IdAreaInteres!, aP => tareasElegibles.Where(t => t.IdAreaInteres == aP.Area.IdAreaInteres).ToList());

        // Days first, areas second — ensures even distribution
        foreach (var dia in diasGeneracion)
        {
            foreach (var areaP in areasOrdenadas)
            {
                var area = areaP.Area;
                var areaId = area.IdAreaInteres!;
                if (horasAreaRestantes[areaId] < MinBloqueHoras) continue;
                if (horasUsadasPorDia[dia.Fecha] >= maxHorasPorDia) break;
                // Only one block per area per day
                if (areasPorDia[dia.Fecha].Contains(areaId)) continue;
                if (horasAreaPorDia[areaId][dia.Fecha] >= maxHorasPorAreaPorDia) continue;

                double disponibleEnDia = maxHorasPorDia - horasUsadasPorDia[dia.Fecha];
                double disponibleAreaDia = maxHorasPorAreaPorDia - horasAreaPorDia[areaId][dia.Fecha];
                double bloqueHoras = Math.Min(Math.Min(disponibleEnDia, disponibleAreaDia), horasAreaRestantes[areaId]);
                bloqueHoras = Math.Min(bloqueHoras, 1.0);
                if (bloqueHoras < MinBloqueHoras) continue;

                // Reservamos el viaje real desde la ubicación actual del día hasta esta área.
                var viajeMin = await _calendarioService.ObtenerMinutosTrasladoAsync(
                    usuarioId, ubicacionPorDia[dia.Fecha], area.UbicacionPred, area.MetodoTransportePred);
                var horaDisponible = cursorPorDia[dia.Fecha] + TimeSpan.FromMinutes(viajeMin);
                // Include gap after block
                if (horaDisponible + TimeSpan.FromHours(bloqueHoras) + GapEntreBloques > horaFin) continue;

                // El bloque se coloca: la ubicación actual del día pasa a ser la de esta área.
                if (!string.IsNullOrEmpty(area.UbicacionPred))
                    ubicacionPorDia[dia.Fecha] = area.UbicacionPred;

                var bloque = new BloqueCalendario
                {
                    Tipo = TipoBloqueCalendario.BloqueInteres,
                    IdAreaInteres = areaId,
                    NombreArea = area.Nombre,
                    ColorHex = area.ColorHex,
                    HoraInicio = horaDisponible,
                    HoraFin = horaDisponible + TimeSpan.FromHours(bloqueHoras),
                    TareasInternas = new ObservableCollection<SubBloqueTarea>()
                };

                LlenarBloqueConTareas(bloque, tareasPorArea.GetValueOrDefault(areaId, new List<Tarea>()), random, GapEntreTareas,
                    tareasUsadasPorArea.GetValueOrDefault(areaId));

                var diaSemana = propuesta.Semana.Dias.FirstOrDefault(d => d.Fecha.Date == dia.Fecha.Date);
                if (diaSemana != null)
                {
                    diaSemana.Bloques.Add(bloque);
                    OrdenarBloques(diaSemana);
                }

                cursorPorDia[dia.Fecha] = AvanzarCursorConGap(horaDisponible, TimeSpan.FromHours(bloqueHoras));
                horasUsadasPorDia[dia.Fecha] += bloqueHoras;
                horasAreaPorDia[areaId][dia.Fecha] += bloqueHoras;
                horasAreaRestantes[areaId] -= bloqueHoras;
                areasPorDia[dia.Fecha].Add(areaId);
            }
        }

        ContarEstadisticas(propuesta);
        return propuesta;
    }

    #endregion

    #region Helpers

    private void LlenarBloqueConTareas(BloqueCalendario bloque, List<Tarea> tareasArea, Random random, TimeSpan gapEntreTareas,
        HashSet<string>? tareasYaUsadas = null)
    {
        if (tareasArea.Count == 0) return;

        var tareasConLimite = tareasArea
            .Where(t => t.FecLimite.HasValue && t.FecLimite.Value >= DateTime.Today)
            .OrderBy(t => t.FecLimite!.Value).ToList();

        var tareasSinLimite = tareasArea
            .Where(t => !t.FecLimite.HasValue || t.FecLimite.Value >= DateTime.Today.AddDays(30))
            .OrderBy(_ => random.Next()).ToList();

        var tareasParaBloque = tareasConLimite.Any() ? tareasConLimite : tareasSinLimite;
        if (!tareasParaBloque.Any()) tareasParaBloque = tareasArea.OrderBy(_ => random.Next()).Take(3).ToList();

        // Prioritize tasks not yet used in this proposal, fallback to used ones if all exhausted
        List<Tarea> tareasNoUsadas;
        List<Tarea> tareasUsadasRepetir;
        if (tareasYaUsadas != null && tareasYaUsadas.Count > 0)
        {
            tareasNoUsadas = tareasParaBloque.Where(t => t.IdTarea == null || !tareasYaUsadas.Contains(t.IdTarea)).ToList();
            tareasUsadasRepetir = tareasParaBloque.Where(t => t.IdTarea != null && tareasYaUsadas.Contains(t.IdTarea)).ToList();
            tareasParaBloque = tareasNoUsadas.Any() ? tareasNoUsadas : tareasUsadasRepetir.Any() ? tareasUsadasRepetir : tareasParaBloque;
        }

        double duracionBloque = (bloque.HoraFin - bloque.HoraInicio).TotalHours;
        if (duracionBloque < MinTareaHoras) return;

        if (bloque.TareasInternas is null)
            bloque.TareasInternas = new ObservableCollection<SubBloqueTarea>();

        int numTareas;
        double duracionPorTarea;
        double gapHoras = gapEntreTareas.TotalHours;

        if (duracionBloque <= 1.0)
        {
            numTareas = 1;
            duracionPorTarea = duracionBloque;
        }
        else
        {
            numTareas = (int)Math.Floor(duracionBloque);
            if (numTareas < 2) numTareas = 2;

            double tiempoGaps = (numTareas - 1) * gapHoras;
            double tiempoTareas = duracionBloque - tiempoGaps;
            duracionPorTarea = tiempoTareas / numTareas;

            while (duracionPorTarea < 0.5 && numTareas > 2)
            {
                numTareas--;
                tiempoGaps = (numTareas - 1) * gapHoras;
                tiempoTareas = duracionBloque - tiempoGaps;
                duracionPorTarea = tiempoTareas / numTareas;
            }

            if (duracionPorTarea < 0.5)
            {
                numTareas = 1;
                duracionPorTarea = duracionBloque;
            }
        }

        numTareas = Math.Min(numTareas, tareasParaBloque.Count);
        if (numTareas == 0) return;

        if (numTareas == 1)
        {
            duracionPorTarea = duracionBloque;
        }
        else
        {
            double tiempoGaps = (numTareas - 1) * gapHoras;
            double tiempoTareas = duracionBloque - tiempoGaps;
            duracionPorTarea = tiempoTareas / numTareas;
        }

        var tareasSeleccionadas = tareasParaBloque.Take(numTareas).ToList();
        double offsetHoras = 0;

        for (int i = 0; i < tareasSeleccionadas.Count; i++)
        {
            if (i > 0) offsetHoras += gapHoras;
            var tarea = tareasSeleccionadas[i];
            var tareaInicio = bloque.HoraInicio + TimeSpan.FromHours(offsetHoras);
            bloque.TareasInternas.Add(new SubBloqueTarea
            {
                IdTarea = tarea.IdTarea ?? "",
                Nombre = tarea.Nombre,
                HoraInicio = tareaInicio,
                HoraFin = tareaInicio + TimeSpan.FromHours(duracionPorTarea),
                Completada = tarea.FecCompletado != null
            });
            offsetHoras += duracionPorTarea;

            if (tareasYaUsadas != null && tarea.IdTarea != null)
                tareasYaUsadas.Add(tarea.IdTarea);
        }
    }

    private PropuestaGeneracion CrearPropuestaBase(TipoPropuesta tipo, string nombre, string desc,
        CondicionesGeneracion condiciones, int minDias)
    {
        var semana = new SemanaCalendario
        {
            NumeroSemana = System.Globalization.CultureInfo.InvariantCulture.Calendar
                .GetWeekOfYear(condiciones.FechaInicio, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday),
            FechaInicio = condiciones.FechaInicio,
            FechaFin = condiciones.FechaFin
        };

        for (int i = 0; i < 7; i++)
        {
            var fecha = condiciones.FechaInicio.AddDays(i);
            semana.Dias.Add(new DiaCalendario
            {
                Fecha = fecha,
                NombreDia = NombresDias[i],
                NumeroDia = fecha.Day,
                EsHoy = fecha.Date == DateTime.Today
            });
        }

        return new PropuestaGeneracion
        {
            Tipo = tipo,
            Nombre = nombre,
            Descripcion = desc,
            Semana = semana,
            EsValida = true,
            MinimoDiasRequeridos = minDias,
            DiasSeleccionadosCount = condiciones.DiasSeleccionados.Count
        };
    }

    private List<DiaCalendario> ObtenerDiasGeneracion(CondicionesGeneracion condiciones)
    {
        return condiciones.DiasSeleccionados
            .Select(diaSemana =>
            {
                var offset = ((int)diaSemana - (int)DayOfWeek.Monday + 7) % 7;
                return condiciones.FechaInicio.AddDays(offset);
            })
            .OrderBy(f => f)
            .Select(f => new DiaCalendario
            {
                Fecha = f,
                NombreDia = NombresDias[(int)f.DayOfWeek == 0 ? 6 : (int)f.DayOfWeek - 1],
                NumeroDia = f.Day,
                EsHoy = f.Date == DateTime.Today
            })
            .ToList();
    }

    private TimeSpan ObtenerHoraInicioJornada(CondicionesGeneracion condiciones)
    {
        return _sesionService.UsuarioActual?.HoraInicioJornada ?? TimeSpan.FromHours(7.5);
    }

    private TimeSpan ObtenerHoraFinJornada(CondicionesGeneracion condiciones)
    {
        return _sesionService.UsuarioActual?.HoraFinJornada ?? TimeSpan.FromHours(22);
    }

    private static void OrdenarBloques(DiaCalendario dia)
    {
        var ordenados = dia.Bloques.OrderBy(b => b.HoraInicio).ToList();
        dia.Bloques.Clear();
        foreach (var b in ordenados) dia.Bloques.Add(b);
    }

    private void ContarEstadisticas(PropuestaGeneracion propuesta)
    {
        propuesta.TotalBloques = 0;
        propuesta.TotalTareas = 0;
        propuesta.HorasFuncionalesUsadas = 0;

        foreach (var dia in propuesta.Semana.Dias)
        {
            foreach (var bloque in dia.Bloques)
            {
                propuesta.TotalBloques++;
                propuesta.HorasFuncionalesUsadas += (bloque.HoraFin - bloque.HoraInicio).TotalHours;

                if (bloque.TareasInternas != null)
                    propuesta.TotalTareas += bloque.TareasInternas.Count;
                else if (bloque.Tipo == TipoBloqueCalendario.Tarea)
                    propuesta.TotalTareas++;
            }
        }

        propuesta.HorasFuncionalesUsadas = Math.Round(propuesta.HorasFuncionalesUsadas, 1);
    }

    #endregion
}