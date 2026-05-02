using planificApp.Data;

namespace planificApp.ViewModels;

public partial class CalendarioSemanalViewModel : PageViewModel
{
    public CalendarioSemanalViewModel()
    {
        PageName = ApplicationPageNames.CalendarioSemanal;
    }
    public string Test { get; set; } = "Calendario Semanal";
}