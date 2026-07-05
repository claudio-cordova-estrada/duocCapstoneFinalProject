using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlanificApp.Models.Enums;
using PlanificApp.Models.Repositories.Interfaces;

namespace PlanificApp.Models.Services;

// ⚠️ HERRAMIENTA DE DESARROLLO — TEMPORAL.
// Genera datos de prueba realistas (áreas de interés, ubicaciones y tareas) para un usuario,
// pensada para poblar la entrega final y probar la generación semanal a fondo. Antes de insertar,
// borra TODO lo existente de ese usuario (tareas, áreas, ubicaciones y bloques de calendario), así
// se puede re-ejecutar cuantas veces se quiera sin duplicar ni dejar restos.
// QUITAR antes de entregar: este archivo, su registro en DI (App.axaml.cs) y el botón
// "Generar datos de prueba (dev)" de UsuarioDetalleView + su comando en el VM.
public class DatosPruebaSeeder
{
    private readonly ITareaRepository _tareaRepo;
    private readonly IAreaInteresRepository _areaRepo;
    private readonly IUbicacionRepository _ubicacionRepo;
    private readonly IBloqueAreaInteresScheduleRepository _bloqueRepo;
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IGeneracionRepository _generacionRepo;

    public DatosPruebaSeeder(ITareaRepository tareaRepo, IAreaInteresRepository areaRepo,
        IUbicacionRepository ubicacionRepo, IBloqueAreaInteresScheduleRepository bloqueRepo,
        IUsuarioRepository usuarioRepo, IGeneracionRepository generacionRepo)
    {
        _tareaRepo = tareaRepo;
        _areaRepo = areaRepo;
        _ubicacionRepo = ubicacionRepo;
        _bloqueRepo = bloqueRepo;
        _usuarioRepo = usuarioRepo;
        _generacionRepo = generacionRepo;
    }

    private const int TareasPorAreaObjetivo = 40;

    // 10 áreas → (Nombre, ColorHex, Prioridad, HorasSemanales, UbicacionPred, TransportePred).
    // UbicacionPred/Transporte hacen que la generación calcule traslados reales entre bloques.
    private static readonly (string Nombre, string Color, PrioridadAreaInteres Prio, int Horas,
        string? UbicPred, MetodoTransporte? Transp)[] AreasDef =
    {
        ("Universidad", "#60a5fa", PrioridadAreaInteres.Alta,  18, "Universidad de Concepción", MetodoTransporte.Bus),
        ("Trabajo",     "#34d399", PrioridadAreaInteres.Alta,  20, "Oficina",                   MetodoTransporte.Auto),
        ("Deporte",     "#fb923c", PrioridadAreaInteres.Media,  5, "Gimnasio Pacific",          MetodoTransporte.Auto),
        ("Salud",       "#f87171", PrioridadAreaInteres.Media,  4, "CESFAM Víctor Manuel",      MetodoTransporte.Bus),
        ("Hogar",       "#a78bfa", PrioridadAreaInteres.Baja,   4, "Casa",                      MetodoTransporte.Caminar),
        ("Finanzas",    "#facc15", PrioridadAreaInteres.Media,  3, null,                        null),
        ("Lectura",     "#2dd4bf", PrioridadAreaInteres.Baja,   4, "Casa",                      MetodoTransporte.Caminar),
        ("Social",      "#f472b6", PrioridadAreaInteres.Baja,   4, "Mall Plaza del Trébol",     MetodoTransporte.Auto),
        ("Idiomas",     "#818cf8", PrioridadAreaInteres.Media,  5, "Casa",                      MetodoTransporte.Caminar),
        ("Música",      "#c084fc", PrioridadAreaInteres.Baja,   3, "Casa",                      MetodoTransporte.Caminar),
    };

