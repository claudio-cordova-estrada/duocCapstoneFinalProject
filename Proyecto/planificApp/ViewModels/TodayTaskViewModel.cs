using planificApp.Data;

namespace planificApp.ViewModels;

public partial class TodayTaskViewModel : PageViewModel
{

    public TodayTaskViewModel()
    {
        PageName = ApplicationPageNames.TodayTask;
    }
    public string Test { get; set; } = "Today";
}