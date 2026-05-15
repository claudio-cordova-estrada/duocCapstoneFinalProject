using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using planificApp.Helpers;
using PlanificApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PlanificApp.Models.Services
{
    public class MongoService
    {
        private readonly IMongoDatabase _database;

        public MongoService()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var usuario = config["MongoDb:Usuario"]
                ?? throw new InvalidOperationException("MongoDb:Usuario no encontrado en appsettings.json.");
            var contrasena = config["MongoDb:Contrasena"]
                ?? throw new InvalidOperationException("MongoDb:Contrasena no encontrado en appsettings.json.");
            var nombreRepo = config["MongoDb:NombreRepo"]
                ?? throw new InvalidOperationException("MongoDb:NombreRepo no encontrado en appsettings.json.");

            var connectionString = $"mongodb+srv://{usuario}:{contrasena}@planificapp.a1hyplb.mongodb.net/";

            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(nombreRepo);
        }

        public IMongoCollection<Usuario> Usuarios =>
            _database.GetCollection<Usuario>("Usuarios");
        public IMongoCollection<Tarea> Tareas =>
            _database.GetCollection<Tarea>("Tareas");
        public IMongoCollection<AreaInteres> AreasInteres =>
            _database.GetCollection<AreaInteres>("AreasInteres");

        public async Task RegistrarUsuario(Usuario nuevoUsuario)
        {
            var existe = await Usuarios.Find(u => u.Correo == nuevoUsuario.Correo).AnyAsync();
            if (existe) throw new Exception("El correo ya está registrado.");

            await Usuarios.InsertOneAsync(nuevoUsuario);
        }

        public async Task<Usuario?> Login(string correo, string password)
        {
            var usuario = await Usuarios.Find(u => u.Correo == correo).FirstOrDefaultAsync();
            if (usuario == null) return null;
            if (!PasswordHelper.VerifyPassword(password, usuario.PasswordHash)) return null;
            return usuario;
        }

        public async Task<Usuario?> BuscarPorCorreo(string correo) =>
            await Usuarios.Find(u => u.Correo == correo).FirstOrDefaultAsync();

        public async Task ActualizarPassword(string correo, string nuevaPasswordHash)
        {
            var usuario = await Usuarios.Find(u => u.Correo == correo).FirstOrDefaultAsync();
            if (usuario == null) throw new Exception("Usuario no encontrado.");

            usuario.PasswordHash = nuevaPasswordHash;
            await Usuarios.ReplaceOneAsync(u => u.IdUsuario == usuario.IdUsuario, usuario);
        }

        public async Task CrearTarea(Tarea nuevaTarea) =>
            await Tareas.InsertOneAsync(nuevaTarea);

        public async Task<List<Tarea>> ObtenerTareasPorArea(string areaId) =>
            await Tareas.Find(t => t.IdAreaInteres == areaId).ToListAsync();

        public async Task<List<Tarea>> ObtenerTareasPorUsuario(string idUsuario)
        {
            var areas = await AreasInteres.Find(a => a.IdUsuario == idUsuario).ToListAsync();
            var areaIds = areas.Select(a => a.IdAreaInteres).ToList();
            return await Tareas.Find(t => areaIds.Contains(t.IdAreaInteres)).ToListAsync();
        }

        public async Task ActualizarTarea(string id, Tarea tareaActualizada) =>
            await Tareas.ReplaceOneAsync(t => t.IdTarea == id, tareaActualizada);

        public async Task EliminarTarea(string id) =>
            await Tareas.DeleteOneAsync(t => t.IdTarea == id);
    }
}
