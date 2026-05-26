using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using planificApp.ViewModels;

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

        BtnCambiarFoto.PointerEntered += Avatar_PointerEntered;
        BtnCambiarFoto.PointerExited += Avatar_PointerExited;
    }

    private void Avatar_PointerEntered(object? sender, PointerEventArgs e)
    {
        AvatarOverlay.IsVisible = true;
    }

    private void Avatar_PointerExited(object? sender, PointerEventArgs e)
    {
        AvatarOverlay.IsVisible = false;
    }

    private async void BtnCambiarFoto_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var storageProvider = topLevel.StorageProvider;

        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Seleccionar foto de perfil",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Imágenes")
                {
                    Patterns = new[] { "*.png", "*.jpg", "*.jpeg" }
                }
            }
        });

        if (files.Count == 0) return;

        await using var stream = await files[0].OpenReadAsync();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        var bytes = ms.ToArray();

        if (DataContext is DatosViewModel vm)
        {
            await vm.GuardarFotoAsync(bytes);
        }
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