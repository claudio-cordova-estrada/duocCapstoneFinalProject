using planificApp.Data;

namespace planificApp.ViewModels;

public partial class BusquedaUsuarioViewModel : PageViewModel
{
    public BusquedaUsuarioViewModel()
    {
        PageName = ApplicationPageNames.AdminUsuarios;
    }
    public string Test { get; set; } = "Busqueda de usuarios";
}