using Avalonia;
using Avalonia.Controls;

namespace planificApp.StyleControl;

public class IconButton : Button
{
    public static readonly StyledProperty<string> IconTextProperty = AvaloniaProperty.Register<IconButton, string>(
        nameof(IconText));

    public string IconText
    {
        get => GetValue(IconTextProperty);
        set => SetValue(IconTextProperty, value);
    }
}