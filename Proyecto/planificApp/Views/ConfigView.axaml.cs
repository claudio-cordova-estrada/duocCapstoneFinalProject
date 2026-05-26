using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace planificApp.Views;

public partial class ConfigView : UserControl
{
    private readonly SolidColorBrush _readonlyBg = new(Color.Parse("#00000000"));
    private readonly SolidColorBrush _readonlyBorder = new(Color.Parse("#00000000"));
    private readonly SolidColorBrush _editBg = new(Color.Parse("#0f0f0f"));
    private readonly SolidColorBrush _editBorder = new(Color.Parse("#3d3060"));
    private readonly SolidColorBrush _fieldFg = new(Color.Parse("#cccccc"));

    private readonly Dictionary<TextBox, Label> _fieldIcons = new();

    public ConfigView()
    {
        InitializeComponent();

        _fieldIcons[FieldInicioHoras] = IconHoraInicio;
        _fieldIcons[FieldInicioMinutos] = IconHoraInicio;
        _fieldIcons[FieldFinHoras] = IconHoraFin;
        _fieldIcons[FieldFinMinutos] = IconHoraFin;

        FieldInicioHoras.GotFocus += Field_GotFocus;
        FieldInicioMinutos.GotFocus += Field_GotFocus;
        FieldFinHoras.GotFocus += Field_GotFocus;
        FieldFinMinutos.GotFocus += Field_GotFocus;
    }

    private void EnterEditMode(TextBox field)
    {
        if (!field.IsReadOnly) return;

        field.IsReadOnly = false;
        field.Background = _editBg;
        field.BorderBrush = _editBorder;
        field.BorderThickness = new Thickness(1);
        field.Foreground = _fieldFg;
        field.Focus();
        field.SelectAll();

        var icon = _fieldIcons[field];
        icon.Content = "\xEBA6";
        icon.Foreground = new SolidColorBrush(Color.Parse("#a78bfa"));
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

    private void EnterPair(TextBox horas, TextBox minutos)
    {
        if (!horas.IsReadOnly) return;
        EnterEditMode(horas);
        EnterEditMode(minutos);
    }

    private void ExitPair(TextBox horas, TextBox minutos)
    {
        ExitEditMode(horas);
        ExitEditMode(minutos);
    }

    private void Field_GotFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is not TextBox tb || !tb.IsReadOnly) return;

        if (tb == FieldInicioHoras || tb == FieldInicioMinutos)
            EnterPair(FieldInicioHoras, FieldInicioMinutos);
        else if (tb == FieldFinHoras || tb == FieldFinMinutos)
            EnterPair(FieldFinHoras, FieldFinMinutos);
    }

    private void BtnHoraInicio_Click(object? sender, RoutedEventArgs e)
    {
        if (FieldInicioHoras.IsReadOnly) EnterPair(FieldInicioHoras, FieldInicioMinutos);
        else ExitPair(FieldInicioHoras, FieldInicioMinutos);
    }

    private void BtnHoraFin_Click(object? sender, RoutedEventArgs e)
    {
        if (FieldFinHoras.IsReadOnly) EnterPair(FieldFinHoras, FieldFinMinutos);
        else ExitPair(FieldFinHoras, FieldFinMinutos);
    }

    private void ToggleDia_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            var classes = btn.Classes;
            if (classes.Contains("active"))
                classes.Remove("active");
            else
                classes.Add("active");
        }
    }
}