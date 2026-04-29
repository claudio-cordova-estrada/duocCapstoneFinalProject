using MongoDB.Driver;
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

        public IMongoCollection<Usuario> Usuarios => _database.GetCollection<Usuario>("Usuarios");
        public IMongoCollection<Tarea> Tareas => _database.GetCollection<Tarea>("Tareas");
        public IMongoCollection<AreaInteres> Areas => _database.GetCollection<AreaInteres>("AreasInteres");
    }
}