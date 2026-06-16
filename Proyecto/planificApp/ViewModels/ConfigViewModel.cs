using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlanificApp.Models;
using PlanificApp.Models.Enums;
using PlanificApp.Models.Repositories.Interfaces;
using PlanificApp.Models.Services.Interfaces;
using planificApp.Data;

namespace planificApp.ViewModels;

public partial class ConfigViewModel : PageViewModel
{
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IUbicacionRepository _ubicacionRepo;
    private readonly ISesionService _sesion;

    [ObservableProperty] private string _horaInicioHoras = "07";
    [ObservableProperty] private string _horaInicioMinutos = "30";
    [ObservableProperty] private string _horaFinHoras = "22";
    [ObservableProperty] private string _horaFinMinutos = "00";
    [ObservableProperty] private string _ubicacionInicioDisplay = "— Sin ubicación —";
    [ObservableProperty] private string _transporteDisplay = "— Sin transporte —";
    [ObservableProperty] private double _horasSueno = 7.5;
    [ObservableProperty] private bool _lunes = true;
    [ObservableProperty] private bool _martes = true;
    [ObservableProperty] private bool _miercoles = true;
    [ObservableProperty] private bool _jueves = true;
    [ObservableProperty] private bool _viernes = true;
    [ObservableProperty] private bool _sabado;
    [ObservableProperty] private bool _domingo;
    [ObservableProperty] private string _mensaje = string.Empty;
    [ObservableProperty] private bool _hayMensaje;

    public ObservableCollection<string> MisUbicaciones { get; } = new() { "— Sin ubicación —" };
    public ObservableCollection<string> OpcionesTransporte { get; } = new() { "— Sin transporte —", "Caminar", "Auto", "Bus" };
    public ObservableCollection<double> OpcionesHorasSueno { get; } = new() { 6, 6.5, 7, 7.5, 8, 8.5, 9 };

    private MetodoTransporte? _transportePredOriginal;
    private string? _ubicacionInicioOriginal;
    private List<DayOfWeek> _diasOriginal = new();

    public ConfigViewModel(IUsuarioRepository usuarioRepo, IUbicacionRepository ubicacionRepo, ISesionService sesion)
        : base()
    {
        _usuarioRepo = usuarioRepo;
        _ubicacionRepo = ubicacionRepo;
        _sesion = sesion;
        PageName = ApplicationPageNames.UserConfig;
    }

