using GuiasBackend.Models;
using System.Threading.Tasks;

namespace GuiasBackend.Services.Interfaces
{
    public interface IAuthService
    {
        /// <summary>
        /// Autentica un usuario usando su nombre de usuario y contraseña
        /// </summary>
        /// <param name="username">Nombre de usuario</param>
        /// <param name="password">Contraseña</param>
        /// <returns>Un tuple con el resultado de la autenticación, el token JWT, el rol del usuario y el ID del usuario si fue exitosa</returns>
        Task<(bool success, string token, string? role, int userId)> AuthenticateAsync(string username, string password);

        /// <summary>
        /// Valida un token JWT
        /// </summary>
        /// <param name="token">Token JWT a validar</param>
        /// <returns>True si el token es válido, false en caso contrario</returns>
        Task<bool> ValidateTokenAsync(string token);

        /// <summary>
        /// Revoca el token de un usuario
        /// </summary>
        /// <param name="token">Token JWT a revocar</param>
        /// <returns>True si se revocó exitosamente, false en caso contrario</returns>
        Task<bool> RevokeTokenAsync(string token);

        /// <summary>
        /// Solicita un código de recuperación de contraseña y lo envía al correo del usuario
        /// </summary>
        /// <param name="email">Correo electrónico del usuario</param>
        /// <returns>True si se envió el código, false si no se encontró el usuario</returns>
        Task<bool> RequestPasswordResetAsync(string email);

        /// <summary>
        /// Verifica que el código de recuperación sea válido
        /// </summary>
        /// <param name="email">Correo electrónico del usuario</param>
        /// <param name="code">Código de verificación</param>
        /// <returns>True si el código es válido, false en caso contrario</returns>
        Task<bool> VerifyResetCodeAsync(string email, string code);

        /// <summary>
        /// Restablece la contraseña del usuario usando el código de verificación
        /// </summary>
        /// <param name="email">Correo electrónico del usuario</param>
        /// <param name="code">Código de verificación</param>
        /// <param name="newPassword">Nueva contraseña</param>
        /// <returns>True si se restableció la contraseña, false en caso contrario</returns>
        Task<bool> ResetPasswordAsync(string email, string code, string newPassword);
    }
}