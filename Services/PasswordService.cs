using System.Linq;
using System.Collections.Generic;

namespace GuiasBackend.Services
{
    public interface IPasswordService
    {
        (bool isValid, string? warning) ValidatePassword(string password);
    }

    public class PasswordService : IPasswordService
    {
        public (bool isValid, string? warning) ValidatePassword(string password)
        {
            var warning = new List<string>();

            if (password.Length < 8)
                warning.Add("La contraseña debe tener al menos 8 caracteres");

            if (!password.Any(char.IsUpper))
                warning.Add("La contraseña debe contener al menos una mayúscula");

            if (!password.Any(char.IsLower))
                warning.Add("La contraseña debe contener al menos una minúscula");

            if (!password.Any(char.IsDigit))
                warning.Add("La contraseña debe contener al menos un número");

            if (!password.Any(c => !char.IsLetterOrDigit(c)))
                warning.Add("La contraseña debe contener al menos un carácter especial");

            return (warning.Count == 0, warning.Count > 0 ? string.Join(". ", warning) : null);
        }
    }
} 