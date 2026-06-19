using System;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace PlanificApp.Models.Services;

public class MongoContext
{
    private readonly IMongoDatabase _database;

    public MongoContext(IConfiguration configuration)
    {
        var usuario = configuration["MongoDb:Usuario"]
            ?? throw new InvalidOperationException("MongoDb:Usuario no encontrado en appsettings.json.");
        var contrasena = configuration["MongoDb:Contrasena"]
            ?? throw new InvalidOperationException("MongoDb:Contrasena no encontrado en appsettings.json.");
        var nombreRepo = configuration["MongoDb:NombreRepo"]
            ?? throw new InvalidOperationException("MongoDb:NombreRepo no encontrado en appsettings.json.");

        var connectionString = $"mongodb+srv://{usuario}:{contrasena}@planificapp.a1hyplb.mongodb.net/?retryWrites=true&w=majority";
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(nombreRepo);
    }

    public IMongoCollection<T> GetCollection<T>(string collectionName) =>
        _database.GetCollection<T>(collectionName);
}