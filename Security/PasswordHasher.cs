using System.Security.Cryptography;
using System.Text;

namespace GuiasBackend.Security
{
    public static class PasswordHasher
    {
        private const int SaltSize = 32;
        private const int HashSize = 32;
        private const int Iterations = 100000;

        // Encriptar contraseña con PBKDF2
        public static string HashPassword(string password)
        {
            using var rng = RandomNumberGenerator.Create();
            byte[] salt = new byte[SaltSize];
            rng.GetBytes(salt);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            byte[] hash = pbkdf2.GetBytes(HashSize);

            return Convert.ToBase64String(salt.Concat(hash).ToArray());
        }

        // Verificar contraseña usando PBKDF2
        public static bool VerifyPassword(string enteredPassword, string? storedHash)
        {
            if (string.IsNullOrEmpty(storedHash))
                return false;

            byte[] storedBytes = Convert.FromBase64String(storedHash);
            byte[] salt = storedBytes[..SaltSize];
            byte[] storedHashBytes = storedBytes[SaltSize..];

            using var pbkdf2 = new Rfc2898DeriveBytes(enteredPassword, salt, Iterations, HashAlgorithmName.SHA256);
            byte[] enteredHashBytes = pbkdf2.GetBytes(HashSize);

            return storedHashBytes.SequenceEqual(enteredHashBytes);
        }
    }
}
