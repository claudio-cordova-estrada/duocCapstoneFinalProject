using planificApp.Data;

namespace planificApp.ViewModels;

public partial class LoginViewModel : PageViewModel
{
    public LoginViewModel()
    {
        PageName = ApplicationPageNames.Login;
    }
    public string Test { get; set; } = "Login";
}