using planificApp.Data;

namespace planificApp.ViewModels;

public partial class DetalleUsuarioViewModel : PageViewModel
{
    public DetalleUsuarioViewModel()
    {
        PageName = ApplicationPageNames.AdminUsuarioDetalle;
    }
    public string Test { get; set; } = "Detalle Usuario";
}