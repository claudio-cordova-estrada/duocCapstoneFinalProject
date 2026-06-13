using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlanificApp.Models;

namespace PlanificApp.Models.Services.Interfaces;

public interface ICalendarioSemanalService
{
    Task<SemanaCalendario> CargarSemanaAsync(DateTime fechaLunes, string usuarioId);
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