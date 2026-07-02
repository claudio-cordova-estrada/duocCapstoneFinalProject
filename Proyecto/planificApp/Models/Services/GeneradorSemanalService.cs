using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
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
        var usuarioId = _sesionService.UsuarioActual?.IdUsuario ?? "";
        var todasLasTareas = string.IsNullOrEmpty(usuarioId)
            ? new List<Tarea>()
            : await _tareaRepo.ObtenerTareasPorUsuario(usuarioId);

        var tareasElegibles = FiltrarTareasElegibles(todasLasTareas, condiciones);
        var random = new Random();

        // "Regenerar" reordena las áreas al azar respetando el nivel de prioridad: los niveles
        // más altos van primero; dentro de un mismo nivel, el orden es aleatorio en cada generación.
        condiciones.AreasPriorizadas = BarajarPorPrioridad(condiciones.AreasPriorizadas, random);

        var ubicacionInicio = _sesionService.UsuarioActual?.UbicacionActual?.Trim() ?? "Casa";

        // Jornada libre real por día (jornada menos las tareas ya agendadas ese día): base de la factibilidad.
        var disponibilidad = CalcularDisponibilidadPorDia(condiciones, todasLasTareas);

        var propuestaEq = await GenerarEquilibrio(condiciones, random, tareasElegibles, usuarioId, ubicacionInicio, disponibilidad);
        var propuestaRush = await GenerarIntensiva(condiciones, random, tareasElegibles, usuarioId, ubicacionInicio, disponibilidad);
        var propuestaRel = await GenerarRelajado(condiciones, random, tareasElegibles, usuarioId, ubicacionInicio, disponibilidad);

        return new List<PropuestaGeneracion> { propuestaEq, propuestaRush, propuestaRel };
    }

    private List<Tarea> FiltrarTareasElegibles(List<Tarea> todasLasTareas, CondicionesGeneracion condiciones)
    {
        var idsAreas = condiciones.AreasConsiderar
            .Where(a => a.IdAreaInteres != null)
            .Select(a => a.IdAreaInteres!).ToHashSet();

        return todasLasTareas
            .Where(t => !string.IsNullOrEmpty(t.IdAreaInteres) && idsAreas.Contains(t.IdAreaInteres))
            .Where(t => !t.FecCompletado.HasValue)
            .Where(t => t.HoraInicio == null && t.HoraFin == null)
            .Where(t => !t.FecLimite.HasValue || t.FecLimite.Value >= condiciones.FechaInicio)
            .Where(t => t.UsoGeneracion != true)
            .ToList();
    }

    // Para cada día de generación (seleccionado y futuro): horas libres totales de la jornada
    // y el mayor tramo continuo libre, descontando las tareas YA agendadas ese día.
    private Dictionary<DateTime, (double HorasLibres, double HuecoMax)> CalcularDisponibilidadPorDia(
        CondicionesGeneracion condiciones, List<Tarea> todasLasTareas)
    {
        var horaInicioJornada = ObtenerHoraInicioJornada(condiciones);
        var horaFin = ObtenerHoraFinJornada(condiciones);
        var dias = FiltrarDiasFuturos(ObtenerDiasGeneracion(condiciones));

        var resultado = new Dictionary<DateTime, (double, double)>();
        foreach (var dia in dias)
        {
            var inicioDia = CalcularHoraInicioDia(dia.Fecha, horaInicioJornada);
            var ocupados = todasLasTareas
                .Where(t => t.FecInicio.HasValue && t.HoraInicio.HasValue && t.HoraFin.HasValue &&
                            t.FecInicio.Value.Date == dia.Fecha.Date)
                .Select(t => (Inicio: t.HoraInicio!.Value, Fin: t.HoraFin!.Value))
                .ToList();
            resultado[dia.Fecha] = CalcularDisponibilidadDia(inicioDia, horaFin, ocupados);
        }
        return resultado;
    }

    // Recorre la jornada [inicioDia, finDia] descontando los intervalos ocupados (que pueden solaparse)
    // y devuelve las horas libres totales y el mayor tramo continuo libre.
    private (double HorasLibres, double HuecoMax) CalcularDisponibilidadDia(
        TimeSpan inicioDia, TimeSpan finDia, List<(TimeSpan Inicio, TimeSpan Fin)> ocupados)
    {
        if (finDia <= inicioDia) return (0, 0);

        var intervalos = ocupados
            .Select(o => (Inicio: o.Inicio < inicioDia ? inicioDia : o.Inicio,
                          Fin: o.Fin > finDia ? finDia : o.Fin))
            .Where(o => o.Fin > o.Inicio)
            .OrderBy(o => o.Inicio)
            .ToList();

        double horasLibres = 0, huecoMax = 0;
        var cursor = inicioDia;
        foreach (var iv in intervalos)
        {
            if (iv.Inicio > cursor)
            {
                var hueco = (iv.Inicio - cursor).TotalHours;
                horasLibres += hueco;
                if (hueco > huecoMax) huecoMax = hueco;
            }
            if (iv.Fin > cursor) cursor = iv.Fin;
        }
        if (finDia > cursor)
        {
            var hueco = (finDia - cursor).TotalHours;
            horasLibres += hueco;
            if (hueco > huecoMax) huecoMax = hueco;
        }
        return (Math.Round(horasLibres, 2), Math.Round(huecoMax, 2));
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

    // Regla general del tamaño de bloque de área: se prefieren bloques de mínimo 2h.
    // Solo se permite 1h como excepción cuando es lo único que cabe (al día o al área le queda ~1h).
    // 'cap' es el tope de horas del eje. Devuelve 0 si no cabe un bloque válido.
    private double AjustarDuracionBloque(double disponible, double restanteArea, double cap)
    {
        double margen = Math.Min(Math.Min(disponible, restanteArea), cap);
        if (margen >= 2.0) return margen;   // bloque de 2h o más (respeta el mínimo)
        if (margen >= 1.0) return 1.0;      // excepción: solo alcanza para 1h
        return 0.0;                          // no cabe
    }

#region Equilibrio — día por día, 3 áreas/día (5 si hay tareas por vencer), bloques 2-4h

    private async Task<PropuestaGeneracion> GenerarEquilibrio(CondicionesGeneracion condiciones, Random random,
        List<Tarea> tareasElegibles, string usuarioId, string ubicacionInicio,
        Dictionary<DateTime, (double HorasLibres, double HuecoMax)> disponibilidad)
    {
        var propuesta = CrearPropuestaBase(TipoPropuesta.Equilibrio, "Equilibrio",
            "Distribuye las actividades de forma pareja entre los d\u00edas seleccionados",
            condiciones, 1);

        if (!disponibilidad.Values.Any(d => d.HorasLibres >= 3.0))
        {
            propuesta.EsValida = false;
            propuesta.MensajeInvalidacion = "Ning\u00fan d\u00eda tiene al menos 3 horas libres para equilibrar";
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
        double maxHorasPorDia = 5.0; // tope duro de horas por día en Equilibrio
        var areasOrdenadas = condiciones.AreasPriorizadas.ToList();

        var cursorPorDia = diasGeneracion.ToDictionary(d => d.Fecha,
            d => CalcularHoraInicioDia(d.Fecha, horaInicioJornada));
        var ubicacionPorDia = diasGeneracion.ToDictionary(d => d.Fecha, d => ubicacionInicio);
        var horasUsadasPorDia = diasGeneracion.ToDictionary(d => d.Fecha, d => 0.0);

        var horasAreaRestantes = new Dictionary<string, double>();
        var tareasUsadasPorArea = new Dictionary<string, HashSet<string>>();
        var tareasPorArea = new Dictionary<string, List<Tarea>>();
        foreach (var areaP in areasOrdenadas)
        {
            var id = areaP.Area.IdAreaInteres!;
            horasAreaRestantes[id] = areaP.Area.HorasSemanales > 0 ? areaP.Area.HorasSemanales : 3.0;
            tareasUsadasPorArea[id] = new HashSet<string>();
            tareasPorArea[id] = tareasElegibles.Where(t => t.IdAreaInteres == id).ToList();
        }

        // Tareas "por vencer": vencen dentro de la semana y son asignables (hay algún día de generación
        // en o antes de su fecha límite). Mientras queden pendientes, cada día admite hasta 5 áreas (si no, 3).
        var primerDiaGen = diasGeneracion.Min(d => d.Fecha);
        var tareasPorVencerPendientes = tareasElegibles.Where(t =>
            t.FecLimite.HasValue &&
            t.FecLimite.Value.Date >= condiciones.FechaInicio.Date &&
            t.FecLimite.Value.Date <= condiciones.FechaFin.Date &&
            t.FecLimite.Value.Date >= primerDiaGen.Date &&
            !string.IsNullOrEmpty(t.IdAreaInteres) && horasAreaRestantes.ContainsKey(t.IdAreaInteres)).ToList();

        var areasDelDia = diasGeneracion.ToDictionary(d => d.Fecha, d => new HashSet<string>());

        // Orden de áreas: las que tienen tareas por vencer primero (para ubicarlas en días tempranos),
        // luego el resto en el orden ya barajado por prioridad.
        var areasConUrgencia = tareasPorVencerPendientes.Select(t => t.IdAreaInteres!).Distinct().ToHashSet();
        var ordenAreas = areasOrdenadas.Where(a => areasConUrgencia.Contains(a.Area.IdAreaInteres!))
            .Concat(areasOrdenadas.Where(a => !areasConUrgencia.Contains(a.Area.IdAreaInteres!)))
            .ToList();

        // Distribución round-robin: cada área reparte sus bloques en días DISTINTOS, rotando un puntero
        // de día. Así se usan todos los días de forma pareja y un área no se repite el mismo día.
        int numDias = diasGeneracion.Count;
        int diaIdx = 0;

        // Objetivo de tiempo por día = horas funcionales repartidas entre los días (tope: la jornada).
        // La fase 1 (variedad) llena hasta acá con áreas distintas; la fase 2 rellena reutilizando.
        double objetivoPorDia = Math.Min(maxHorasPorDia, condiciones.HorasFuncionales / numDias);

        foreach (var areaP in ordenAreas)
        {
            var area = areaP.Area;
            var areaId = area.IdAreaInteres!;
            var diasFallidos = new HashSet<DateTime>();

            // Válvula de seguridad: como máximo intentamos ~ (éxitos + fallos) por área.
            int intentos = 0, maxIntentos = numDias * 3 + 3;
            while (horasAreaRestantes[areaId] >= 1.0 && intentos++ < maxIntentos)
            {
                DiaCalendario? diaElegido = null;
                int kElegido = 0;
                for (int k = 0; k < numDias; k++)
                {
                    var d = diasGeneracion[(diaIdx + k) % numDias];
                    if (areasDelDia[d.Fecha].Contains(areaId)) continue;   // no repetir el área el mismo día
                    if (diasFallidos.Contains(d.Fecha)) continue;
                    int maxAreasDia = tareasPorVencerPendientes.Count(t => t.FecLimite!.Value.Date >= d.Fecha.Date) > 0 ? 5 : 3;
                    if (areasDelDia[d.Fecha].Count >= maxAreasDia) continue;
                    if (horasUsadasPorDia[d.Fecha] >= maxHorasPorDia) continue;
                    diaElegido = d;
                    kElegido = k;
                    break;
                }
                if (diaElegido == null) break;   // no hay día disponible para esta área

                var fecha = diaElegido.Fecha;

                // Traslado real desde la ubicación actual del día hasta esta área.
                var viajeMin = await _calendarioService.ObtenerMinutosTrasladoAsync(
                    usuarioId, ubicacionPorDia[fecha], area.UbicacionPred, area.MetodoTransportePred);
                var horaDisponible = cursorPorDia[fecha] + TimeSpan.FromMinutes(viajeMin);

                double disponibleDia = maxHorasPorDia - horasUsadasPorDia[fecha];
                double margenFinDia = (horaFin - horaDisponible - GapEntreBloques).TotalHours;
                // Cap 2h en equilibrio: bloques cortos para esparcir mejor (un área de 3h ocupa 2 días).
                double bloqueHoras = AjustarDuracionBloque(Math.Min(disponibleDia, margenFinDia), horasAreaRestantes[areaId], 2.0);
                if (bloqueHoras <= 0)
                {
                    diasFallidos.Add(fecha);   // este día ya no da para esta área; probamos otro
                    continue;
                }

                if (!string.IsNullOrEmpty(area.UbicacionPred))
                    ubicacionPorDia[fecha] = area.UbicacionPred;

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

                LlenarBloqueConTareas(bloque, tareasPorArea[areaId], random, GapEntreTareas, fecha,
                    tareasUsadasPorArea[areaId]);

                var diaSemana = propuesta.Semana.Dias.FirstOrDefault(d => d.Fecha.Date == fecha.Date);
                if (diaSemana != null)
                {
                    diaSemana.Bloques.Add(bloque);
                    OrdenarBloques(diaSemana);
                }

                // Marcamos como asignadas las tareas por vencer que este bloque efectivamente colocó.
                if (bloque.TareasInternas != null)
                    foreach (var sub in bloque.TareasInternas)
                        tareasPorVencerPendientes.RemoveAll(t => t.IdTarea == sub.IdTarea);

                cursorPorDia[fecha] = AvanzarCursorConGap(horaDisponible, TimeSpan.FromHours(bloqueHoras));
                horasUsadasPorDia[fecha] += bloqueHoras;
                horasAreaRestantes[areaId] -= bloqueHoras;
                areasDelDia[fecha].Add(areaId);
                diaIdx = (diaIdx + kElegido + 1) % numDias;  // avanzar el puntero al siguiente día
            }
        }

        // FASE 2 — Relleno: completar cada día hasta su objetivo (horas funcionales/día). Primero se
        // agregan áreas nuevas (hasta el tope de distintas); si el día aún no llega al objetivo, se
        // REUTILIZAN áreas ya presentes con tareas todavía sin usar, para aprovechar el tiempo libre.
        bool TieneTareasLibres(string id) =>
            tareasPorArea.TryGetValue(id, out var lista) && lista.Count > tareasUsadasPorArea[id].Count;

        foreach (var dia in diasGeneracion)
        {
            var fecha = dia.Fecha;
            int guardia = 0;
            while (objetivoPorDia - horasUsadasPorDia[fecha] >= 1.0 && guardia++ < 40)
            {
                int maxAreasDia = tareasPorVencerPendientes.Count(t => t.FecLimite!.Value.Date >= fecha.Date) > 0 ? 5 : 3;

                // 1) Área NUEVA (no presente hoy) con tareas libres, si aún hay cupo de áreas distintas.
                string? elegido = null;
                if (areasDelDia[fecha].Count < maxAreasDia)
                    elegido = ordenAreas.Select(a => a.Area.IdAreaInteres!)
                        .FirstOrDefault(id => !areasDelDia[fecha].Contains(id) && TieneTareasLibres(id));

                // 2) Si no hay nueva, reutilizamos un área ya presente con tareas libres.
                elegido ??= ordenAreas.Select(a => a.Area.IdAreaInteres!)
                        .FirstOrDefault(id => areasDelDia[fecha].Contains(id) && TieneTareasLibres(id));

                if (elegido == null) break;  // no queda nada para rellenar este día

                var area = areasOrdenadas.First(a => a.Area.IdAreaInteres == elegido).Area;
                var viajeMin = await _calendarioService.ObtenerMinutosTrasladoAsync(
                    usuarioId, ubicacionPorDia[fecha], area.UbicacionPred, area.MetodoTransportePred);
                var horaDisponible = cursorPorDia[fecha] + TimeSpan.FromMinutes(viajeMin);

                double margenFinDia = (horaFin - horaDisponible - GapEntreBloques).TotalHours;
                double faltaObjetivo = objetivoPorDia - horasUsadasPorDia[fecha];
                double bloqueHoras = AjustarDuracionBloque(Math.Min(margenFinDia, faltaObjetivo), 2.0, 2.0);
                if (bloqueHoras <= 0) break;

                if (!string.IsNullOrEmpty(area.UbicacionPred))
                    ubicacionPorDia[fecha] = area.UbicacionPred;

                var bloque = new BloqueCalendario
                {
                    Tipo = TipoBloqueCalendario.BloqueInteres,
                    IdAreaInteres = elegido,
                    NombreArea = area.Nombre,
                    ColorHex = area.ColorHex,
                    HoraInicio = horaDisponible,
                    HoraFin = horaDisponible + TimeSpan.FromHours(bloqueHoras),
                    TareasInternas = new ObservableCollection<SubBloqueTarea>()
                };

                LlenarBloqueConTareas(bloque, tareasPorArea[elegido], random, GapEntreTareas, fecha,
                    tareasUsadasPorArea[elegido]);

                // Si el bloque quedó sin tareas colocables para este día, no aporta: dejamos de rellenar.
                if (bloque.TareasInternas == null || bloque.TareasInternas.Count == 0) break;

                var diaSemana = propuesta.Semana.Dias.FirstOrDefault(d => d.Fecha.Date == fecha.Date);
                if (diaSemana != null)
                {
                    diaSemana.Bloques.Add(bloque);
                    OrdenarBloques(diaSemana);
                }

                foreach (var sub in bloque.TareasInternas)
                    tareasPorVencerPendientes.RemoveAll(t => t.IdTarea == sub.IdTarea);

                cursorPorDia[fecha] = AvanzarCursorConGap(horaDisponible, TimeSpan.FromHours(bloqueHoras));
                horasUsadasPorDia[fecha] += bloqueHoras;
                areasDelDia[fecha].Add(elegido);
            }
        }

        ContarEstadisticas(propuesta);
        return propuesta;
    }

    #endregion

    #region Intensiva

    private async Task<PropuestaGeneracion> GenerarIntensiva(CondicionesGeneracion condiciones, Random random,
        List<Tarea> tareasElegibles, string usuarioId, string ubicacionInicio,
        Dictionary<DateTime, (double HorasLibres, double HuecoMax)> disponibilidad)
    {
        var propuesta = CrearPropuestaBase(TipoPropuesta.Rushear, "Intensiva",
            "Concentra las actividades en la menor cantidad de d\u00edas posible",
            condiciones, 2);

        if (!disponibilidad.Values.Any(d => d.HuecoMax >= 5.0))
        {
            propuesta.EsValida = false;
            propuesta.MensajeInvalidacion = "Ning\u00fan d\u00eda tiene 5 horas seguidas libres para una jornada intensiva";
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

        const double topeDia = 8.0; // tope e ideal de horas para la jornada intensiva

        // Buscamos el día MÁS DESOCUPADO (mayor tiempo libre) y concentramos todo ahí.
        var diaObjetivo = diasFuturos
            .OrderByDescending(d => disponibilidad.TryGetValue(d.Fecha, out var disp) ? disp.HorasLibres : 0.0)
            .ThenBy(d => d.Fecha)
            .First();
        var fecha = diaObjetivo.Fecha;

        var areasOrdenadas = condiciones.AreasPriorizadas.ToList();
        var tareasUsadasPorArea = areasOrdenadas
            .Where(aP => aP.Area.IdAreaInteres != null)
            .ToDictionary(aP => aP.Area.IdAreaInteres!, _ => new HashSet<string>());
        var tareasPorArea = areasOrdenadas
            .ToDictionary(aP => aP.Area.IdAreaInteres!, aP => tareasElegibles.Where(t => t.IdAreaInteres == aP.Area.IdAreaInteres).ToList());

        bool TieneTareasLibres(string id) =>
            tareasPorArea.TryGetValue(id, out var lista) && lista.Count > tareasUsadasPorArea[id].Count;

        var cursor = CalcularHoraInicioDia(fecha, horaInicioJornada);
        var ubicacionActual = ubicacionInicio;
        double horasUsadas = 0;
        var areasEnDia = new HashSet<string>();

        // Llenamos ese único día hasta 8h: primero áreas nuevas (variedad, por prioridad),
        // luego reutilizamos áreas con tareas todavía sin usar. Bloques de 2h (cap 3h).
        int guardia = 0;
        while (horasUsadas < topeDia && guardia++ < 40)
        {
            string? elegido = areasOrdenadas.Select(a => a.Area.IdAreaInteres!)
                .FirstOrDefault(id => !areasEnDia.Contains(id) && TieneTareasLibres(id));
            elegido ??= areasOrdenadas.Select(a => a.Area.IdAreaInteres!)
                .FirstOrDefault(id => TieneTareasLibres(id));
            if (elegido == null) break;

            var area = areasOrdenadas.First(a => a.Area.IdAreaInteres == elegido).Area;
            var viajeMin = await _calendarioService.ObtenerMinutosTrasladoAsync(
                usuarioId, ubicacionActual, area.UbicacionPred, area.MetodoTransportePred);
            var horaDisponible = cursor + TimeSpan.FromMinutes(viajeMin);

            double margenFinDia = (horaFin - horaDisponible - GapEntreBloques).TotalHours;
            double faltaTope = topeDia - horasUsadas;
            // Bloques de 2h para que entren MÁS áreas distintas (Intensiva no topea la cantidad de áreas).
            double bloqueHoras = AjustarDuracionBloque(Math.Min(margenFinDia, faltaTope), 2.0, 2.0);
            if (bloqueHoras <= 0) break;

            if (!string.IsNullOrEmpty(area.UbicacionPred))
                ubicacionActual = area.UbicacionPred;

            var bloque = new BloqueCalendario
            {
                Tipo = TipoBloqueCalendario.BloqueInteres,
                IdAreaInteres = elegido,
                NombreArea = area.Nombre,
                ColorHex = area.ColorHex,
                HoraInicio = horaDisponible,
                HoraFin = horaDisponible + TimeSpan.FromHours(bloqueHoras),
                TareasInternas = new ObservableCollection<SubBloqueTarea>()
            };

            LlenarBloqueConTareas(bloque, tareasPorArea[elegido], random, GapEntreTareas, fecha,
                tareasUsadasPorArea[elegido]);

            if (bloque.TareasInternas == null || bloque.TareasInternas.Count == 0) break;

            var diaSemana = propuesta.Semana.Dias.FirstOrDefault(d => d.Fecha.Date == fecha.Date);
            if (diaSemana != null)
            {
                diaSemana.Bloques.Add(bloque);
                OrdenarBloques(diaSemana);
            }

            cursor = AvanzarCursorConGap(horaDisponible, TimeSpan.FromHours(bloqueHoras));
            horasUsadas += bloqueHoras;
            areasEnDia.Add(elegido);
        }

        ContarEstadisticas(propuesta);
        return propuesta;
    }

    #endregion

    #region Relajado — max 2h/d\u00eda, 1h per \u00e1rea, day-first, gaps between blocks

    private async Task<PropuestaGeneracion> GenerarRelajado(CondicionesGeneracion condiciones, Random random,
        List<Tarea> tareasElegibles, string usuarioId, string ubicacionInicio,
        Dictionary<DateTime, (double HorasLibres, double HuecoMax)> disponibilidad)
    {
        var propuesta = CrearPropuestaBase(TipoPropuesta.Relajado, "Relajado",
            "M\u00e1ximo 2 horas por d\u00eda, una sola \u00e1rea por d\u00eda con calma",
            condiciones, 4);

        if (!disponibilidad.Values.Any(d => d.HorasLibres > 1.0))
        {
            propuesta.EsValida = false;
            propuesta.MensajeInvalidacion = "Ning\u00fan d\u00eda tiene m\u00e1s de 1 hora libre";
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
        var areasOrdenadas = condiciones.AreasPriorizadas.ToList();

        var cursorPorDia = diasGeneracion.ToDictionary(d => d.Fecha,
            d => CalcularHoraInicioDia(d.Fecha, horaInicioJornada));
        var ubicacionPorDia = diasGeneracion.ToDictionary(d => d.Fecha, d => ubicacionInicio);
        var horasAreaRestantes = new Dictionary<string, double>();
        var tareasUsadasPorArea = condiciones.AreasPriorizadas
            .Where(aP => aP.Area.IdAreaInteres != null)
            .ToDictionary(aP => aP.Area.IdAreaInteres!, _ => new HashSet<string>());

        foreach (var areaP in areasOrdenadas)
            horasAreaRestantes[areaP.Area.IdAreaInteres!] = areaP.Area.HorasSemanales > 0 ? areaP.Area.HorasSemanales : 2.0;

        var tareasPorArea = areasOrdenadas
            .ToDictionary(aP => aP.Area.IdAreaInteres!, aP => tareasElegibles.Where(t => t.IdAreaInteres == aP.Area.IdAreaInteres).ToList());

        // Una sola área por día en un bloque de 2h (o 1h si al área le queda poco). Máx 2h/día.
        foreach (var dia in diasGeneracion)
        {
            foreach (var areaP in areasOrdenadas)
            {
                var area = areaP.Area;
                var areaId = area.IdAreaInteres!;
                if (horasAreaRestantes[areaId] < 1.0) continue; // mínimo 1h para calmado

                // Reservamos el viaje real desde la ubicación actual del día hasta esta área.
                var viajeMin = await _calendarioService.ObtenerMinutosTrasladoAsync(
                    usuarioId, ubicacionPorDia[dia.Fecha], area.UbicacionPred, area.MetodoTransportePred);
                var horaDisponible = cursorPorDia[dia.Fecha] + TimeSpan.FromMinutes(viajeMin);

                double margenFinDia = (horaFin - horaDisponible - GapEntreBloques).TotalHours;
                double bloqueHoras = AjustarDuracionBloque(Math.Min(maxHorasPorDia, margenFinDia), horasAreaRestantes[areaId], 2.0);
                if (bloqueHoras <= 0) continue;

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

                LlenarBloqueConTareas(bloque, tareasPorArea.GetValueOrDefault(areaId, new List<Tarea>()), random, GapEntreTareas, dia.Fecha,
                    tareasUsadasPorArea.GetValueOrDefault(areaId));

                var diaSemana = propuesta.Semana.Dias.FirstOrDefault(d => d.Fecha.Date == dia.Fecha.Date);
                if (diaSemana != null)
                {
                    diaSemana.Bloques.Add(bloque);
                    OrdenarBloques(diaSemana);
                }

                horasAreaRestantes[areaId] -= bloqueHoras;
                break; // solo 1 área por día en calmado
            }
        }

        ContarEstadisticas(propuesta);
        return propuesta;
    }

    #endregion

    #region Helpers

    private void LlenarBloqueConTareas(BloqueCalendario bloque, List<Tarea> tareasArea, Random random, TimeSpan gapEntreTareas,
        DateTime fechaBloque, HashSet<string>? tareasYaUsadas = null)
    {
        if (tareasArea.Count == 0) return;

        var tareasConLimite = tareasArea
            .Where(t => t.FecLimite.HasValue && t.FecLimite.Value.Date >= fechaBloque.Date)
            .OrderBy(t => t.FecLimite!.Value).ToList();

        var tareasSinLimite = tareasArea
            .Where(t => !t.FecLimite.HasValue || t.FecLimite.Value.Date >= fechaBloque.Date.AddDays(30))
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

    // Baraja las áreas al azar pero respetando el nivel de prioridad (niveles altos primero;
    // dentro de cada nivel, orden aleatorio). Es lo que hace variar el orden al "Regenerar".
    private List<AreaPriorizada> BarajarPorPrioridad(IEnumerable<AreaPriorizada> areas, Random random)
    {
        return areas
            .GroupBy(a => a.NivelPriorizacion)
            .OrderByDescending(g => g.Key)
            .SelectMany(g => g.OrderBy(_ => random.Next()))
            .ToList();
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