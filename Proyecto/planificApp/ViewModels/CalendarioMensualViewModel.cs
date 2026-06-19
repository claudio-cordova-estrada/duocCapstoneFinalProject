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

namespace planificApp.ViewModels;

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
    [ObservableProperty] private ObservableCollection<TareaDetalleSidebar> _tareasVencidas = new();
    [ObservableProperty] private bool _hayTareasVencidas;
    [ObservableProperty] private ObservableCollection<AreaInteres> _areasDelUsuario = new();

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

        var usuarioActual = _sesionService.UsuarioActual;
        if (usuarioActual == null)
        {
            Dias = new ObservableCollection<CalendarioDiaViewModel>(listaDias);
            return;
        }

        var tareas = await _tareaRepo.ObtenerTareasPorUsuario(usuarioActual.IdUsuario!);
        var areas = await _areaRepo.ObtenerAreasPorUsuario(usuarioActual.IdUsuario!);

        string ColorDeArea(Tarea t) =>
            areas.FirstOrDefault(a => a.IdAreaInteres == t.IdAreaInteres)?.ColorHex ?? "#888888";
        string NombreDeArea(Tarea t) =>
            areas.FirstOrDefault(a => a.IdAreaInteres == t.IdAreaInteres)?.Nombre ?? "Inbox";

        // --- Puntitos en la grilla: por cada día, un punto por cada área PLANIFICADA (FecInicio) ese día ---
        foreach (var dia in listaDias)
        {
            var puntos = tareas
                .Where(t => t.FecInicio.HasValue && t.FecInicio.Value.Date == dia.Fecha.Date)
                .GroupBy(t => t.IdAreaInteres ?? "INBOX")
                .Select(g => new PuntoAreaDia
                {
                    ColorHex = ColorDeArea(g.First()),
                    NombreArea = NombreDeArea(g.First())
                })
                .OrderBy(p => p.NombreArea);

            foreach (var punto in puntos)
                dia.PuntosAreas.Add(punto);
        }

        // --- Panel lateral: áreas con las tareas planificadas en el mes visible ---
        var planificadasMes = tareas
            .Where(t => t.FecInicio.HasValue
                && t.FecInicio.Value.Month == FechaReferencia.Month
                && t.FecInicio.Value.Year == FechaReferencia.Year)
            .ToList();

        var grupos = planificadasMes
            .GroupBy(t => t.IdAreaInteres ?? "INBOX")
            .Select(g => new AreaSidebarGroup
            {
                NombreArea = NombreDeArea(g.First()),
                ColorHex = ColorDeArea(g.First()),
                Tareas = new ObservableCollection<TareaDetalleSidebar>(
                    g.OrderBy(t => t.FecInicio).Select(t => new TareaDetalleSidebar
                    {
                        Titulo = t.Nombre ?? "Sin título",
                        Fecha = t.FecInicio!.Value
                    }))
            })
            .OrderBy(g => g.NombreArea);

        AreasSidebar = new ObservableCollection<AreaSidebarGroup>(grupos);

        // --- Vencidas: con fecha límite pasada y sin completar ---
        var vencidas = tareas
            .Where(t => t.FecLimite.HasValue
                && t.FecLimite.Value.Date < DateTime.Today
                && t.FecCompletado == null)
            .OrderBy(t => t.FecLimite)
            .Select(t => new TareaDetalleSidebar
            {
                Titulo = t.Nombre ?? "Sin título",
                Fecha = t.FecLimite!.Value
            });

        TareasVencidas = new ObservableCollection<TareaDetalleSidebar>(vencidas);
        HayTareasVencidas = TareasVencidas.Count > 0;

        AreasDelUsuario = new ObservableCollection<AreaInteres>(areas);

        Dias = new ObservableCollection<CalendarioDiaViewModel>(listaDias);
    }
}