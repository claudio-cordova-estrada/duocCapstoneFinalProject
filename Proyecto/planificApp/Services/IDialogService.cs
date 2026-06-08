using System.Collections.ObjectModel;
using System.Threading.Tasks;
using PlanificApp.Models;
using PlanificApp.Models.Services.Interfaces;
using planificApp.Views;
using planificApp.ViewModels;

namespace planificApp.Services;

public interface IDialogService
{
    Task<bool> ShowNewTaskDialog(string? preSelectedArea = null);
    Task<bool> ShowNewAreaDialog();
    Task<bool> ShowEditAreaDialog(AreaInteres area);
    Task<bool> ShowConfirmDeleteAreaDialog(AreaInteres area);
    void ShowGenerarSemanaDialog();

    Task<LocationFormData?> ShowAddLocationDialog(IGeoService geoService, ObservableCollection<string> areas);
    Task<LocationFormData?> ShowEditLocationDialog(IGeoService geoService, ObservableCollection<string> areas, string nombre, string direccion, string area, string color, string transporte);
    Task ShowRouteCalculatorDialog(IGeoService geoService, ObservableCollection<UbicacionVisual> ubicaciones);
    void ShowConfirmDeleteLocationDialog(string nombre);
}