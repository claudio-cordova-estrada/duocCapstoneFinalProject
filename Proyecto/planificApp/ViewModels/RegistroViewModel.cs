using planificApp.Data;

namespace planificApp.ViewModels;

public partial class RegistroViewModel : PageViewModel
{
    public RegistroViewModel()
    {
        PageName = ApplicationPageNames.Registro;
    }
    public string Test { get; set; } = "Registro";
}