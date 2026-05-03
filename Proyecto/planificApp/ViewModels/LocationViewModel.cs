using planificApp.Data;

namespace planificApp.ViewModels;

public partial class LocationViewModel : PageViewModel
{
    public LocationViewModel()
    {
        PageName = ApplicationPageNames.Location;
    }
    
    public string Test { get; set; } = "Location";
}