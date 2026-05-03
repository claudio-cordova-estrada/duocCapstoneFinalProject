using planificApp.Data;

namespace planificApp.ViewModels;

public partial class CalendarioMensualViewModel : PageViewModel
{
    public CalendarioMensualViewModel()
    {
        PageName = ApplicationPageNames.CalendarioMensual;
    }
    public string Test { get; set; } = "Calendario Mensual";
}