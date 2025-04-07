using Microsoft.AspNetCore.Mvc;
using GuiasBackend.Models.Auth;
using GuiasBackend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace GuiasBackend.Controllers
{
    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Autentica un usuario y devuelve un token JWT
        /// </summary>
        /// <param name="request">Credenciales del usuario</param>
        /// <returns>Token JWT si la autenticación es exitosa</returns>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var (success, token, role, userId) = await _authService.AuthenticateAsync(request.Username, request.Password);
                
                if (!success || string.IsNullOrEmpty(token))
                {
                    return Unauthorized(new { message = "Credenciales inválidas" });
                }
                
                return Ok(new LoginResponse 
                { 
                    Token = token,
                    Role = role ?? string.Empty,
                    UserId = userId.ToString(),
                    Username = request.Username
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el login para el usuario {Username}", request.Username);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Valida si un token JWT es válido
        /// </summary>
        /// <returns>Estado de validez del token</returns>
        [HttpGet("validate")]
        [Authorize]
        [ProducesResponseType(typeof(ValidateTokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult ValidateToken()
        {
            try
            {
                // Si llegamos aquí, el token es válido (el middleware de autenticación ya lo validó)
                var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var response = new ValidateTokenResponse 
                { 
                    IsValid = true,
                    Username = username
                };
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar token");
                return Unauthorized(new { message = "Token inválido" });
            }
        }

        /// <summary>
        /// Cierra la sesión del usuario (revoca el token)
        /// </summary>
        /// <returns>Confirmación de cierre de sesión</returns>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Logout([FromHeader(Name = "Authorization")] string authorization)
        {
            try
            {
                var token = ExtractTokenFromAuthHeader(authorization);
                if (string.IsNullOrEmpty(token))
                {
                    return BadRequest(new { message = "Token no proporcionado" });
                }

                await _authService.RevokeTokenAsync(token);
                return Ok(new { message = "Sesión cerrada exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el logout");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Extrae el token JWT de la cabecera de autorización
        /// </summary>
        [NonAction]
        private string? ExtractTokenFromAuthHeader(string? authorizationHeader)
        {
            if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            
            return authorizationHeader.Substring("Bearer ".Length).Trim();
        }
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
    }

    public class ValidateTokenResponse
    {
        public bool IsValid { get; set; }
        public string? Username { get; set; }
    }
}
