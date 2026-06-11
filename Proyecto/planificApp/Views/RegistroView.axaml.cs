using Avalonia.Controls;
using Avalonia.Interactivity;
using planificApp.ViewModels;
using System;

namespace planificApp.Views
{
    public partial class RegistroView : UserControl
    {
        public RegistroView()
        {
            InitializeComponent();
        }

        private async void CrearCuenta_Click(object? sender, RoutedEventArgs e)
        {
            // Verificamos que el DataContext sea nuestro ViewModel
            if (DataContext is RegistroViewModel viewModel)
            {
                // Le pasamos la fecha seleccionada del control visual al ViewModel
                viewModel.FecNacimiento = DatePickerNacimiento.SelectedDate;

                // Ejecutamos el comando de registro original
                await viewModel.RegistroCommand.ExecuteAsync(null);

                // Si tu lógica original navegaba tras el registro exitoso:
                if (viewModel.RegistroExitoso)
                {
                    // Asumo que aquí tenías alguna lógica para volver al Login o avanzar
                    viewModel.GoToLoginCommand?.Execute(null);
                }
            }
        }
    }
}