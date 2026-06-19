using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlanificApp.Models;
using PlanificApp.Models.Repositories.Interfaces;
using PlanificApp.Models.Services.Interfaces;
using planificApp.Data;

namespace planificApp.ViewModels;

public partial class CondicionesGeneracionViewModel : ViewModelBase
{
    private readonly IAreaInteresRepository _areaRepo;
    private readonly ITareaRepository _tareaRepo;
    private readonly IUbicacionRepository _ubicacionRepo;
    private readonly ISesionService _sesionService;
    private readonly ICalendarioSemanalService _calendarioService;

    [ObservableProperty] private ObservableCollection<AreaInteres> _areasConsiderar = new();
    [ObservableProperty] private ObservableCollection<AreaInteres> _areasNoConsiderar = new();
    [ObservableProperty] private ObservableCollection<AreaPriorizada> _areasPriorizadas = new();

    [ObservableProperty] private double _maxHorasGeneracionSemanal;
    [ObservableProperty] private double _horasFuncionales;
    [ObservableProperty] private double _horasLibres;
    [ObservableProperty] private string _rangoFechas = string.Empty;

    [ObservableProperty] private bool _lunesSeleccionado = true;
    [ObservableProperty] private bool _martesSeleccionado = true;
    [ObservableProperty] private bool _miercolesSeleccionado = true;
    [ObservableProperty] private bool _juevesSeleccionado = true;
    [ObservableProperty] private bool _viernesSeleccionado = true;
    [ObservableProperty] private bool _sabadoSeleccionado;
    [ObservableProperty] private bool _domingoSeleccionado;

    [ObservableProperty] private bool _puedeGenerar = true;
    [ObservableProperty] private DateTime _fechaLunes;
    [ObservableProperty] private bool _semanaSiguiente;

    private List<Tarea> _todasLasTareas = new();
    private double _horasOcupadas = 0;
    private DateTime _baseFechaLunes;

    public CondicionesGeneracion Condiciones { get; private set; } = new();
    public bool ResultadoConfirmado { get; private set; }

    public CondicionesGeneracionViewModel(
        IAreaInteresRepository areaRepo,
        ITareaRepository tareaRepo,
        IUbicacionRepository ubicacionRepo,
        ISesionService sesionService,
        ICalendarioSemanalService calendarioService)
    {
        _areaRepo = areaRepo;
        _tareaRepo = tareaRepo;
        _ubicacionRepo = ubicacionRepo;
        _sesionService = sesionService;
        _calendarioService = calendarioService;
    }

    public async Task InicializarAsync(DateTime fechaLunes)
    {
        _baseFechaLunes = fechaLunes;
        FechaLunes = SemanaSiguiente ? fechaLunes.AddDays(7) : fechaLunes;
        ActualizarRangoFechas();

        if (_sesionService.UsuarioActual == null) return;

        var usuario = _sesionService.UsuarioActual;
        var areas = await _areaRepo.ObtenerAreasPorUsuario(usuario.IdUsuario!);
        _todasLasTareas = await _tareaRepo.ObtenerTareasPorUsuario(usuario.IdUsuario!);

        AreasConsiderar.Clear();
        AreasNoConsiderar.Clear();

        foreach (var area in areas)
        {
            if (area.GeneracionSemanal == true)
                AreasConsiderar.Add(area);
            else
                AreasNoConsiderar.Add(area);
        }

        InicializarDiasDesdeUsuario(usuario);
        await CalcularHorasGeneracionAsync(FechaLunes);
        ActualizarPriorizacion();
    }

    private void ActualizarRangoFechas()
    {
        var cultura = new System.Globalization.CultureInfo("es-ES");
        var mesInicio = cultura.DateTimeFormat.GetMonthName(FechaLunes.Month);
        var mesFin = cultura.DateTimeFormat.GetMonthName(FechaLunes.AddDays(6).Month);

        if (FechaLunes.Month == FechaLunes.AddDays(6).Month)
            RangoFechas = $"Generaci\u00f3n desde el {FechaLunes.Day} hasta el {FechaLunes.AddDays(6).Day} de {mesInicio}";
        else
            RangoFechas = $"Generaci\u00f3n desde el {FechaLunes.Day} de {mesInicio} hasta el {FechaLunes.AddDays(6).Day} de {mesFin}";
    }

