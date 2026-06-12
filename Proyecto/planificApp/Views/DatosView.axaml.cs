using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using planificApp.Services;
using planificApp.ViewModels;
using PlanificApp.Models;
using PlanificApp.Models.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace planificApp.Views;

public partial class DatosView : UserControl
{
    private readonly SolidColorBrush _readonlyBg = new(Color.Parse("#00000000"));
    private readonly SolidColorBrush _readonlyBorder = new(Color.Parse("#00000000"));
    private readonly SolidColorBrush _editBg = new(Color.Parse("#0f0f0f"));
    private readonly SolidColorBrush _editBorder = new(Color.Parse("#3d3060"));
    private readonly SolidColorBrush _fieldFg = new(Color.Parse("#cccccc"));

    private readonly Dictionary<TextBox, Label> _fieldIcons = new();
    private readonly Dictionary<TextBox, TextBlock> _fieldErrors = new();

    private bool _isEditingUbicacion = false;

    public DatosView()
    {
        InitializeComponent();

        _fieldIcons[FieldNombre] = IconNombre;
        _fieldIcons[FieldCorreo] = IconCorreo;
        _fieldIcons[FieldFecha] = IconFecha;

        _fieldErrors[FieldNombre] = ErrorNombre;
        _fieldErrors[FieldCorreo] = ErrorCorreo;
        _fieldErrors[FieldFecha] = ErrorFecha;

        FieldNombre.GotFocus += Field_GotFocus;
        FieldCorreo.GotFocus += Field_GotFocus;
        FieldFecha.GotFocus += Field_GotFocus;

        // Asignamos la recarga de comunas dinámicas al cambiar de región
        CmbRegion.SelectionChanged += CmbRegion_SelectionChanged;

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
        icon.Content = "\xEBA6"; // Ícono Check
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
        icon.Content = "\xE3B2"; // Ícono Lápiz
        icon.Foreground = new SolidColorBrush(Color.Parse("#888888"));
    }

