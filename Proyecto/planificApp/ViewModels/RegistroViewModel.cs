using planificApp.Data;

namespace planificApp.ViewModels;

public partial class RegistroViewModel : PageViewModel
{
    public MainViewModel Main { get; set; }
    
    public RegistroViewModel()
    {
        PageName = ApplicationPageNames.Registro;
    }
}