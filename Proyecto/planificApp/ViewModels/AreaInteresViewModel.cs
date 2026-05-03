using planificApp.Data;

namespace planificApp.ViewModels;

public partial class AreaInteresViewModel : PageViewModel
{
    public AreaInteresViewModel()
    {
        PageName = ApplicationPageNames.AreaInteres;
    }
    
    public string Test { get; set; } = "Calendario Semanal";
}