    // Feature: alternar entre semana actual y siguiente. Reubica FechaLunes y recalcula.
    partial void OnSemanaSiguienteChanged(bool value)
    {
        if (_baseFechaLunes == default) return;
        FechaLunes = _baseFechaLunes.AddDays(value ? 7 : 0);
        ActualizarRangoFechas();
        _ = RecalcularSemanaAsync();
    }

    private async Task RecalcularSemanaAsync()
    {
        await CalcularHorasGeneracionAsync(FechaLunes);
        ActualizarPriorizacion();
    }

    private void InicializarDiasDesdeUsuario(Usuario usuario)
    {
        var dias = usuario.DiasGeneracionSemanal ?? new List<DayOfWeek>
        {
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
            DayOfWeek.Thursday, DayOfWeek.Friday
        };

        LunesSeleccionado = dias.Contains(DayOfWeek.Monday);
        MartesSeleccionado = dias.Contains(DayOfWeek.Tuesday);
        MiercolesSeleccionado = dias.Contains(DayOfWeek.Wednesday);
        JuevesSeleccionado = dias.Contains(DayOfWeek.Thursday);
        ViernesSeleccionado = dias.Contains(DayOfWeek.Friday);
        SabadoSeleccionado = dias.Contains(DayOfWeek.Saturday);
        DomingoSeleccionado = dias.Contains(DayOfWeek.Sunday);
    }

    private async void RecalcularTodo()
    {
        await CalcularHorasGeneracionAsync(FechaLunes);
        ActualizarPriorizacion();
    }

    private async Task CalcularHorasGeneracionAsync(DateTime fechaLunes)
    {
        if (_sesionService.UsuarioActual == null) return;

        var usuario = _sesionService.UsuarioActual;
        var horasPorDia = (usuario.HoraFinJornada - usuario.HoraInicioJornada).TotalHours;

        int diasSeleccionados = ObtenerDiasSeleccionadosCount();
        double totalHorasDisponibles = horasPorDia * diasSeleccionados;

        var semanaFin = fechaLunes.AddDays(7);
        var tareasSemana = _todasLasTareas.Where(t =>
            t.FecInicio.HasValue && t.HoraInicio.HasValue && t.HoraFin.HasValue &&
            t.FecInicio.Value.Date >= fechaLunes.Date &&
            t.FecInicio.Value.Date < semanaFin.Date).ToList();

        _horasOcupadas = tareasSemana.Sum(t => (t.HoraFin!.Value - t.HoraInicio!.Value).TotalHours);

        MaxHorasGeneracionSemanal = Math.Max(0, totalHorasDisponibles - _horasOcupadas);

        if (MaxHorasGeneracionSemanal <= 0)
        {
            HorasFuncionales = 0;
            HorasLibres = 0;
        }
        else
        {
            double funcionalRatio = HorasFuncionales + HorasLibres > 0
                ? HorasFuncionales / (HorasFuncionales + HorasLibres)
                : 0.6;

            if (funcionalRatio <= 0) funcionalRatio = 0.6;

            HorasFuncionales = Math.Round(MaxHorasGeneracionSemanal * funcionalRatio, 1);
            HorasLibres = Math.Round(MaxHorasGeneracionSemanal - HorasFuncionales, 1);
        }

        ValidarPuedeGenerar();
    }

    private void ActualizarPriorizacion()
    {
        var fechaFinSemana = FechaLunes.AddDays(7);
        var fechaFinSiguiente = FechaLunes.AddDays(14);

        var priorizadas = AreasConsiderar.Select(area =>
        {
            var tareasArea = _todasLasTareas.Where(t => t.IdAreaInteres == area.IdAreaInteres).ToList();

            int cercanas = tareasArea.Count(t =>
                t.FecLimite.HasValue &&
                t.FecLimite.Value.Date >= FechaLunes.Date &&
                t.FecLimite.Value.Date < fechaFinSemana.Date);

            int semanaSiguiente = tareasArea.Count(t =>
                t.FecLimite.HasValue &&
                t.FecLimite.Value.Date >= fechaFinSemana.Date &&
                t.FecLimite.Value.Date < fechaFinSiguiente.Date);

            int total = tareasArea.Count;

            return new AreaPriorizada
            {
                Area = area,
                TareasCercanas = cercanas,
                TareasSemanaSiguiente = semanaSiguiente,
                TareasTotales = total
            };
        })
        .OrderByDescending(a => a.NivelPriorizacion)
        .ThenByDescending(a => a.TareasCercanas)
        .ThenByDescending(a => a.TareasSemanaSiguiente)
        .ThenByDescending(a => a.TareasTotales)
        .ToList();

        AreasPriorizadas = new ObservableCollection<AreaPriorizada>(priorizadas);
    }

