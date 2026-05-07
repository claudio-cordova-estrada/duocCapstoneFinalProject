using planificApp.Data;

namespace planificApp.ViewModels;

public partial class SugerenciasSemanalesViewModel : PageViewModel
{
    public SugerenciasSemanalesViewModel()
    {
        PageName = ApplicationPageNames.UserSugerencias;
    }
    public string Test { get; set; } = "Sugerencias Semanales";
}