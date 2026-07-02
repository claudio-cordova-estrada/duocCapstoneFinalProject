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

    // Disponibilidad de cada día: un día que ya pasó (en la semana actual) no puede activarse.
    [ObservableProperty] private bool _lunesHabilitado = true;
    [ObservableProperty] private bool _martesHabilitado = true;
    [ObservableProperty] private bool _miercolesHabilitado = true;
    [ObservableProperty] private bool _juevesHabilitado = true;
    [ObservableProperty] private bool _viernesHabilitado = true;
    [ObservableProperty] private bool _sabadoHabilitado = true;
    [ObservableProperty] private bool _domingoHabilitado = true;

    [ObservableProperty] private bool _puedeGenerar = true;
    [ObservableProperty] private DateTime _fechaLunes;
    [ObservableProperty] private bool _semanaSiguiente;

    private List<Tarea> _todasLasTareas = new();
    private double _horasOcupadas = 0;
    private DateTime _baseFechaLunes;

    // Preferencia de días del usuario (la del registro/config), para reaplicarla al cambiar de semana.
    private bool _prefLunes = true, _prefMartes = true, _prefMiercoles = true,
                 _prefJueves = true, _prefViernes = true, _prefSabado, _prefDomingo;
    private bool _suprimirRecalculo;

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
        ActualizarDiasHabilitados();
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
        ActualizarDiasHabilitados();
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

        _prefLunes = dias.Contains(DayOfWeek.Monday);
        _prefMartes = dias.Contains(DayOfWeek.Tuesday);
        _prefMiercoles = dias.Contains(DayOfWeek.Wednesday);
        _prefJueves = dias.Contains(DayOfWeek.Thursday);
        _prefViernes = dias.Contains(DayOfWeek.Friday);
        _prefSabado = dias.Contains(DayOfWeek.Saturday);
        _prefDomingo = dias.Contains(DayOfWeek.Sunday);
    }

    // Un día solo puede usarse si todavía no pasó (su fecha >= hoy). En "semana siguiente"
    // quedan todos disponibles. Reaplicamos la preferencia del usuario solo sobre los disponibles,
    // así un día no disponible queda en OFF y no suma horas.
    private void ActualizarDiasHabilitados()
    {
        var hoy = DateTime.Today;
        bool sig = SemanaSiguiente;

        LunesHabilitado     = sig || FechaLunes.Date           >= hoy;
        MartesHabilitado    = sig || FechaLunes.AddDays(1).Date >= hoy;
        MiercolesHabilitado = sig || FechaLunes.AddDays(2).Date >= hoy;
        JuevesHabilitado    = sig || FechaLunes.AddDays(3).Date >= hoy;
        ViernesHabilitado   = sig || FechaLunes.AddDays(4).Date >= hoy;
        SabadoHabilitado    = sig || FechaLunes.AddDays(5).Date >= hoy;
        DomingoHabilitado   = sig || FechaLunes.AddDays(6).Date >= hoy;

        // Suprimimos el recálculo por-día; el llamador recalcula las horas una sola vez al final.
        _suprimirRecalculo = true;
        LunesSeleccionado     = _prefLunes     && LunesHabilitado;
        MartesSeleccionado    = _prefMartes    && MartesHabilitado;
        MiercolesSeleccionado = _prefMiercoles && MiercolesHabilitado;
        JuevesSeleccionado    = _prefJueves    && JuevesHabilitado;
        ViernesSeleccionado   = _prefViernes   && ViernesHabilitado;
        SabadoSeleccionado    = _prefSabado    && SabadoHabilitado;
        DomingoSeleccionado   = _prefDomingo   && DomingoHabilitado;
        _suprimirRecalculo = false;
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
        // Solo contamos como "ocupado" el tiempo de los días que sí se van a generar
        // (un día deshabilitado/no seleccionado no aporta ni resta horas).
        var diasActivos = ObtenerDiasSeleccionados();
        var tareasSemana = _todasLasTareas.Where(t =>
            t.FecInicio.HasValue && t.HoraInicio.HasValue && t.HoraFin.HasValue &&
            t.FecInicio.Value.Date >= fechaLunes.Date &&
            t.FecInicio.Value.Date < semanaFin.Date &&
            diasActivos.Contains(t.FecInicio.Value.DayOfWeek)).ToList();

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

    partial void OnLunesSeleccionadoChanged(bool value) { if (!_suprimirRecalculo) RecalcularTodo(); }
    partial void OnMartesSeleccionadoChanged(bool value) { if (!_suprimirRecalculo) RecalcularTodo(); }
    partial void OnMiercolesSeleccionadoChanged(bool value) { if (!_suprimirRecalculo) RecalcularTodo(); }
    partial void OnJuevesSeleccionadoChanged(bool value) { if (!_suprimirRecalculo) RecalcularTodo(); }
    partial void OnViernesSeleccionadoChanged(bool value) { if (!_suprimirRecalculo) RecalcularTodo(); }
    partial void OnSabadoSeleccionadoChanged(bool value) { if (!_suprimirRecalculo) RecalcularTodo(); }
    partial void OnDomingoSeleccionadoChanged(bool value) { if (!_suprimirRecalculo) RecalcularTodo(); }

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