    // Ubicaciones reales de Concepción. AreaInteres = "General" como hace la app al crearlas.
    private static readonly (string Nombre, string Dir, string Color, string Transporte, double Lat, double Lon)[] UbicacionesDef =
    {
        ("Casa",                      "Barrio Universitario, Concepción",          "#a78bfa", "A pie", -36.8333, -73.0350),
        ("Universidad de Concepción", "Víctor Lamas 1290, Concepción",             "#60a5fa", "Bus",   -36.8299, -73.0355),
        ("Oficina",                   "Avenida OHiggins 940, Concepción",          "#34d399", "Auto",  -36.8271, -73.0490),
        ("Gimnasio Pacific",          "Autopista Concepción-Talcahuano 8900",      "#fb923c", "Auto",  -36.8090, -73.0640),
        ("Mall Plaza del Trébol",     "Avenida Jorge Alessandri 3177, Talcahuano", "#f472b6", "Auto",  -36.7930, -73.0770),
        ("CESFAM Víctor Manuel",      "Chacabuco 916, Concepción",                 "#f87171", "Bus",   -36.8260, -73.0450),
    };

    // Nombres de tareas por área: se combinan plantillas × temas para producir ~40 nombres realistas.
    private static readonly Dictionary<string, (string[] Plantillas, string[] Temas)> TareasPorArea = new()
    {
        ["Universidad"] = (
            new[] { "Estudiar para el certamen de {t}", "Entregar el informe de {t}", "Preparar la presentación de {t}",
                    "Resolver la guía de {t}", "Leer el capítulo de {t}", "Repasar los apuntes de {t}" },
            new[] { "Cálculo", "Física", "Programación", "Bases de Datos", "Redes", "Estadística", "Álgebra", "Ingeniería de Software" }),
        ["Trabajo"] = (
            new[] { "Revisar el pull request de {t}", "Documentar el módulo de {t}", "Corregir el bug de {t}",
                    "Preparar la reunión de {t}", "Actualizar la API de {t}", "Escribir pruebas para {t}" },
            new[] { "Clientes", "Pagos", "Reportes", "Autenticación", "Inventario", "Notificaciones", "Facturación", "Usuarios" }),
        ["Deporte"] = (
            new[] { "Rutina de {t}", "Entrenamiento de {t}", "Sesión de {t}", "Clase de {t}", "Bloque de {t}" },
            new[] { "piernas", "brazos", "cardio", "natación", "running", "spinning", "funcional", "estiramiento" }),
        ["Salud"] = (
            new[] { "Agendar hora al {t}", "Control con el {t}", "Consulta con el {t}", "Ir al {t}", "Renovar receta con el {t}" },
            new[] { "dentista", "nutricionista", "kinesiólogo", "oftalmólogo", "médico general", "dermatólogo", "traumatólogo", "psicólogo" }),
        ["Hogar"] = (
            new[] { "Ordenar {t}", "Limpiar {t}", "Organizar {t}", "Ventilar {t}", "Aspirar {t}" },
            new[] { "el escritorio", "la cocina", "el living", "el dormitorio", "el baño", "el clóset", "la despensa", "el balcón" }),
        ["Finanzas"] = (
            new[] { "Revisar {t}", "Pagar {t}", "Poner al día {t}", "Planificar {t}", "Cuadrar {t}" },
            new[] { "el presupuesto del mes", "la cuenta de la luz", "la cuenta del agua", "el arriendo",
                    "las inversiones", "la tarjeta de crédito", "el ahorro mensual", "los gastos fijos" }),
        ["Lectura"] = (
            new[] { "Leer un capítulo de {t}", "Avanzar en {t}", "Terminar {t}", "Tomar notas de {t}", "Empezar {t}" },
            new[] { "Clean Code", "El Quijote", "Sapiens", "Hábitos Atómicos", "Dune", "1984", "Cien Años de Soledad", "El Principito" }),
        ["Social"] = (
            new[] { "Juntarse con {t}", "Llamar a {t}", "Almorzar con {t}", "Ir al cine con {t}", "Coordinar una salida con {t}" },
            new[] { "los amigos", "la familia", "los compañeros", "el grupo de estudio", "los primos", "los ex compañeros", "el equipo", "los vecinos" }),
        ["Idiomas"] = (
            new[] { "Practicar {t}", "Repasar vocabulario de {t}", "Ver una serie en {t}", "Hacer ejercicios de {t}", "Tomar una clase de {t}" },
            new[] { "inglés", "francés", "alemán", "italiano", "portugués", "japonés", "coreano", "chino mandarín" }),
        ["Música"] = (
            new[] { "Practicar {t}", "Ensayar {t}", "Aprender {t}", "Grabar {t}", "Componer {t}" },
            new[] { "guitarra", "piano", "una canción nueva", "escalas", "acordes", "una balada", "una pieza clásica", "improvisación" }),
    };

