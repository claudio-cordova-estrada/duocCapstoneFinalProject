using Avalonia.Controls;

namespace planificApp.Views;

public partial class NewTaskWindow : Window
{
    public NewTaskWindow()
    {
        InitializeComponent();
        SaveButton.Click += (_, _) => Close(true);
        CancelButton.Click += (_, _) => Close(false);
    }
}