using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using planificApp.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace planificApp.ViewModels;

public partial class CalendarioMensualViewModel : PageViewModel
{
    // Aquí inyectarías tu repositorio o servicio para obtener las áreas por fecha
    // private readonly IPlanificacionRepository _planificacionRepo;

    [ObservableProperty] private DateTime _fechaReferencia = DateTime.Today;
    [ObservableProperty] private string _nombreMesAnio = string.Empty;
    [ObservableProperty] private ObservableCollection<CalendarioDiaViewModel> _dias = new();

    public CalendarioMensualViewModel()
    {
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

        // Generamos la cuadrícula en segundo plano para no congelar la UI
        _ = GenerarDiasGridAsync();
    }

    private async Task GenerarDiasGridAsync()
    {
        var listaDias = new List<CalendarioDiaViewModel>();

        // Primer día del mes
        DateTime primerDiaMes = new DateTime(FechaReferencia.Year, FechaReferencia.Month, 1);

        // Calculamos cuántos días del mes anterior debemos mostrar para rellenar la primera semana
        // En C#, DayOfWeek (Sunday = 0, Monday = 1... Saturday = 6). Ajustamos para que Lunes sea 0.
        int diasDesfase = ((int)primerDiaMes.DayOfWeek - 1 + 7) % 7;
        DateTime fechaInicioGrid = primerDiaMes.AddDays(-diasDesfase);

        // Una cuadrícula estándar de calendario mensual suele usar 42 celdas (6 semanas de 7 días)
        for (int i = 0; i < 42; i++)
        {
            DateTime fechaCelda = fechaInicioGrid.AddDays(i);

            var diaVm = new CalendarioDiaViewModel
            {
                Fecha = fechaCelda,
                NumeroDia = fechaCelda.Day,
                EsMesActual = fechaCelda.Month == FechaReferencia.Month,
                EsHoy = fechaCelda == DateTime.Today
            };

            listaDias.Add(diaVm);
        }

        // --- SIMULACIÓN DE DATOS (SOLID: Aquí conectarías tu base de datos de MongoDB) ---
        // Obtenemos los eventos o áreas planificadas para este rango de 42 días
        var eventosDelMes = await ObtenerEventosSimuladosAsync(fechaInicioGrid, fechaInicioGrid.AddDays(41));

        // Inyectamos los colores correspondientes a cada día
        foreach (var dia in listaDias)
        {
            var areasDelDia = eventosDelMes.Where(e => e.Fecha.Date == dia.Fecha.Date);
            foreach (var area in areasDelDia)
            {
                dia.ColoresAreas.Add(area.ColorHex);
            }
        }

        // Actualizamos la UI en el hilo principal
        Dias = new ObservableCollection<CalendarioDiaViewModel>(listaDias);
    }

    // Mock temporal para pruebas visuales
    private Task<List<AreaInteresEvento>> ObtenerEventosSimuladosAsync(DateTime inicio, DateTime fin)
    {
        return Task.FromResult(new List<AreaInteresEvento>
        {
            new() { Fecha = DateTime.Today, ColorHex = "#ea580c" }, // Naranja
            new() { Fecha = DateTime.Today, ColorHex = "#38bdf8" }, // Celeste
            new() { Fecha = DateTime.Today.AddDays(1), ColorHex = "#10b981" }, // Verde
            new() { Fecha = DateTime.Today.AddDays(-2), ColorHex = "#ef4444" }, // Rojo
            new() { Fecha = DateTime.Today.AddDays(-2), ColorHex = "#38bdf8" }
        });
    }
}