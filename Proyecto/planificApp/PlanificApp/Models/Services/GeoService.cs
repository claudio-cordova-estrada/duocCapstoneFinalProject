using System;
using PlanificApp.Models.Enums;

namespace PlanificApp.Models.Services
{
    public class GeoService
    {
        // Cambiar el método para ser obtenidos desde la API de Google Maps.
        public int EstimarMinutos(double distanciaKm, MetodoTransporte metodo)
        {
            double velocidad = metodo switch
            {
                MetodoTransporte.Automovil => 45.0,
                MetodoTransporte.Bicicleta => 15.0,
                MetodoTransporte.Pie => 5.0,
                _ => 10.0
            };

            return (int)Math.Ceiling((distanciaKm / velocidad) * 60);
        }
    }
}