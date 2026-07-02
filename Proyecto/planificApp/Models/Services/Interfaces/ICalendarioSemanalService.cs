using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlanificApp.Models.Enums;

namespace PlanificApp.Models.Services.Interfaces;

public interface ICalendarioSemanalService
{
    Task<SemanaCalendario> CargarSemanaAsync(DateTime fechaLunes, string usuarioId);
    // Minutos de viaje entre dos ubicaciones (por nombre). Devuelve 0 si son el mismo lugar.
    // Comparte cache y lógica con el cálculo de traslados para que el generador reserve el mismo
    // tiempo que luego se dibuja como bloque de traslado.
    Task<int> ObtenerMinutosTrasladoAsync(string usuarioId, string? origenNombre, string? destinoNombre, MetodoTransporte? metodoFallback);
    BloqueCalendario AgregarBloqueInteres(DiaCalendario dia, AreaInteres area, TimeSpan horaInicio, TimeSpan horaFin);
    BloqueCalendario AgregarTarea(DiaCalendario dia, Tarea tarea, TimeSpan horaInicio, TimeSpan horaFin, List<AreaInteres>? areas = null);
    BloqueCalendario AgregarTareaEnBloque(DiaCalendario dia, string bloqueInteresId, Tarea tarea, TimeSpan horaInicio, TimeSpan horaFin);
    void MoverBloque(DiaCalendario dia, string bloqueId, TimeSpan nuevaHoraInicio);
    void RedimensionarBloque(DiaCalendario dia, string bloqueId, TimeSpan nuevaHoraInicio, TimeSpan nuevaHoraFin);
    BloqueCalendario? EliminarBloque(DiaCalendario dia, string bloqueId);
    Task CalcularTrasladosAsync(DiaCalendario dia, string usuarioId);
    Task GuardarCambiosAsync(SemanaCalendario semana, string usuarioId);
    BloqueCalendario? FindInteresBlockAtTime(DiaCalendario dia, TimeSpan horaInicio);
    void MoverSubTarea(DiaCalendario dia, string parentBloqueId, string idTarea, TimeSpan nuevaInicio, TimeSpan nuevaFin);
    SubBloqueTarea? ExtraerSubTarea(DiaCalendario dia, string parentBloqueId, string idTarea);
    void EliminarSubTarea(DiaCalendario dia, string parentBloqueId, string idTarea);
    BloqueCalendario? InsertarTareaEnBloqueInteres(DiaCalendario dia, string bloqueId, string idTarea, string nombre, TimeSpan horaInicio, TimeSpan horaFin, bool completada);
}