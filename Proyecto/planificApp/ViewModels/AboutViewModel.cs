using planificApp.Data;

namespace planificApp.ViewModels;

public partial class AboutViewModel : PageViewModel
{
    public AboutViewModel()
    {
        PageName = ApplicationPageNames.About;
    }
    
    public string Test { get; set; } = "About";
}