using planificApp.Data;

namespace planificApp.ViewModels;

public partial class BusquedaUsuarioViewModel : PageViewModel
{
    public BusquedaUsuarioViewModel()
    {
        PageName = ApplicationPageNames.BusquedaUsuario;
    }
    public string Test { get; set; } = "Busqueda de usuarios";
}