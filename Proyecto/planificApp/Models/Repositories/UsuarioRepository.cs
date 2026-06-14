using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using PlanificApp.Models;
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
        var usuario = await _usuarios.Find(u => u.Correo == correo).FirstOrDefaultAsync();
        if (usuario == null) throw new Exception("Usuario no encontrado.");
        usuario.PasswordHash = nuevaPasswordHash;
        await _usuarios.ReplaceOneAsync(u => u.IdUsuario == usuario.IdUsuario, usuario);
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