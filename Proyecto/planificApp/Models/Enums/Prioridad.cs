namespace PlanificApp.Models.Enums
{
    // existen 2 categorias aparte de baja, media y alta, que son urgente y fija.
    // Urgente es para tareas que deben ser completadas en un tiempo alerta determinado,
    // mientras que fija es para tareas que no tienen un tiempo variante pero son importantes.
    // Referencia: 1=Baja, 2=Media, 3=Alta, 4=Urgente, 5=Fija. Tarea.Prioridad usa int directamente.
    public enum PrioridadTarea { Baja = 0, Media = 1, Alta = 2, Urgente = 3, Fija = 4}
    public enum PrioridadAreaInteres { Predeterminado = 0, Baja = 1, Media = 2, Alta = 3}
}