    // Ubicación por defecto asociada a cada área (null = actividad en casa / sin traslado).
    private static readonly Dictionary<string, string?> UbicacionPorArea = new()
    {
        ["Universidad"] = "Universidad de Concepción",
        ["Trabajo"] = "Oficina",
        ["Deporte"] = "Gimnasio Pacific",
        ["Salud"] = "CESFAM Víctor Manuel",
        ["Hogar"] = "Casa",
        ["Finanzas"] = null,
        ["Lectura"] = "Casa",
        ["Social"] = "Mall Plaza del Trébol",
        ["Idiomas"] = "Casa",
        ["Música"] = "Casa",
    };

    public async Task<string> SembrarAsync(string idUsuario)
    {
        if (string.IsNullOrWhiteSpace(idUsuario))
            throw new ArgumentException("idUsuario vacío.", nameof(idUsuario));

        // 0. Limpieza total para poder re-ejecutar sin duplicar ni dejar restos.
        await _tareaRepo.EliminarTareasPorUsuario(idUsuario);
        await _areaRepo.EliminarAreasPorUsuario(idUsuario);
        await _ubicacionRepo.EliminarUbicacionesPorUsuario(idUsuario);
        await _bloqueRepo.EliminarBloquesPorUsuario(idUsuario);
        await _generacionRepo.EliminarPorUsuario(idUsuario);

        // 1. Áreas de interés. Tras CrearAreaInteres el driver rellena IdAreaInteres.
        var areasPorNombre = new Dictionary<string, AreaInteres>();
        foreach (var d in AreasDef)
        {
            var area = new AreaInteres
            {
                Nombre = d.Nombre,
                ColorHex = d.Color,
                Prioridad = d.Prio,
                HorasSemanales = d.Horas,
                GeneracionSemanal = true,
                UbicacionPred = d.UbicPred,
                MetodoTransportePred = d.Transp,
                IdUsuario = idUsuario,
            };
            await _areaRepo.CrearAreaInteres(area);
            areasPorNombre[d.Nombre] = area;
        }

        // 2. Ubicaciones. Tras CrearUbicacion el driver rellena IdUbicacion.
        var ubicacionesPorNombre = new Dictionary<string, UbicacionGuardada>();
        foreach (var u in UbicacionesDef)
        {
            var ubic = new UbicacionGuardada
            {
                IdUsuario = idUsuario,
                Nombre = u.Nombre,
                DireccionExacta = u.Dir,
                AreaInteres = "General",
                ColorHex = u.Color,
                TransportePreferido = u.Transporte,
                Latitud = u.Lat,
                Longitud = u.Lon,
            };
            await _ubicacionRepo.CrearUbicacion(ubic);
            ubicacionesPorNombre[u.Nombre] = ubic;
        }

        // 3. Tareas por área. Se construyen todas en memoria y se insertan en un solo lote.
        var hoy = DateTime.Today;
        var tiempos = new[] { 15, 30, 45, 60, 90, 120 };
        var nuevasTareas = new List<Tarea>();

        foreach (var d in AreasDef)
        {
            var area = areasPorNombre[d.Nombre];
            var (plantillas, temas) = TareasPorArea[d.Nombre];
            var nombres = GenerarNombres(plantillas, temas, TareasPorAreaObjetivo);
            var nombreUbic = UbicacionPorArea.GetValueOrDefault(d.Nombre);

            for (int j = 0; j < nombres.Count; j++)
            {
                var tarea = new Tarea
                {
                    Nombre = nombres[j],
                    IdUsuario = idUsuario,
                    IdAreaInteres = area.IdAreaInteres,
                    Prioridad = (j % 5) + 1,
                    TiempoEstimado = tiempos[j % tiempos.Length],
                };

                // Ubicación por defecto del área en ~2 de cada 3 tareas.
                if (nombreUbic != null && j % 3 != 0 && ubicacionesPorNombre.TryGetValue(nombreUbic, out var ubic))
                {
                    tarea.Ubicacion = ubic.Nombre;
                    tarea.IdUbicacion = ubic.IdUbicacion;
                    tarea.MetodoTransporte = MapTransporte(ubic.TransportePreferido);
                }

                AplicarEstado(tarea, j, hoy);
                AplicarFlagsGeneracion(tarea, j);
                nuevasTareas.Add(tarea);
            }
        }

        // 4. Pocas tareas sueltas en el Inbox (sin área, sin fecha límite).
        foreach (var n in new[] { "Renovar el pase escolar", "Responder el correo pendiente", "Buscar un tutorial de Avalonia" })
        {
            nuevasTareas.Add(new Tarea
            {
                Nombre = n,
                IdUsuario = idUsuario,
                Prioridad = 2,
                FecCreacion = hoy.AddDays(-2),
            });
        }

        await _tareaRepo.CrearTareasEnLote(nuevasTareas);

        // 5. Historial de generaciones del usuario (conteo por mes + objetivo de uso del motor).
        var estrategiasGen = new[] { "Equilibrio", "Intensiva", "Relajado" };
        var historial = new List<Generacion>();
        for (int k = 0; k < 12; k++)
        {
            int mes = (k % 7) + 1;                          // Ene–Jul
            var fec = new DateTime(2026, mes, 3 + (k % 20), 10, 0, 0);
            historial.Add(new Generacion
            {
                IdUsuario = idUsuario,
                FecGeneracion = fec,
                FechaSemana = fec.Date,
                Estrategia = estrategiasGen[k % 3],
                TotalBloques = 10 + (k % 6),
                TotalTareas = 15 + (k % 10),
            });
        }
        await _generacionRepo.CrearEnLote(historial);

        // 6. Usuarios demo para que los objetivos globales muestren porcentajes realistas.
        int demo = await SembrarUsuariosDemoAsync(hoy);

        return $"Listo: {AreasDef.Length} áreas, {UbicacionesDef.Length} ubicaciones, {nuevasTareas.Count} tareas, " +
               $"{historial.Count} generaciones y {demo} usuarios demo.";
    }

