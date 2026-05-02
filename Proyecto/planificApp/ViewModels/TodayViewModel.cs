using planificApp.Data;

namespace planificApp.ViewModels;

public partial class TodayViewModel : PageViewModel
{

    public TodayViewModel()
    {
        PageName = ApplicationPageNames.Today;
    }
    public string Test { get; set; } = "Today";
}