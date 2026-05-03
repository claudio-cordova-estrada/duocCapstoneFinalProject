using planificApp.Data;

namespace planificApp.ViewModels;

public partial class SettingsViewModel : PageViewModel
{
    public SettingsViewModel()
    {
        PageName = ApplicationPageNames.Settings;
    }
    public string Test { get; set; } = "Settings";
}