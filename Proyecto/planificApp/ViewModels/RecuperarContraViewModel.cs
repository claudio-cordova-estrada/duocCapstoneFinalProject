using planificApp.Data;

namespace planificApp.ViewModels;

public partial class RecuperarContraViewModel : PageViewModel
{
    public MainViewModel Main { get; set; }
    
    public RecuperarContraViewModel()
    {
        PageName = ApplicationPageNames.RecuperarContra;
    }
}