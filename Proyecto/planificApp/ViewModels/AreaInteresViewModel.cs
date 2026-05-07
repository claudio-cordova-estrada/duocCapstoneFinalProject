using planificApp.Data;

namespace planificApp.ViewModels;

public partial class AreaInteresViewModel : PageViewModel
{
    public AreaInteresViewModel()
    {
        PageName = ApplicationPageNames.UserAreaInteres;
    }
    
    public string Test { get; set; } = "Calendario Semanal";
}