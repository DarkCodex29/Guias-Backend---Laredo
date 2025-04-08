using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GuiasBackend.Models;
using GuiasBackend.Services.Interfaces;
using GuiasBackend.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using GuiasBackend.Constants;
using Oracle.ManagedDataAccess.Client;

namespace GuiasBackend.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuthService> _logger;
        private readonly string _jwtKey;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;
        private readonly int _jwtExpirationHours;
        private readonly IEmailService _emailService;
        // Lista para almacenar tokens revocados (en producción, usar una base de datos)
        private static readonly ConcurrentDictionary<string, DateTime> _revokedTokens = new();

        public AuthService(
            IConfiguration configuration, 
            ApplicationDbContext context, 
            ILogger<AuthService> logger,
            IEmailService emailService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));

            var configKey = configuration["Jwt:Key"] ?? 
                throw new InvalidOperationException("La clave JWT no está configurada en appsettings.json");
            
            _jwtKey = configKey;
            _jwtIssuer = configuration["Jwt:Issuer"] ?? "GuiasBackend";
            _jwtAudience = configuration["Jwt:Audience"] ?? "GuiasBackendAPI"; 
            
            if (!int.TryParse(configuration["Jwt:ExpirationHours"], out _jwtExpirationHours) || _jwtExpirationHours <= 0)
            {
                _logger.LogWarning("La expiración JWT no está configurada correctamente. Se usará un valor predeterminado de 24 horas.");
                _jwtExpirationHours = 24;
            }
        }

        public async Task<(bool success, string token, string? role, int userId)> AuthenticateAsync(string username, string password)
        {
            try
            {
                _logger.LogInformation("Intentando autenticar usuario: {Username}", username);
                
                // Usar consulta SQL cruda con FromSqlRaw y ToListAsync, que ha demostrado funcionar con Oracle
                var users = await _context.Usuarios
                    .FromSqlRaw("SELECT * FROM USUARIO WHERE USERNAME = :p0", username)
                    .AsNoTracking()
                    .ToListAsync();
                    
                var user = users.FirstOrDefault();

                if (user == null)
                {
                    _logger.LogWarning("Usuario no encontrado: {Username}", username);
                    return (false, string.Empty, null, 0);
                }

                var passwordValid = VerifyPassword(password, user.CONTRASEÑA);
                
                if (!passwordValid)
                {
                    _logger.LogWarning("Contraseña inválida para el usuario: {Username}", username);
                    return (false, string.Empty, null, 0);
                }

                var token = GenerateJwtToken(user);
                _logger.LogInformation("Autenticación exitosa para: {Username}", username);
                return (true, token, user.ROL, user.ID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante la autenticación del usuario {Username}", username);
                throw new InvalidOperationException($"Error durante la autenticación del usuario {username}", ex);
            }
        }

        private string GenerateJwtToken(Usuario user)
        {
            try
            {
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                // Set up claims for the token
                var claims = new List<Claim>
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.USERNAME),
                    new Claim(JwtRegisteredClaimNames.Email, user.EMAIL ?? string.Empty),
                    new Claim(ClaimTypes.NameIdentifier, user.ID.ToString()),
                    new Claim(ClaimTypes.Role, user.ROL), // Standard role claim
                    new Claim("id", user.ID.ToString()),
                    new Claim("rol", user.ROL),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                // Create the token with the specified parameters
                var token = new JwtSecurityToken(
                    issuer: _jwtIssuer,
                    audience: _jwtAudience,
                    claims: claims,
                    expires: DateTime.Now.AddHours(_jwtExpirationHours),
                    signingCredentials: credentials
                );

                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
                
                // Log para depuración
                _logger.LogInformation("Token generado para usuario ID: {ID}, Username: {Username}, Expira: {Expiration}, Claims count: {ClaimsCount}", 
                    user.ID, user.USERNAME, token.ValidTo, claims.Count);
                
                return tokenString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando token JWT para {Username}", user.USERNAME);
                throw new InvalidOperationException($"Error al generar token JWT para el usuario {user.USERNAME}", ex);
            }
        }

        private static bool VerifyPassword(string inputPassword, string storedPassword)
        {
            try 
            {
                var result = BCrypt.Net.BCrypt.Verify(inputPassword, storedPassword);
                return result;
            }
            catch (Exception)
            {
                // Si hay algún error en la verificación (formato hash inválido), retornamos false
                return false;
            }
        }

        private static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                // Verificar si el token ha sido revocado
                if (_revokedTokens.ContainsKey(token))
                {
                    _logger.LogWarning("Intento de usar un token revocado");
                    return false;
                }

                return await Task.Run(() =>
                {
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var key = Encoding.UTF8.GetBytes(_jwtKey);
                    
                    try
                    {
                        // Validar el token
                        tokenHandler.ValidateToken(token, new TokenValidationParameters
                        {
                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = new SymmetricSecurityKey(key),
                            ValidateIssuer = true,
                            ValidIssuer = _jwtIssuer,
                            ValidateAudience = true,
                            ValidAudience = _jwtAudience,
                            ClockSkew = TimeSpan.Zero
                        }, out _);

                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar el token");
                throw new InvalidOperationException("Error al validar el token", ex);
            }
        }

        public async Task<bool> RevokeTokenAsync(string token)
        {
            try
            {
                // Validar que el token sea válido antes de revocarlo
                if (await ValidateTokenAsync(token))
                {
                    // Agregar el token a la lista de tokens revocados
                    // En un entorno de producción, esto debería persistirse en una base de datos
                    if (!_revokedTokens.TryAdd(token, DateTime.Now))
                    {
                        _logger.LogWarning("El token ya estaba revocado");
                    }
                    else
                    {
                        _logger.LogInformation("Token revocado exitosamente");
                    }
                    
                    return true;
                }
                
                _logger.LogWarning("Intento de revocar un token inválido");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al revocar el token");
                return false;
            }
        }

        // Método para limpiar tokens revocados expirados (se debería ejecutar periódicamente)
        public void CleanupRevokedTokens()
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokensToRemove = _revokedTokens
                .Where(entry => {
                    try 
                    {
                        var token = tokenHandler.ReadJwtToken(entry.Key);
                        return token.ValidTo < DateTime.Now;
                    }
                    catch
                    {
                        return true; // Si no podemos leer el token, lo eliminamos
                    }
                })
                .Select(entry => entry.Key)
                .ToList();

            foreach (var token in tokensToRemove)
            {
                _revokedTokens.TryRemove(token, out _);
            }

            _logger.LogInformation("Se eliminaron {Count} tokens revocados expirados", tokensToRemove.Count);
        }

        public async Task<bool> RequestPasswordResetAsync(string email)
        {
            try
            {
                // Buscar usuario por email
                var users = await _context.Usuarios
                    .FromSqlRaw("SELECT * FROM USUARIO WHERE EMAIL = :p0 AND ESTADO = '1'", email)
                    .AsNoTracking()
                    .ToListAsync();

                var user = users.FirstOrDefault();
                if (user == null)
                {
                    _logger.LogWarning("No se encontró un usuario con el email: {Email}", email);
                    return false;
                }

                // Generar código de 6 dígitos
                var code = GenerateResetCode(6);

                // Invalidar códigos anteriores para este email
                var previousCodes = await _context.PasswordResets
                    .Where(r => r.EMAIL == email && !r.USADO)
                    .ToListAsync();

                foreach (var prevCode in previousCodes)
                {
                    prevCode.USADO = true;
                }

                // Crear nuevo código con expiración de 30 minutos
                var passwordReset = new PasswordReset
                {
                    EMAIL = email,
                    CODIGO = code,
                    FECHA_CREACION = DateTime.Now,
                    FECHA_EXPIRACION = DateTime.Now.AddMinutes(30),
                    USADO = false
                };

                _context.PasswordResets.Add(passwordReset);
                await _context.SaveChangesAsync();

                // Enviar email con el código
                var subject = "Código de restablecimiento de contraseña";
                var body = $@"
                    <html>
                    <head>
                        <style>
                            body {{ font-family: Arial, sans-serif; line-height: 1.6; }}
                            .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                            .code {{ font-size: 28px; font-weight: bold; text-align: center; 
                                     padding: 10px; background-color: #f5f5f5; border-radius: 5px; }}
                            .footer {{ font-size: 12px; color: #777; margin-top: 20px; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <h2>Restablecimiento de contraseña</h2>
                            <p>Hemos recibido una solicitud para restablecer su contraseña.</p>
                            <p>Su código de verificación es:</p>
                            <div class='code'>{code}</div>
                            <p>Este código expirará en 30 minutos.</p>
                            <p>Si usted no solicitó restablecer su contraseña, puede ignorar este mensaje.</p>
                            <div class='footer'>
                                <p>Este es un correo automático, por favor no responda a este mensaje.</p>
                            </div>
                        </div>
                    </body>
                    </html>
                ";

                var emailSent = await _emailService.SendEmailAsync(email, subject, body);
                if (!emailSent)
                {
                    _logger.LogError("Error al enviar email de restablecimiento a {Email}", email);
                    return false;
                }

                _logger.LogInformation("Código de restablecimiento enviado a {Email}", email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al solicitar restablecimiento de contraseña para {Email}", email);
                return false;
            }
        }

        public async Task<bool> VerifyResetCodeAsync(string email, string code)
        {
            try
            {
                // Buscar código válido
                var passwordReset = await _context.PasswordResets
                    .Where(r => r.EMAIL == email && r.CODIGO == code && !r.USADO && r.FECHA_EXPIRACION > DateTime.Now)
                    .OrderByDescending(r => r.FECHA_CREACION)
                    .FirstOrDefaultAsync();

                if (passwordReset == null)
                {
                    _logger.LogWarning("Código de restablecimiento inválido o expirado para {Email}", email);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar código de restablecimiento para {Email}", email);
                return false;
            }
        }

        public async Task<bool> ResetPasswordAsync(string email, string code, string newPassword)
        {
            try
            {
                // Verificar código
                var isValidCode = await VerifyResetCodeAsync(email, code);
                if (!isValidCode)
                {
                    return false;
                }

                // Buscar usuario
                var users = await _context.Usuarios
                    .FromSqlRaw("SELECT * FROM USUARIO WHERE EMAIL = :p0 AND ESTADO = '1'", email)
                    .ToListAsync();

                var user = users.FirstOrDefault();
                if (user == null)
                {
                    _logger.LogWarning("No se encontró un usuario con el email: {Email}", email);
                    return false;
                }

                // Actualizar contraseña
                user.CONTRASEÑA = HashPassword(newPassword);
                user.FECHA_ACTUALIZACION = DateTime.Now;

                // Marcar código como usado
                var passwordReset = await _context.PasswordResets
                    .Where(r => r.EMAIL == email && r.CODIGO == code && !r.USADO)
                    .OrderByDescending(r => r.FECHA_CREACION)
                    .FirstOrDefaultAsync();

                if (passwordReset != null)
                {
                    passwordReset.USADO = true;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Contraseña restablecida para {Email}", email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al restablecer contraseña para {Email}", email);
                return false;
            }
        }

        private static string GenerateResetCode(int length)
        {
            var random = new Random();
            return new string(Enumerable.Repeat("0123456789", length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}