    partial void OnHorasFuncionalesChanged(double value)
    {
        if (MaxHorasGeneracionSemanal <= 0) return;
        if (value < 0) { HorasFuncionales = 0; return; }
        if (value > MaxHorasGeneracionSemanal) { HorasFuncionales = MaxHorasGeneracionSemanal; return; }
        HorasLibres = Math.Round(MaxHorasGeneracionSemanal - value, 1);
    }

    partial void OnHorasLibresChanged(double value)
    {
        if (MaxHorasGeneracionSemanal <= 0) return;
        if (value < 0) { HorasLibres = 0; return; }
        if (value > MaxHorasGeneracionSemanal) { HorasLibres = MaxHorasGeneracionSemanal; return; }
        HorasFuncionales = Math.Round(MaxHorasGeneracionSemanal - value, 1);
    }

    partial void OnLunesSeleccionadoChanged(bool value) => RecalcularTodo();
    partial void OnMartesSeleccionadoChanged(bool value) => RecalcularTodo();
    partial void OnMiercolesSeleccionadoChanged(bool value) => RecalcularTodo();
    partial void OnJuevesSeleccionadoChanged(bool value) => RecalcularTodo();
    partial void OnViernesSeleccionadoChanged(bool value) => RecalcularTodo();
    partial void OnSabadoSeleccionadoChanged(bool value) => RecalcularTodo();
    partial void OnDomingoSeleccionadoChanged(bool value) => RecalcularTodo();

    private void ValidarPuedeGenerar()
    {
        PuedeGenerar = ObtenerDiasSeleccionadosCount() > 0 && MaxHorasGeneracionSemanal > 0;
    }

    private int ObtenerDiasSeleccionadosCount()
    {
        int count = 0;
        if (LunesSeleccionado) count++;
        if (MartesSeleccionado) count++;
        if (MiercolesSeleccionado) count++;
        if (JuevesSeleccionado) count++;
        if (ViernesSeleccionado) count++;
        if (SabadoSeleccionado) count++;
        if (DomingoSeleccionado) count++;
        return count;
    }

    public List<DayOfWeek> ObtenerDiasSeleccionados()
    {
        var dias = new List<DayOfWeek>();
        if (LunesSeleccionado) dias.Add(DayOfWeek.Monday);
        if (MartesSeleccionado) dias.Add(DayOfWeek.Tuesday);
        if (MiercolesSeleccionado) dias.Add(DayOfWeek.Wednesday);
        if (JuevesSeleccionado) dias.Add(DayOfWeek.Thursday);
        if (ViernesSeleccionado) dias.Add(DayOfWeek.Friday);
        if (SabadoSeleccionado) dias.Add(DayOfWeek.Saturday);
        if (DomingoSeleccionado) dias.Add(DayOfWeek.Sunday);
        return dias;
    }

    [RelayCommand]
    private void MoverAreaAConsiderar(AreaInteres area)
    {
        if (AreasNoConsiderar.Remove(area))
            AreasConsiderar.Add(area);
        ActualizarPriorizacion();
    }

    [RelayCommand]
    private void MoverAreaANoConsiderar(AreaInteres area)
    {
        if (AreasConsiderar.Remove(area))
            AreasNoConsiderar.Add(area);
        ActualizarPriorizacion();
    }

    public CondicionesGeneracion ObtenerCondiciones()
    {
        return new CondicionesGeneracion
        {
            AreasConsiderar = AreasConsiderar.ToList(),
            AreasPriorizadas = AreasPriorizadas.ToList(),
            MaxHorasGeneracionSemanal = MaxHorasGeneracionSemanal,
            HorasFuncionales = HorasFuncionales,
            HorasLibres = HorasLibres,
            DiasSeleccionados = ObtenerDiasSeleccionados(),
            FechaInicio = FechaLunes,
            FechaFin = FechaLunes.AddDays(6)
        };
    }

    [RelayCommand]
    private void Confirmar()
    {
        ResultadoConfirmado = true;
    }

    [RelayCommand]
    private void Cancelar()
    {
        ResultadoConfirmado = false;
    }
}