    // Distribuye el estado de cada tarea según su índice dentro del área (40 por área):
    // 0-23  → pendientes al borde de vencer (fecha límite 6-9 jul): combustible de la generación.
    // 24-33 → completadas a tiempo, repartidas por los meses de 2026 (historial + métricas).
    // 34-37 → vencidas (pendientes con límite ya pasado): badges de atrasadas.
    // 38-39 → pendientes con vencimiento más lejano.
    private static void AplicarEstado(Tarea tarea, int j, DateTime hoy)
    {
        if (j < 24)
        {
            tarea.FecCreacion = hoy.AddDays(-(3 + (j % 25)));
            tarea.FecLimite = new DateTime(2026, 7, 6, 18, 0, 0).AddDays(j % 4); // 6-9 jul
        }
        else if (j < 34)
        {
            int mes = (j % 6) + 1;                              // Ene–Jun
            var fc = new DateTime(2026, mes, 5 + (j % 20), 12, 0, 0);
            tarea.FecCreacion = fc;
            tarea.FecLimite = fc.AddDays(4);
            tarea.FecCompletado = fc.AddDays(2);
            tarea.CompletadoEnTiempo = true;
        }
        else if (j < 38)
        {
            tarea.FecCreacion = hoy.AddDays(-(20 + (j % 10)));
            tarea.FecLimite = hoy.AddDays(-(1 + (j % 3)));      // 2-4 jul (pasado)
        }
        else
        {
            tarea.FecCreacion = hoy.AddDays(-(2 + (j % 5)));
            tarea.FecLimite = new DateTime(2026, 7, 20, 18, 0, 0).AddDays(j % 10);
        }
    }

    // Marca flags de generación para los objetivos: las completadas (24-33) y una vencida (34) cuentan
    // como "usadas en generación". ~30% quedan modificadas (objetivo: 60% sin modificar) y ~10% quedan
    // completadas fuera de tiempo (objetivo: 80% a tiempo).
    private static void AplicarFlagsGeneracion(Tarea tarea, int j)
    {
        if (j >= 24 && j <= 34)
            tarea.UsoGeneracion = true;

        if (j == 27 || j == 29 || j == 32)
            tarea.ModificacionGeneracion = true;

        if (j == 31)
            tarea.CompletadoEnTiempo = false;   // completada, pero fuera de tiempo
    }