    public async Task CargarConfigAsync()
    {
        if (_sesion.UsuarioActual == null) return;

        var usuario = _sesion.UsuarioActual;

        HoraInicioHoras = ((int)usuario.HoraInicioJornada.TotalHours).ToString("D2");
        HoraInicioMinutos = usuario.HoraInicioJornada.Minutes.ToString("D2");
        HoraFinHoras = ((int)usuario.HoraFinJornada.TotalHours).ToString("D2");
        HoraFinMinutos = usuario.HoraFinJornada.Minutes.ToString("D2");
        HorasSueno = usuario.HorasSueno;

        Lunes = usuario.DiasGeneracionSemanal.Contains(DayOfWeek.Monday);
        Martes = usuario.DiasGeneracionSemanal.Contains(DayOfWeek.Tuesday);
        Miercoles = usuario.DiasGeneracionSemanal.Contains(DayOfWeek.Wednesday);
        Jueves = usuario.DiasGeneracionSemanal.Contains(DayOfWeek.Thursday);
        Viernes = usuario.DiasGeneracionSemanal.Contains(DayOfWeek.Friday);
        Sabado = usuario.DiasGeneracionSemanal.Contains(DayOfWeek.Saturday);
        Domingo = usuario.DiasGeneracionSemanal.Contains(DayOfWeek.Sunday);

        _diasOriginal = usuario.DiasGeneracionSemanal.ToList();
        _transportePredOriginal = usuario.TransportePred;

        var ubicaciones = await _ubicacionRepo.ObtenerUbicacionesPorUsuario(usuario.IdUsuario!);
        SincronizarUbicaciones(ubicaciones);

        if (usuario.TransportePred.HasValue)
        {
            TransporteDisplay = usuario.TransportePred.Value switch
            {
                MetodoTransporte.Caminar => "Caminar",
                MetodoTransporte.Auto => "Auto",
                MetodoTransporte.Bus => "Bus",
                _ => "— Sin transporte —"
            };
        }
        else
        {
            TransporteDisplay = "— Sin transporte —";
        }

        _ubicacionInicioOriginal = string.IsNullOrEmpty(usuario.UbicacionActual) ? "— Sin ubicación —" : usuario.UbicacionActual;
        UbicacionInicioDisplay = MisUbicaciones.Contains(_ubicacionInicioOriginal) ? _ubicacionInicioOriginal : "— Sin ubicación —";
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

    partial void OnTransporteDisplayChanged(string value)
    {
        VerificarSueno();
    }

    partial void OnHorasSuenoChanged(double value)
    {
        VerificarSueno();
    }

    partial void OnHoraInicioHorasChanged(string value) => VerificarSueno();
    partial void OnHoraInicioMinutosChanged(string value) => VerificarSueno();
    partial void OnHoraFinHorasChanged(string value) => VerificarSueno();
    partial void OnHoraFinMinutosChanged(string value) => VerificarSueno();

    private void VerificarSueno()
    {
        if (!int.TryParse(HoraInicioHoras, out int hiH) || !int.TryParse(HoraInicioMinutos, out int hiM)
            || !int.TryParse(HoraFinHoras, out int hfH) || !int.TryParse(HoraFinMinutos, out int hfM))
            return;

        var inicio = TimeSpan.FromHours(hiH) + TimeSpan.FromMinutes(hiM);
        var fin = TimeSpan.FromHours(hfH) + TimeSpan.FromMinutes(hfM);

        double horasJornada;
        if (fin > inicio)
            horasJornada = (fin - inicio).TotalHours;
        else
            horasJornada = (TimeSpan.FromHours(24) - inicio + fin).TotalHours;

        if (horasJornada < HorasSueno)
        {
            Mensaje = $"⚠ Solo {horasJornada:F1}h de jornada para {HorasSueno}h de sueño.";
            HayMensaje = true;
        }
        else if (horasJornada - HorasSueno < 2)
        {
            Mensaje = $"Quedan {(horasJornada - HorasSueno):F1}h libres tras dormir.";
            HayMensaje = true;
        }
        else
        {
            Mensaje = string.Empty;
            HayMensaje = false;
        }
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        if (_sesion.UsuarioActual == null) return;

        var usuario = _sesion.UsuarioActual;

        if (!int.TryParse(HoraInicioHoras, out int hiH) || !int.TryParse(HoraInicioMinutos, out int hiM))
        { Mensaje = "Hora de inicio inválida."; HayMensaje = true; return; }
        if (!int.TryParse(HoraFinHoras, out int hfH) || !int.TryParse(HoraFinMinutos, out int hfM))
        { Mensaje = "Hora de fin inválida."; HayMensaje = true; return; }

        var horaInicio = TimeSpan.FromHours(hiH) + TimeSpan.FromMinutes(hiM);
        var horaFin = TimeSpan.FromHours(hfH) + TimeSpan.FromMinutes(hfM);

        if (horaInicio >= horaFin)
        { Mensaje = "La hora de inicio debe ser menor a la de fin."; HayMensaje = true; return; }

        var transporte = TransporteDisplay switch
        {
            "Caminar" => (MetodoTransporte?)MetodoTransporte.Caminar,
            "Auto" => (MetodoTransporte?)MetodoTransporte.Auto,
            "Bus" => (MetodoTransporte?)MetodoTransporte.Bus,
            _ => null
        };

        var ubicacion = UbicacionInicioDisplay == "— Sin ubicación —" ? null : UbicacionInicioDisplay;

        var dias = new List<DayOfWeek>();
        if (Lunes) dias.Add(DayOfWeek.Monday);
        if (Martes) dias.Add(DayOfWeek.Tuesday);
        if (Miercoles) dias.Add(DayOfWeek.Wednesday);
        if (Jueves) dias.Add(DayOfWeek.Thursday);
        if (Viernes) dias.Add(DayOfWeek.Friday);
        if (Sabado) dias.Add(DayOfWeek.Saturday);
        if (Domingo) dias.Add(DayOfWeek.Sunday);

        try
        {
            await _usuarioRepo.ActualizarConfigUsuario(
                usuario.IdUsuario!, horaInicio, horaFin, ubicacion, transporte, HorasSueno, dias);

            usuario.HoraInicioJornada = horaInicio;
            usuario.HoraFinJornada = horaFin;
            usuario.UbicacionActual = ubicacion ?? "Casa";
            usuario.TransportePred = transporte;
            usuario.HorasSueno = HorasSueno;
            usuario.DiasGeneracionSemanal = dias;

            Mensaje = "Configuración guardada";
            HayMensaje = true;
        }
        catch (Exception ex)
        {
            Mensaje = $"Error: {ex.Message}";
            HayMensaje = true;
        }
    }

    [RelayCommand]
    private void ResetHoraInicio()
    {
        HoraInicioHoras = "07";
        HoraInicioMinutos = "30";
    }

    [RelayCommand]
    private void ResetHoraFin()
    {
        HoraFinHoras = "22";
        HoraFinMinutos = "00";
    }

    [RelayCommand]
    private void ResetUbicacion() => UbicacionInicioDisplay = "— Sin ubicación —";

    [RelayCommand]
    private void ResetTransporte() => TransporteDisplay = "— Sin transporte —";

    [RelayCommand]
    private void ResetSueno() => HorasSueno = 7.5;
}