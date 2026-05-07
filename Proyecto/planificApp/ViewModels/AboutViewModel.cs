using planificApp.Data;

namespace planificApp.ViewModels;

public partial class AboutViewModel : PageViewModel
{
    public AboutViewModel()
    {
        PageName = ApplicationPageNames.UserSobre;
    }
    
    public string Test { get; set; } = "About";
}