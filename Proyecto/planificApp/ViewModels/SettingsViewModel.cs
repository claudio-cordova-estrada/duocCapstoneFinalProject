using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;
﻿using Avalonia.Controls;
using Avalonia.Interactivity;
using planificApp.Data;

namespace planificApp.ViewModels;

public partial class SettingsViewModel : PageViewModel
{
    public SettingsViewModel()
    {
        PageName = ApplicationPageNames.UserConfig;
    }
    public string Test { get; set; } = "Settings";

    public void OnDiaCommand(object? parameter)
    {
        if (parameter is Button button)
        {
            if (button.Classes.Contains("outline"))
            {
                button.Classes.Remove("outline");
            }
            else
            {
                button.Classes.Add("outline");
            }
        }
    }
}