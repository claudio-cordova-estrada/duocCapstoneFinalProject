using planificApp.Data;

namespace planificApp.ViewModels;

public partial class CuentaViewModel : PageViewModel
{
    public CuentaViewModel()
    {
        PageName = ApplicationPageNames.Cuenta;
    }
    public string Test { get; set; } = "Mi cuenta";
}