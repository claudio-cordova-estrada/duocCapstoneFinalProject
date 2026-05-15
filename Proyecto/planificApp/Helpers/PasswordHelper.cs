using System;
using System.Linq;
using System.Security.Cryptography;

namespace planificApp.Helpers;

public static class PasswordHelper
{
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public static string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        using var sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(salt.Concat(System.Text.Encoding.UTF8.GetBytes(password)).ToArray());
        return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);
    }

    public static bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split(':');
        if (parts.Length != 2) return false;
        byte[] salt = Convert.FromBase64String(parts[0]);
        byte[] storedPasswordHash = Convert.FromBase64String(parts[1]);
        using var sha256 = SHA256.Create();
        byte[] computedHash = sha256.ComputeHash(salt.Concat(System.Text.Encoding.UTF8.GetBytes(password)).ToArray());
        return computedHash.SequenceEqual(storedPasswordHash);
    }
}
