using planificApp.Data;

namespace planificApp.ViewModels;

public partial class RecuperarContraViewModel : PageViewModel
{
    public RecuperarContraViewModel()
    {
        PageName = ApplicationPageNames.RecuperarContra;
    }
    public string Test { get; set; } = "Recuperar Contra";
}