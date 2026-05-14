using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace planificApp.Views;

public partial class DatosView : UserControl
{
    private readonly SolidColorBrush _readonlyBg = new(Color.Parse("#00000000"));
    private readonly SolidColorBrush _readonlyBorder = new(Color.Parse("#00000000"));
    private readonly SolidColorBrush _editBg = new(Color.Parse("#0f0f0f"));
    private readonly SolidColorBrush _editBorder = new(Color.Parse("#3d3060"));
    private readonly SolidColorBrush _fieldFg = new(Color.Parse("#cccccc"));

    private readonly Dictionary<TextBox, Label> _fieldIcons = new();

    public DatosView()
    {
        InitializeComponent();

        _fieldIcons[FieldNombre] = IconNombre;
        _fieldIcons[FieldCorreo] = IconCorreo;
        _fieldIcons[FieldFecha] = IconFecha;
        _fieldIcons[FieldUbicacion] = IconUbicacion;

        FieldNombre.GotFocus += Field_GotFocus;
        FieldCorreo.GotFocus += Field_GotFocus;
        FieldFecha.GotFocus += Field_GotFocus;
        FieldUbicacion.GotFocus += Field_GotFocus;
    }

    private void EnterEditMode(TextBox field)
    {
        if (!field.IsReadOnly) return;

        field.IsReadOnly = false;
        field.Background = _editBg;
        field.BorderBrush = _editBorder;
        field.BorderThickness = new Thickness(1);
        field.Foreground = _fieldFg;

        var icon = _fieldIcons[field];
        icon.Content = "\xEBA6";
        icon.Foreground = new SolidColorBrush(Color.Parse("#a78bfa"));

        field.Focus();
        field.SelectAll();
    }

    private void ExitEditMode(TextBox field)
    {
        if (field.IsReadOnly) return;

        field.IsReadOnly = true;
        field.Background = _readonlyBg;
        field.BorderBrush = _readonlyBorder;
        field.BorderThickness = new Thickness(0);
        field.Foreground = _fieldFg;

        var icon = _fieldIcons[field];
        icon.Content = "\xE3B2";
        icon.Foreground = new SolidColorBrush(Color.Parse("#888888"));
    }

    private void Field_GotFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb && tb.IsReadOnly)
            EnterEditMode(tb);
    }

    private void BtnNombre_Click(object? sender, RoutedEventArgs e)
    {
        if (FieldNombre.IsReadOnly) EnterEditMode(FieldNombre);
        else ExitEditMode(FieldNombre);
    }

    private void BtnCorreo_Click(object? sender, RoutedEventArgs e)
    {
        if (FieldCorreo.IsReadOnly) EnterEditMode(FieldCorreo);
        else ExitEditMode(FieldCorreo);
    }

    private void BtnFecha_Click(object? sender, RoutedEventArgs e)
    {
        if (FieldFecha.IsReadOnly) EnterEditMode(FieldFecha);
        else ExitEditMode(FieldFecha);
    }

    private void BtnUbicacion_Click(object? sender, RoutedEventArgs e)
    {
        if (FieldUbicacion.IsReadOnly) EnterEditMode(FieldUbicacion);
        else ExitEditMode(FieldUbicacion);
    }
}