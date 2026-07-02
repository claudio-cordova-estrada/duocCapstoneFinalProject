using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using planificApp.ViewModels;

namespace planificApp.Views;

public partial class EstadisticasView : UserControl
{
    public EstadisticasView()
    {
        InitializeComponent();
        // Enfocable para poder "recibir" el foco y así soltar el del buscador al click afuera.
        Focusable = true;
        AddHandler(PointerPressedEvent, OnRootPointerPressed, RoutingStrategies.Bubble);
        AddHandler(KeyDownEvent, OnRootKeyDown, RoutingStrategies.Bubble);
    }

    // Al hacer click en una zona vacía (fuera del buscador), movemos el foco a la vista para que el
    // AutoCompleteBox se "desmarque" — por defecto no libera el foco al clickear en no-enfocables.
    private void OnRootPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is Visual v && v.GetSelfAndVisualAncestors().OfType<AutoCompleteBox>().Any())
            return;

        Focus();
    }

    // Enter dentro del buscador → navegar al usuario que matchea el texto (cuando no se eligió una sugerencia).
    private void OnRootKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;

        if (e.Source is Visual v && v.GetSelfAndVisualAncestors().OfType<AutoCompleteBox>().Any()
            && DataContext is EstadisticasViewModel vm)
        {
            vm.IrAUsuarioPorTextoCommand.Execute(null);
        }
    }
}
