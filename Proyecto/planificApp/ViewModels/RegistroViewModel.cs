using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using planificApp.Helpers;
using planificApp.Data;
using planificApp.Services;
using PlanificApp.Models;
using PlanificApp.Models.Repositories.Interfaces;
using PlanificApp.Models.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace planificApp.ViewModels
{
    public partial class RegistroViewModel : PageViewModel // O ViewModelBase, según uses en tu app
    {
        // --- SERVICIOS INYECTADOS ---
        private readonly IRegionesService _regionesService;
        private readonly INavigationService _navigationService;
        private readonly IUsuarioRepository _usuarioRepo;

        // --- PROPIEDADES DE TEXTO ---
        [ObservableProperty] private string _nombreCompleto = string.Empty;
        [ObservableProperty] private string _correo = string.Empty;
        [ObservableProperty] private string _password = string.Empty;
        [ObservableProperty] private string _repetirPassword = string.Empty;

        // --- PROPIEDADES DE CONTROL VISUAL ---
        [ObservableProperty] private string _errorMessage = string.Empty;
        [ObservableProperty] private bool _hasError;
        public bool RegistroExitoso { get; private set; }
        public DateTime? FecNacimiento { get; set; }

        // --- SISTEMA DE REGIONES Y COMUNAS ---
        public ObservableCollection<string> Regiones { get; } = new();
        public ObservableCollection<string> ComunasDisponibles { get; } = new();

        private string? _regionSeleccionada;
        public string? RegionSeleccionada
        {
            get => _regionSeleccionada;
            set
            {
                if (SetProperty(ref _regionSeleccionada, value))
                {
                    _ = ActualizarComunasAsync();
                }
            }
        }

        [ObservableProperty] private string? _comunaSeleccionada;
        [ObservableProperty] private bool _comunasHabilitadas;

        // --- COMANDOS ---
        public ICommand GoToLoginCommand { get; }

        // --- CONSTRUCTOR ---
        public RegistroViewModel(IRegionesService regionesService, INavigationService navigationService, IUsuarioRepository usuarioRepo)
        {
            _regionesService = regionesService;
            _navigationService = navigationService;
            _usuarioRepo = usuarioRepo;

            GoToLoginCommand = new RelayCommand(VolverAlLogin);

            // Iniciamos la carga de regiones al abrir la ventana
            _ = CargarRegionesAsync();
        }

        // --- LÓGICA PRINCIPAL DE REGISTRO ---
        [RelayCommand]
        public async Task RegistroAsync()
        {
            HasError = false;
            ErrorMessage = string.Empty;
            RegistroExitoso = false;

            // 1. Limpieza de espacios accidentales al inicio o final
            NombreCompleto = NombreCompleto?.Trim() ?? string.Empty;
            Correo = Correo?.Trim() ?? string.Empty;

            // 2. Validación del Nombre
            if (string.IsNullOrWhiteSpace(NombreCompleto) || NombreCompleto.Length < 3)
            {
                ErrorMessage = "El nombre debe tener al menos 3 caracteres.";
                HasError = true; return;
            }

            // Usamos el mismo Regex que tienes en tu clase Usuario para consistencia visual
            if (!Regex.IsMatch(NombreCompleto, @"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$"))
            {
                ErrorMessage = "El nombre solo puede contener letras y espacios.";
                HasError = true; return;
            }

            // 3. Validación del Correo Electrónico (Regex estándar para emails)
            if (string.IsNullOrWhiteSpace(Correo) || !Regex.IsMatch(Correo, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                ErrorMessage = "Ingresa un correo electrónico válido (ej: usuario@correo.com).";
                HasError = true; return;
            }

            // 4. Validación de Contraseñas
            if (string.IsNullOrWhiteSpace(Password) || Password.Length < 6)
            {
                ErrorMessage = "La contraseña debe tener al menos 6 caracteres.";
                HasError = true; return;
            }

            if (Password != RepetirPassword)
            {
                ErrorMessage = "Las contraseñas no coinciden.";
                HasError = true; return;
            }

            // 5. Validación de la Fecha de Nacimiento (Viajeros en el tiempo y edad mínima)
            if (FecNacimiento == null)
            {
                ErrorMessage = "Por favor, selecciona tu fecha de nacimiento.";
                HasError = true; return;
            }

            if (FecNacimiento.Value > DateTime.Now)
            {
                ErrorMessage = "La fecha de nacimiento no puede ser en el futuro.";
                HasError = true; return;
            }

            // Opcional: Validar que el usuario tenga una edad lógica (ej. mínimo 14 años)
            if (FecNacimiento.Value > DateTime.Now.AddYears(-14))
            {
                ErrorMessage = "Debes tener al menos 14 años para registrarte.";
                HasError = true; return;
            }

            // 6. Validación de Ubicación
            if (string.IsNullOrWhiteSpace(RegionSeleccionada) || string.IsNullOrWhiteSpace(ComunaSeleccionada))
            {
                ErrorMessage = "Por favor, selecciona tu región y comuna.";
                HasError = true; return;
            }

            // ... A partir de aquí, continúa con tu bloque try { } normal ...
            try
            {
                // 2. Validar que el correo no exista en MongoDB
                var usuarioExistente = await _usuarioRepo.BuscarPorCorreo(Correo);
                if (usuarioExistente != null)
                {
                    ErrorMessage = "Este correo electrónico ya está registrado.";
                    HasError = true;
                    return;
                }

                // 3. Formatear la ubicación
                string ubicacionFinal = $"{ComunaSeleccionada}, {RegionSeleccionada}";

                // 4. Crear la instancia exacta de tu modelo Usuario
                var nuevoUsuario = new Usuario
                {
                    NombreCompleto = NombreCompleto,
                    Correo = Correo,

                    PasswordHash = PasswordHelper.HashPassword(Password),

                    FecNacimiento = FecNacimiento.Value,
                    Ubicacion = ubicacionFinal,
                    FecCreacion = DateTime.UtcNow,
                    CuentaConfirmada = false,
                    HoraInicioJornada = TimeSpan.FromHours(7.5),
                    HoraFinJornada = TimeSpan.FromHours(22),
                    UbicacionActual = "Casa",
                    HorasSueno = 7.5,
                    DiasGeneracionSemanal = new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday }
                };

                // 5. Guardar en MongoDB
                await _usuarioRepo.RegistrarUsuario(nuevoUsuario);

                // 6. Éxito
                RegistroExitoso = true;
            }
            catch (ArgumentException argEx)
            {
                // Atrapa el error si el Regex de NombreCompleto falla
                ErrorMessage = argEx.Message;
                HasError = true;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al registrar: {ex.Message}";
                HasError = true;
            }
        }

        // --- MÉTODOS PRIVADOS ---
        private async Task CargarRegionesAsync()
        {
            var regiones = await _regionesService.ObtenerRegionesAsync();
            foreach (var region in regiones)
            {
                Regiones.Add(region);
            }
        }

        private async Task ActualizarComunasAsync()
        {
            ComunasDisponibles.Clear();
            ComunaSeleccionada = null;

            if (!string.IsNullOrEmpty(RegionSeleccionada))
            {
                var comunas = await _regionesService.ObtenerComunasPorRegionAsync(RegionSeleccionada);
                foreach (var comuna in comunas)
                {
                    ComunasDisponibles.Add(comuna);
                }
                ComunasHabilitadas = true;
            }
            else
            {
                ComunasHabilitadas = false;
            }
        }

        private void VolverAlLogin()
        {
            _navigationService?.NavigateToPage(ApplicationPageNames.Login);
        }
    }
}