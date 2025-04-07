using Microsoft.AspNetCore.Mvc;
using GuiasBackend.Services.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace GuiasBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PasswordResetController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<PasswordResetController> _logger;

        public PasswordResetController(IAuthService authService, ILogger<PasswordResetController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Solicita un código de restablecimiento de contraseña
        /// </summary>
        /// <param name="model">Modelo con el email</param>
        /// <returns>Respuesta con el resultado de la operación</returns>
        [HttpPost("request")]
        public async Task<IActionResult> RequestReset([FromBody] RequestResetModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.RequestPasswordResetAsync(model.Email);
                if (!result)
                {
                    return NotFound(new { message = "No se encontró un usuario con ese correo electrónico" });
                }

                return Ok(new { message = "Se ha enviado un código de verificación a su correo electrónico" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al solicitar restablecimiento de contraseña para {Email}", model.Email);
                return StatusCode(500, new { message = "Error al procesar la solicitud" });
            }
        }

        /// <summary>
        /// Verifica un código de restablecimiento
        /// </summary>
        /// <param name="model">Modelo con el email y código</param>
        /// <returns>Respuesta con el resultado de la verificación</returns>
        [HttpPost("verify")]
        public async Task<IActionResult> VerifyCode([FromBody] VerifyCodeModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.VerifyResetCodeAsync(model.Email, model.Code);
                if (!result)
                {
                    return BadRequest(new { message = "Código inválido o expirado" });
                }

                return Ok(new { message = "Código verificado correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar código para {Email}", model.Email);
                return StatusCode(500, new { message = "Error al procesar la solicitud" });
            }
        }

        /// <summary>
        /// Restablece la contraseña usando un código de verificación
        /// </summary>
        /// <param name="model">Modelo con el email, código y nueva contraseña</param>
        /// <returns>Respuesta con el resultado de la operación</returns>
        [HttpPost("reset")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.ResetPasswordAsync(model.Email, model.Code, model.NewPassword);
                if (!result)
                {
                    return BadRequest(new { message = "No se pudo restablecer la contraseña. Código inválido o expirado" });
                }

                return Ok(new { message = "Contraseña restablecida correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al restablecer contraseña para {Email}", model.Email);
                return StatusCode(500, new { message = "Error al procesar la solicitud" });
            }
        }
    }

    public class RequestResetModel
    {
        [Required(ErrorMessage = "El correo electrónico es requerido")]
        [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido")]
        public string Email { get; set; } = string.Empty;
    }

    public class VerifyCodeModel
    {
        [Required(ErrorMessage = "El correo electrónico es requerido")]
        [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "El código es requerido")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "El código debe tener 6 dígitos")]
        public string Code { get; set; } = string.Empty;
    }

    public class ResetPasswordModel
    {
        [Required(ErrorMessage = "El correo electrónico es requerido")]
        [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "El código es requerido")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "El código debe tener 6 dígitos")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "La nueva contraseña es requerida")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        public string NewPassword { get; set; } = string.Empty;
    }
} 