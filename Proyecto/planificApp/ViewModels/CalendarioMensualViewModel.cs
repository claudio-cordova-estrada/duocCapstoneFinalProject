using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using planificApp.Data;
using PlanificApp.Models;
using PlanificApp.Models.Repositories.Interfaces;
using PlanificApp.Models.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace planificApp.ViewModels
{
    // ========================================================
    // CLASES AUXILIARES PARA EL PANEL LATERAL
    // ========================================================
    public class TareaDetalleSidebar
    {
        public string Titulo { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
    }

    public class AreaSidebarGroup
    {
        public string NombreArea { get; set; } = string.Empty;
        public string ColorHex { get; set; } = "#ffffff";
        public ObservableCollection<TareaDetalleSidebar> Tareas { get; set; } = new();
    }

    // ========================================================
    // VIEWMODEL PRINCIPAL
    // ========================================================
    public partial class CalendarioMensualViewModel : PageViewModel
    {
        private readonly ISesionService _sesionService;
        private readonly ITareaRepository _tareaRepo;
        private readonly IAreaInteresRepository _areaRepo;

        [ObservableProperty] private DateTime _fechaReferencia = DateTime.Today;
        [ObservableProperty] private string _nombreMesAnio = string.Empty;
        [ObservableProperty] private ObservableCollection<CalendarioDiaViewModel> _dias = new();

        [ObservableProperty] private ObservableCollection<AreaSidebarGroup> _areasSidebar = new();

        // ¡AQUÍ ES DONDE DEBÍA ESTAR LA LISTA DE ÁREAS PARA LA LEYENDA!
        [ObservableProperty] private ObservableCollection<AreaInteres> _areasDelUsuario = new();

        private ObservableCollection<TareaDetalleSidebar> _tareasVencidas = new();
        public ObservableCollection<TareaDetalleSidebar> TareasVencidas
        {
            get => _tareasVencidas;
            set => SetProperty(ref _tareasVencidas, value);
        }

        private bool _hayTareasVencidas;
        public bool HayTareasVencidas
        {
            get => _hayTareasVencidas;
            set => SetProperty(ref _hayTareasVencidas, value);
        }

        public CalendarioMensualViewModel(
            ISesionService sesionService,
            ITareaRepository tareaRepo,
            IAreaInteresRepository areaRepo)
        {
            _sesionService = sesionService;
            _tareaRepo = tareaRepo;
            _areaRepo = areaRepo;

            PageName = ApplicationPageNames.UserCalendarioMensual;
            ActualizarCalendario();
        }

        [RelayCommand]
        private void MesAnterior()
        {
            FechaReferencia = FechaReferencia.AddMonths(-1);
            ActualizarCalendario();
        }

        [RelayCommand]
        private void MesSiguiente()
        {
            FechaReferencia = FechaReferencia.AddMonths(1);
            ActualizarCalendario();
        }

        private void ActualizarCalendario()
        {
            NombreMesAnio = FechaReferencia.ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-CL")).ToUpper();
            _ = GenerarDiasGridAsync();
        }

        private async Task GenerarDiasGridAsync()
        {
            var listaDias = new List<CalendarioDiaViewModel>();

            DateTime primerDiaMes = new DateTime(FechaReferencia.Year, FechaReferencia.Month, 1);
            int diasDesfase = ((int)primerDiaMes.DayOfWeek - 1 + 7) % 7;
            DateTime fechaInicioGrid = primerDiaMes.AddDays(-diasDesfase);
            DateTime fechaFinGrid = fechaInicioGrid.AddDays(41);

            for (int i = 0; i < 42; i++)
            {
                DateTime fechaCelda = fechaInicioGrid.AddDays(i);
                listaDias.Add(new CalendarioDiaViewModel
                {
                    Fecha = fechaCelda,
                    NumeroDia = fechaCelda.Day,
                    EsMesActual = fechaCelda.Month == FechaReferencia.Month,
                    EsHoy = fechaCelda.Date == DateTime.Today.Date
                });
            }

            var todasLasTareas = await ObtenerEventosRealesAsync(fechaInicioGrid, fechaFinGrid);

            foreach (var dia in listaDias)
            {
                var tareasDelDia = todasLasTareas.Where(e => e.Fecha.Date == dia.Fecha.Date);
                foreach (var tarea in tareasDelDia)
                {
                    dia.TareasDelDia.Add(tarea);
                }
            }

            Dias = new ObservableCollection<CalendarioDiaViewModel>(listaDias);
        }

        private async Task<List<TareaCalendario>> ObtenerEventosRealesAsync(DateTime inicio, DateTime fin)
        {
            var tareasCalendario = new List<TareaCalendario>();
            var gruposDict = new Dictionary<string, AreaSidebarGroup>();

            // 1. NUEVO: Lista temporal para guardar las vencidas
            var vencidasTemp = new List<TareaDetalleSidebar>();

            var usuarioActual = _sesionService.UsuarioActual;
            if (usuarioActual == null) return tareasCalendario;

            var tareas = await _tareaRepo.ObtenerTareasPorUsuario(usuarioActual.IdUsuario!);
            var areas = await _areaRepo.ObtenerAreasPorUsuario(usuarioActual.IdUsuario!);

            AreasDelUsuario = new ObservableCollection<AreaInteres>(areas);

            var tareasEnRango = tareas.Where(t =>
                t.FecLimite.HasValue &&
                t.FecLimite.Value.Date >= inicio.Date &&
                t.FecLimite.Value.Date <= fin.Date).ToList();

            foreach (var tarea in tareasEnRango)
            {
                var areaCorrespondiente = areas.FirstOrDefault(a => a.IdAreaInteres == tarea.IdAreaInteres);
                var colorRealArea = areaCorrespondiente?.ColorHex ?? "#888888";
                var colorPuntoGrid = colorRealArea;

                // 2. NUEVO: Evaluamos si está vencida de forma más limpia
                bool esVencida = tarea.FecLimite.HasValue && tarea.FecLimite.Value.Date < DateTime.Today && tarea.FecCompletado == null;

                if (esVencida)
                {
                    colorPuntoGrid = "#ef4444";

                    // Agregamos a nuestra nueva lista
                    vencidasTemp.Add(new TareaDetalleSidebar
                    {
                        Titulo = tarea.Nombre ?? "Sin título",
                        Fecha = tarea.FecLimite.Value
                    });
                }

                tareasCalendario.Add(new TareaCalendario
                {
                    Titulo = tarea.Nombre ?? "Sin título",
                    ColorHex = colorPuntoGrid,
                    Fecha = tarea.FecLimite.Value
                });

                if (tarea.FecLimite.Value.Month == FechaReferencia.Month)
                {
                    string idArea = tarea.IdAreaInteres ?? "INBOX";

                    if (!gruposDict.ContainsKey(idArea))
                    {
                        gruposDict[idArea] = new AreaSidebarGroup
                        {
                            NombreArea = areaCorrespondiente?.Nombre ?? "Inbox",
                            ColorHex = colorRealArea
                        };
                    }

                    gruposDict[idArea].Tareas.Add(new TareaDetalleSidebar
                    {
                        Titulo = tarea.Nombre ?? "Sin título",
                        Fecha = tarea.FecLimite.Value
                    });
                }
            }

            foreach (var grupo in gruposDict.Values)
            {
                var tareasOrdenadas = grupo.Tareas.OrderBy(t => t.Fecha).ToList();
                grupo.Tareas = new ObservableCollection<TareaDetalleSidebar>(tareasOrdenadas);
            }

            AreasSidebar = new ObservableCollection<AreaSidebarGroup>(gruposDict.Values.OrderBy(g => g.NombreArea));

            // 3. NUEVO: Llenamos nuestras propiedades para que la Vista reaccione
            TareasVencidas = new ObservableCollection<TareaDetalleSidebar>(vencidasTemp.OrderBy(t => t.Fecha));
            HayTareasVencidas = TareasVencidas.Any(); // Si hay 1 o más, esto será true

            return tareasCalendario;
        }
    }
}