using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using PlanificApp.Models;
using PlanificApp.Models.Services;
using System;

namespace PlanificApp
{
    public partial class MainWindow : Window
    {
        private readonly MongoService _mongoService;

        public MainWindow()
        {
            InitializeComponent();
            _mongoService = new MongoService(); // Conecta automáticamente al disco D:
        }

        private async void OnRegistrarClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var nuevoUsuario = new Usuario
                {
                    NombreCompleto = TxtNombre.Text,
                    Correo = TxtCorreo.Text,
                    PasswordHash = TxtPassword.Text // Mañana implementas el hash, hoy es funcional
                };

                await _mongoService.RegistrarUsuario(nuevoUsuario);

                LblStatus.Text = "¡Usuario registrado en Disco D!";
                LblStatus.Foreground = Brushes.LightGreen;
            }
            catch (Exception ex)
            {
                LblStatus.Text = $"Error: {ex.Message}";
                LblStatus.Foreground = Brushes.Red;
            }
        }
    }
}