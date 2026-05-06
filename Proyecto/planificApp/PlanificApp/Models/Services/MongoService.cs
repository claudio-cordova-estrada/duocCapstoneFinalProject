using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlanificApp.Models;

namespace PlanificApp.Models.Services
{
    public class MongoService
    {
        private readonly IMongoDatabase _database;

        public MongoService()
        {
            // Configuración de la conexión a MongoDB Local
            var client = new MongoClient("mongodb://localhost:27017");
            _database = client.GetDatabase("PlanificAppDB");
        }

        // Colecciones vinculadas a tus modelos
        public IMongoCollection<Usuario> Usuarios => _database.GetCollection<Usuario>("Usuarios");
        public IMongoCollection<Tarea> Tareas => _database.GetCollection<Tarea>("Tareas");

        // --- SECCIÓN: AUTENTICACIÓN Y USUARIOS ---

        public async Task RegistrarUsuario(Usuario nuevoUsuario)
        {
            // Verificamos si el correo ya existe para evitar duplicados
            var existe = await Usuarios.Find(u => u.Correo == nuevoUsuario.Correo).AnyAsync();
            if (existe) throw new System.Exception("El correo ya está registrado.");

            await Usuarios.InsertOneAsync(nuevoUsuario);
        }

        public async Task<Usuario?> Login(string correo, string password)
        {
            // Buscamos el usuario por su correo validado
            var usuario = await Usuarios.Find(u => u.Correo == correo).FirstOrDefaultAsync();

            // Verificación básica (En un paso posterior añadiremos Hash de contraseña)
            if (usuario != null && usuario.PasswordHash == password) return usuario;

            return null;
        }

        // --- SECCIÓN: ACCIONES DE TAREA (CRUD) ---

        public async Task CrearTarea(Tarea nuevaTarea) =>
            await Tareas.InsertOneAsync(nuevaTarea); 

        public async Task<List<Tarea>> ObtenerTareasPorUsuario(string areaId) =>
            await Tareas.Find(t => t.IdAreaInteres == areaId).ToListAsync(); 

        public async Task ActualizarTarea(string id, Tarea tareaActualizada) =>
            await Tareas.ReplaceOneAsync(t => t.IdTarea == id, tareaActualizada); 

        public async Task EliminarTarea(string id) =>
            await Tareas.DeleteOneAsync(t => t.IdTarea == id);
    }
}