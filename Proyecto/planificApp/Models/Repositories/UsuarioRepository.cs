using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using PlanificApp.Models.Enums;
using PlanificApp.Models.Repositories.Interfaces;
using PlanificApp.Models.Services;

namespace PlanificApp.Models.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly IMongoCollection<Usuario> _usuarios;

    public UsuarioRepository(MongoContext context)
    {
        _usuarios = context.GetCollection<Usuario>("Usuarios");
    }

    public async Task RegistrarUsuario(Usuario nuevoUsuario)
    {
        var existe = await _usuarios.Find(u => u.Correo == nuevoUsuario.Correo).AnyAsync();
        if (existe) throw new Exception("El correo ya está registrado.");
        await _usuarios.InsertOneAsync(nuevoUsuario);
    }

    public async Task<Usuario?> BuscarPorCorreo(string correo) =>
        await _usuarios.Find(u => u.Correo == correo).FirstOrDefaultAsync();

    public async Task ActualizarPassword(string correo, string nuevaPasswordHash)
    {
        var filter = Builders<Usuario>.Filter.Eq(u => u.Correo, correo);
        var update = Builders<Usuario>.Update.Set(u => u.PasswordHash, nuevaPasswordHash);
        var result = await _usuarios.UpdateOneAsync(filter, update);
        if (result.MatchedCount == 0) throw new Exception("Usuario no encontrado.");
    }

    public async Task ActualizarFotoPerfil(string idUsuario, string fotoBase64)
    {
        var filter = Builders<Usuario>.Filter.Eq(u => u.IdUsuario, idUsuario);
        var update = Builders<Usuario>.Update.Set(u => u.FotoPerfil, fotoBase64);
        await _usuarios.UpdateOneAsync(filter, update);
    }

    public async Task ActualizarUbicacionActual(string idUsuario, string ubicacionActual)
    {
        var filter = Builders<Usuario>.Filter.Eq(u => u.IdUsuario, idUsuario);
        var update = Builders<Usuario>.Update.Set(u => u.UbicacionActual, ubicacionActual);
        await _usuarios.UpdateOneAsync(filter, update);
    }

    public async Task ActualizarEstadoActivo(string idUsuario, bool estaActivo)
    {
        var filter = Builders<Usuario>.Filter.Eq(u => u.IdUsuario, idUsuario);
        var update = Builders<Usuario>.Update.Set(u => u.EstaActivo, estaActivo);
        await _usuarios.UpdateOneAsync(filter, update);
    }

    public async Task ActualizarDatosPersonales(string idUsuario, string nombreCompleto, string correo, DateTime fecNacimiento, string ubicacion)
    {
        var filter = Builders<Usuario>.Filter.Eq(u => u.IdUsuario, idUsuario);
        var update = Builders<Usuario>.Update
            .Set(u => u.NombreCompleto, nombreCompleto)
            .Set(u => u.Correo, correo)
            .Set(u => u.FecNacimiento, fecNacimiento)
            .Set(u => u.Ubicacion, ubicacion);
        await _usuarios.UpdateOneAsync(filter, update);
    }

    public async Task ActualizarConfigUsuario(string idUsuario, TimeSpan horaInicio, TimeSpan horaFin, string? ubicacionActual, MetodoTransporte? transportePred, double horasSueno, List<DayOfWeek> diasGeneracion)
    {
        var filter = Builders<Usuario>.Filter.Eq(u => u.IdUsuario, idUsuario);
        var update = Builders<Usuario>.Update
            .Set(u => u.HoraInicioJornada, horaInicio)
            .Set(u => u.HoraFinJornada, horaFin)
            .Set(u => u.UbicacionActual, ubicacionActual)
            .Set(u => u.TransportePred, transportePred)
            .Set(u => u.HorasSueno, horasSueno)
            .Set(u => u.DiasGeneracionSemanal, diasGeneracion);
        await _usuarios.UpdateOneAsync(filter, update);
    }

    // --- NUEVOS MÉTODOS PARA EL ADMINISTRADOR ---

    public async Task<IEnumerable<Usuario>> ObtenerTodosLosUsuarios()
    {
        // El filtro "_ => true" le dice a Mongo que traiga absolutamente todos los documentos
        return await _usuarios.Find(_ => true).ToListAsync();
    }

    public async Task ActualizarUsuario(string idUsuario, Usuario usuarioActualizado)
    {
        // Reemplaza el usuario antiguo por la versión modificada en el panel de Admin
        await _usuarios.ReplaceOneAsync(u => u.IdUsuario == idUsuario, usuarioActualizado);
    }
}