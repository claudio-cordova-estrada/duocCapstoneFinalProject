using planificApp.Data;

namespace planificApp.ViewModels;

public partial class SoporteViewModel : PageViewModel
{
    public SoporteViewModel()
    {
        PageName = ApplicationPageNames.Soporte;
    }
    public string Test { get; set; } = "Soporte";
}