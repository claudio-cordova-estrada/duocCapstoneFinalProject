using planificApp.Data;

namespace planificApp.ViewModels;

public partial class TodayTaskViewModel : PageViewModel
{

    public TodayTaskViewModel()
    {
        PageName = ApplicationPageNames.UserHoy;
    }
    public string Test { get; set; } = "Today";
}