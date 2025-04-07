using GuiasBackend.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace GuiasBackend.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _senderEmail;
        private readonly bool _enableSsl;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _smtpServer = configuration["Email:SmtpServer"] ?? "smtp.office365.com";
            _smtpPort = int.TryParse(configuration["Email:SmtpPort"], out int port) ? port : 587;
            _smtpUsername = configuration["Email:Username"] ?? "correo@empresa.com";
            _smtpPassword = configuration["Email:Password"] ?? "contraseña";
            _senderEmail = configuration["Email:SenderEmail"] ?? _smtpUsername;
            
            bool ssl = true; // Valor predeterminado
            bool.TryParse(configuration["Email:EnableSsl"], out ssl);
            _enableSsl = ssl;
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                var message = new MailMessage
                {
                    From = new MailAddress(_senderEmail),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                message.To.Add(new MailAddress(to));

                using var client = new SmtpClient(_smtpServer, _smtpPort)
                {
                    Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
                    EnableSsl = _enableSsl
                };

                await client.SendMailAsync(message);
                _logger.LogInformation("Correo enviado a {Recipient}", to);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar correo a {Recipient}", to);
                return false;
            }
        }
    }
} 