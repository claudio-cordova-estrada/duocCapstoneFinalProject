using planificApp.Data;

namespace planificApp.ViewModels;

public partial class InboxViewModel : PageViewModel
{
    public InboxViewModel()
    {
        PageName = ApplicationPageNames.Inbox;
    }
    public string Test { get; set; } = "Inbox";
}