    // Marcador de correo para reconocer (y limpiar) los usuarios demo.
    private const string DemoMarcador = "@seed.local";

    // Crea/reemplaza usuarios demo con estados variados, para que los KPIs globales del panel de
    // objetivos (activos, uso de generación) muestren porcentajes realistas en la presentación.
    private async Task<int> SembrarUsuariosDemoAsync(DateTime hoy)
    {
        // Limpieza idempotente de los demo previos (y sus generaciones).
        var todos = await _usuarioRepo.ObtenerTodosLosUsuarios();
        foreach (var u in todos.Where(u => !string.IsNullOrEmpty(u.Correo)
                                           && u.Correo.EndsWith(DemoMarcador, StringComparison.OrdinalIgnoreCase)))
        {
            if (string.IsNullOrEmpty(u.IdUsuario)) continue;
            await _generacionRepo.EliminarPorUsuario(u.IdUsuario);
            await _usuarioRepo.EliminarUsuario(u.IdUsuario);
        }

        // (Nombre, Correo, Activo, Genera). 9 activos que generan + 3 inactivos que no.
        var defs = new (string Nombre, string Correo, bool Activo, bool Genera)[]
        {
            ("Ana Torres",     "ana@seed.local",      true,  true),
            ("Bruno Díaz",     "bruno@seed.local",    true,  true),
            ("Camila Rojas",   "camila@seed.local",   true,  true),
            ("Diego Fuentes",  "diego@seed.local",    true,  true),
            ("Elena Muñoz",    "elena@seed.local",    true,  true),
            ("Felipe Soto",    "felipe@seed.local",   true,  true),
            ("Gabriela Pérez", "gabriela@seed.local", true,  true),
            ("Hugo Castro",    "hugo@seed.local",     true,  true),
            ("Ivana Reyes",    "ivana@seed.local",    true,  true),
            ("Javier Núñez",   "javier@seed.local",   false, false),
            ("Karla Vega",     "karla@seed.local",    false, false),
            ("Luis Araya",     "luis@seed.local",     false, false),
        };

        int creados = 0;
        for (int i = 0; i < defs.Length; i++)
        {
            var d = defs[i];
            var usuario = new Usuario
            {
                NombreCompleto = d.Nombre,
                Correo = d.Correo,
                PasswordHash = "seed-demo",
                EstaActivo = d.Activo,
                FecCreacion = hoy.AddDays(-(15 + i * 4)),
                FecNacimiento = new DateTime(1998, 1, 1).AddDays(i * 30),
                Ubicacion = "Concepción, Región del Biobío",
            };
            try
            {
                await _usuarioRepo.RegistrarUsuario(usuario);   // rellena IdUsuario
                creados++;
                if (d.Genera && !string.IsNullOrEmpty(usuario.IdUsuario))
                {
                    await _generacionRepo.Registrar(new Generacion
                    {
                        IdUsuario = usuario.IdUsuario,
                        FecGeneracion = hoy.AddDays(-(3 + i)),
                        FechaSemana = hoy.Date,
                        Estrategia = "Equilibrio",
                        TotalBloques = 12,
                        TotalTareas = 20,
                    });
                }
            }
            catch { /* correo ya existente u otro problema puntual: lo salteamos */ }
        }
        return creados;
    }

    // Combina plantillas × temas hasta juntar 'max' nombres distintos.
    private static List<string> GenerarNombres(string[] plantillas, string[] temas, int max)
    {
        var nombres = new List<string>();
        foreach (var tema in temas)
            foreach (var plantilla in plantillas)
            {
                nombres.Add(plantilla.Replace("{t}", tema));
                if (nombres.Count >= max) return nombres;
            }
        return nombres;
    }

    private static MetodoTransporte? MapTransporte(string transporte) => transporte switch
    {
        "Auto" => MetodoTransporte.Auto,
        "Bus" => MetodoTransporte.Bus,
        "A pie" => MetodoTransporte.Caminar,
        _ => null,
    };
}
