using System.Threading.Tasks;

namespace GuiasBackend.Services.Interfaces
{
    public interface IEmailService
    {
        /// <summary>
        /// Envía un correo electrónico
        /// </summary>
        /// <param name="to">Dirección de correo del destinatario</param>
        /// <param name="subject">Asunto del correo</param>
        /// <param name="body">Cuerpo del correo (puede ser HTML)</param>
        /// <returns>True si se envió correctamente, false en caso contrario</returns>
        Task<bool> SendEmailAsync(string to, string subject, string body);
    }
} 