using System;
using System.Collections.Generic;
using System.Linq;
using PlanificApp.Models.Enums;

namespace PlanificApp.Models.Services
{
    public class GeneradorSemanalService
    {
        public List<Tarea> OrganizarSemana(Usuario usuario, List<Tarea> tareasActivas, List<AreaInteres> areas)
        {
            // Ordenar por prioridad: Fija (4) es la primera
            var tareasPriorizadas = tareasActivas
                .Where(t => !t.CompletadoEnTiempo)
                .OrderByDescending(t => (int)t.Prioridad)
                .ToList();

            // Iniciamos el cursor de tiempo en la hora de entrada del usuario
            TimeSpan cursorTiempo = usuario.HoraInicioJornada;
            List<Tarea> agendaPropuesta = new List<Tarea>();

            foreach (var tarea in tareasPriorizadas)
            {
                // Supongamos una duración por defecto de 60 minutos si no tiene TiempoEstimado
                int duracionTarea = 60;
                TimeSpan horaFinEstimada = cursorTiempo.Add(TimeSpan.FromMinutes(duracionTarea));

                // Verificamos si la tarea cabe dentro de la jornada laboral[cite: 1]
                if (horaFinEstimada <= usuario.HoraFinJornada)
                {
                    tarea.HoraInicio = cursorTiempo;
                    tarea.HoraFin = horaFinEstimada;
                    tarea.UsoGeneracion = true;

                    agendaPropuesta.Add(tarea);

                    // Movemos el cursor de tiempo al final de esta tarea para la siguiente[cite: 1]
                    cursorTiempo = horaFinEstimada;
                }
            }

            return agendaPropuesta;
        }
    }
}