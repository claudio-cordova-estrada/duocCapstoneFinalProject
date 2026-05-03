using planificApp.Data;

namespace planificApp.ViewModels;

public partial class ModificacionUsuarioViewModel : PageViewModel
{
    public ModificacionUsuarioViewModel()
    {
        PageName = ApplicationPageNames.ModificacionUsuario;
    }
    
    public string Test { get; set; } = "Modificacion de usuario";
}