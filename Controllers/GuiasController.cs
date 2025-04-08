using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GuiasBackend.Services.Interfaces;
using GuiasBackend.Models;
using GuiasBackend.Constants;
using GuiasBackend.Models.Common;
using GuiasBackend.DTOs;
using System.Security.Claims;

namespace GuiasBackend.Controllers
{
    [Authorize] // Todos los usuarios autenticados pueden acceder a las guías
    [ApiController]
    [Route("api/guias")]
    [Produces("application/json")]
    public class GuiasController : ControllerBase
    {
        private readonly IGuiasService _guiasService;
        private readonly IUsuarioService _usuarioService;
        private readonly ILogger<GuiasController> _logger;

        public GuiasController(
            IGuiasService guiasService,
            IUsuarioService usuarioService,
            ILogger<GuiasController> logger)
        {
            _guiasService = guiasService ?? throw new ArgumentNullException(nameof(guiasService));
            _usuarioService = usuarioService ?? throw new ArgumentNullException(nameof(usuarioService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtiene todas las guías con paginación opcional
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResponse<Guia>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PagedResponse<Guia>>> GetGuias(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 20,
            [FromQuery] bool all = false,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Validamos parámetros solo si no se pide todos los registros
                if (!all)
                {
                    if (page < 1)
                    {
                        return BadRequest("El número de página debe ser mayor o igual a 1");
                    }

                    if (pageSize < 1 || pageSize > 50)
                    {
                        return BadRequest("El tamaño de página debe estar entre 1 y 50");
                    }
                }

                var guias = await _guiasService.GetGuiasAsync(page, pageSize, all, cancellationToken);
                return Ok(guias);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener las guías");
                return StatusCode(500, ErrorMessages.InternalServerError);
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Guia), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Guia>> GetGuia(int id)
        {
            try
            {
                var guia = await _guiasService.GetGuiaByIdAsync(id);
                if (guia == null)
                {
                    return NotFound(new { message = $"No se encontró la guía con ID {id}" });
                }
                return Ok(guia);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener la guía con ID {Id}", id);
                return StatusCode(500, new { message = ErrorMessages.InternalServerError });
            }
        }

        // Buscar guías por ID de usuario
        [HttpGet("usuario/{idUsuario}")]
        [ProducesResponseType(typeof(IEnumerable<Guia>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetGuiasByUsuarioId(
            int idUsuario,
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 20, 
            [FromQuery] bool all = false, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Verificar si existe el usuario
                var usuario = await _usuarioService.GetUsuarioByIdAsync(idUsuario);
                if (usuario == null)
                {
                    _logger.LogWarning("Se intentó acceder a guías de un usuario inexistente: {IdUsuario}", idUsuario);
                    return NotFound(new { message = $"No existe un usuario con ID {idUsuario}" });
                }

                var guias = await _guiasService.GetGuiasByUsuarioIdAsync(idUsuario, page, pageSize, all, cancellationToken);
                
                _logger.LogInformation("Se obtuvieron {Count} guías para el usuario ID: {IdUsuario}", guias.Count(), idUsuario);
                    
                return Ok(guias);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener las guías del usuario {IdUsuario}", idUsuario);
                return StatusCode(500, new { message = ErrorMessages.InternalServerError });
            }
        }

        /// <summary>
        /// Crea una nueva guía subiendo un archivo
        /// </summary>
        /// <returns>La guía creada con su ID</returns>
        [HttpPost]
        [ProducesResponseType(typeof(Guia), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Guia>> CreateGuia([FromForm] GuiaCreateDto guiaDTO)
        {
            try
            {
                if (guiaDTO.Archivo == null || guiaDTO.Archivo.Length == 0)
                {
                    return BadRequest("No se ha proporcionado ningún archivo");
                }

                // Validar tamaño máximo (10MB)
                if (guiaDTO.Archivo.Length > 10 * 1024 * 1024)
                {
                    return BadRequest("El archivo no puede ser mayor a 10MB");
                }

                // Validar tipo de archivo
                var allowedTypes = new[] { "application/pdf", "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" };
                if (!allowedTypes.Contains(guiaDTO.Archivo.ContentType.ToLower()))
                {
                    return BadRequest("Solo se permiten archivos PDF y Word");
                }

                if (string.IsNullOrWhiteSpace(guiaDTO.Nombre))
                {
                    return BadRequest("El nombre no puede estar vacío");
                }

                // Verificar si el usuario existe
                var usuario = await _usuarioService.GetUsuarioByIdAsync(guiaDTO.IdUsuario);
                if (usuario == null)
                {
                    return BadRequest($"No existe un usuario con ID {guiaDTO.IdUsuario}");
                }

                using var ms = new MemoryStream();
                await guiaDTO.Archivo.CopyToAsync(ms);
                var fileBytes = ms.ToArray();

                var guia = new Guia
                {
                    NOMBRE = guiaDTO.Nombre,
                    ARCHIVO = fileBytes,
                    FECHA_SUBIDA = DateTime.Now,
                    ID_USUARIO = guiaDTO.IdUsuario
                };

                var result = await _guiasService.CreateGuiaAsync(guia);
                return CreatedAtAction(nameof(GetGuia), new { id = result.ID }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear la guía");
                return StatusCode(500, new { message = ErrorMessages.InternalServerError });
            }
        }

        [HttpGet("correlativo")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCorrelativoGuia()
        {
            try
            {
                var correlativo = await _guiasService.GenerarCorrelativoGuiaAsync();
                return Ok(new { correlativo });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar el correlativo de guía");
                return StatusCode(500, new { message = ErrorMessages.InternalServerError });
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteGuia(int id)
        {
            try
            {
                var resultado = await _guiasService.DeleteGuiaAsync(id);
                if (!resultado)
                {
                    return NotFound(new { message = $"No se encontró la guía con ID {id}" });
                }

                return Ok(new { message = "Guía eliminada correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar la guía con ID {Id}", id);
                return StatusCode(500, new { message = ErrorMessages.InternalServerError });
            }
        }
    }
}