    private void Field_GotFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb && tb.IsReadOnly)
            EnterEditMode(tb);
    }

    // --- MANEJO EXCLUSIVO DE EDICIÓN DE UBICACIÓN (REGIONES/COMUNAS) ---
    private async Task EnterUbicacionEditModeAsync()
    {
        _isEditingUbicacion = true;
        FieldUbicacion.IsVisible = false;
        EditUbicacionPanel.IsVisible = true;

        IconUbicacion.Content = "\xEBA6"; // Check
        IconUbicacion.Foreground = new SolidColorBrush(Color.Parse("#a78bfa"));

        // Cargar regiones desde el servicio si no están cargadas
        if (CmbRegion.ItemsSource == null)
        {
            var regionesService = App.Services.GetRequiredService<IRegionesService>();
            var regiones = await regionesService.ObtenerRegionesAsync();
            CmbRegion.ItemsSource = regiones.ToList();
        }

        // Pre-seleccionar automáticamente Región y Comuna basándonos en el texto actual "Comuna, Región"
        string actual = FieldUbicacion.Text;
        if (!string.IsNullOrEmpty(actual) && actual.Contains(","))
        {
            var partes = actual.Split(',');
            string comunaAct = partes[0].Trim();
            string regionAct = partes[1].Trim();

            if (CmbRegion.ItemsSource is List<string> listaRegiones)
            {
                int rIndex = listaRegiones.FindIndex(r => r.Equals(regionAct, StringComparison.OrdinalIgnoreCase));
                if (rIndex >= 0)
                {
                    CmbRegion.SelectedIndex = rIndex;

                    var regionesService = App.Services.GetRequiredService<IRegionesService>();
                    var comunas = await regionesService.ObtenerComunasPorRegionAsync(regionAct);
                    var listaComunas = comunas.ToList();

                    CmbComuna.ItemsSource = listaComunas;
                    CmbComuna.IsEnabled = true;

                    int cIndex = listaComunas.FindIndex(c => c.Equals(comunaAct, StringComparison.OrdinalIgnoreCase));
                    if (cIndex >= 0) CmbComuna.SelectedIndex = cIndex;
                }
            }
        }
    }

    private void ExitUbicacionEditMode()
    {
        _isEditingUbicacion = false;
        EditUbicacionPanel.IsVisible = false;
        FieldUbicacion.IsVisible = true;

        IconUbicacion.Content = "\xE3B2"; // Lápiz
        IconUbicacion.Foreground = new SolidColorBrush(Color.Parse("#888888"));
    }

    private async void CmbRegion_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (CmbRegion.SelectedItem is string regionSeleccionada)
        {
            CmbComuna.ItemsSource = null;
            CmbComuna.IsEnabled = false;
            ErrorUbicacion.IsVisible = false;

            var regionesService = App.Services.GetRequiredService<IRegionesService>();
            var comunas = await regionesService.ObtenerComunasPorRegionAsync(regionSeleccionada);
            CmbComuna.ItemsSource = comunas.ToList();
            CmbComuna.IsEnabled = true;
        }
    }

    // --- VALIDACIONES DE CAMPOS ---
    private bool ValidarCampo(TextBox field)
    {
        string texto = field.Text?.Trim() ?? string.Empty;
        var errorBlock = _fieldErrors[field];

        if (field == FieldNombre)
        {
            if (string.IsNullOrWhiteSpace(texto) || texto.Length < 3)
            {
                MostrarError(errorBlock, "El nombre debe tener al menos 3 caracteres.");
                return false;
            }
            if (!Regex.IsMatch(texto, @"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$"))
            {
                MostrarError(errorBlock, "El nombre solo puede contener letras y espacios.");
                return false;
            }
        }
        else if (field == FieldCorreo)
        {
            if (!Regex.IsMatch(texto, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                MostrarError(errorBlock, "Por favor, ingresa un correo electrónico válido.");
                return false;
            }
        }
        else if (field == FieldFecha)
        {
            if (!DateTime.TryParse(texto, new CultureInfo("es-CL"), DateTimeStyles.None, out DateTime fechaCasteada))
            {
                MostrarError(errorBlock, "Formato inválido. Usa 'dd/mm/aaaa' o '30 abril 2026'.");
                return false;
            }
            if (fechaCasteada > DateTime.Now)
            {
                MostrarError(errorBlock, "La fecha de nacimiento no puede estar en el futuro.");
                return false;
            }
            if (fechaCasteada.Year < 1900)
            {
                MostrarError(errorBlock, "Por favor, ingresa una fecha de nacimiento válida.");
                return false;
            }
            field.Text = fechaCasteada.ToString("dd MMMM yyyy", new CultureInfo("es-CL"));
        }

        errorBlock.IsVisible = false;
        return true;
    }

    private bool ValidarYGuardarUbicacion()
    {
        if (CmbRegion.SelectedItem == null || CmbComuna.SelectedItem == null)
        {
            ErrorUbicacion.Text = "Por favor, selecciona tu región y comuna.";
            ErrorUbicacion.IsVisible = true;
            return false;
        }

        ErrorUbicacion.IsVisible = false;
        string nuevaUbi = $"{CmbComuna.SelectedItem}, {CmbRegion.SelectedItem}";
        FieldUbicacion.Text = nuevaUbi;

        if (DataContext is DatosViewModel vm)
        {
            vm.Ubicacion = nuevaUbi;
        }

        return true;
    }

    private void MostrarError(TextBlock errorBlock, string mensaje)
    {
        errorBlock.Text = mensaje;
        errorBlock.IsVisible = true;
    }

    // --- ACCIONES DE BOTONES ---
    private void BtnNombre_Click(object? sender, RoutedEventArgs e)
    {
        if (FieldNombre.IsReadOnly) EnterEditMode(FieldNombre);
        else if (ValidarCampo(FieldNombre)) ExitEditMode(FieldNombre);
    }

    private void BtnCorreo_Click(object? sender, RoutedEventArgs e)
    {
        if (FieldCorreo.IsReadOnly) EnterEditMode(FieldCorreo);
        else if (ValidarCampo(FieldCorreo)) ExitEditMode(FieldCorreo);
    }

    private void BtnFecha_Click(object? sender, RoutedEventArgs e)
    {
        if (FieldFecha.IsReadOnly) EnterEditMode(FieldFecha);
        else if (ValidarCampo(FieldFecha)) ExitEditMode(FieldFecha);
    }

    private async void BtnUbicacion_Click(object? sender, RoutedEventArgs e)
    {
        if (!_isEditingUbicacion)
        {
            await EnterUbicacionEditModeAsync();
        }
        else
        {
            if (ValidarYGuardarUbicacion())
            {
                ExitUbicacionEditMode();
            }
        }
    }
}