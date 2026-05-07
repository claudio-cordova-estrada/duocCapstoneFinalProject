using planificApp.Data;

namespace planificApp.ViewModels;

public partial class SoporteViewModel : PageViewModel
{
    public SoporteViewModel()
    {
        PageName = ApplicationPageNames.UserSoporte;
    }
    public string Test { get; set; } = "Soporte";
}