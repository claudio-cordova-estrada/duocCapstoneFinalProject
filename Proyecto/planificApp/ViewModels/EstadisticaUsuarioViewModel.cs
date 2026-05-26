using planificApp.Data;

namespace planificApp.ViewModels;

public partial class EstadisticaUsuarioViewModel : PageViewModel
{
    public EstadisticaUsuarioViewModel()
    {
        PageName = ApplicationPageNames.AdminEstadisticas;
    }
    public string Test { get; set; } = "Estadisticas de usuario";
}