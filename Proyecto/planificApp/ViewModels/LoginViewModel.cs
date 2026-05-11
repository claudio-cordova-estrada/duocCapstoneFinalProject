using planificApp.Data;

namespace planificApp.ViewModels;

public partial class LoginViewModel : PageViewModel
{
    public MainViewModel Main { get; set; }
    
    public LoginViewModel()
    {
        PageName = ApplicationPageNames.Login;
    }
}