using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GuiasBackend.Services.Interfaces;
using GuiasBackend.Models;
using Microsoft.Extensions.Logging;
using GuiasBackend.Constants;
using GuiasBackend.Models.Common;

namespace GuiasBackend.Controllers
{
    /// <summary>
    /// Controlador para gestionar los campos
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/campos")]
    [Produces("application/json")]
    public class CamposController : ControllerBase
    {
        private readonly ICampoService _campoService;
        private readonly ILogger<CamposController> _logger;

        public CamposController(
            ICampoService campoService,
            ILogger<CamposController> logger)
        {
            _campoService = campoService ?? throw new ArgumentNullException(nameof(campoService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtiene todos los campos ordenados por código con paginación opcional
        /// </summary>
        /// <param name="page">Número de página (por defecto 1)</param>
        /// <param name="pageSize">Tamaño de página (por defecto 50)</param>
        /// <param name="all">Si es true, devuelve todos los registros sin paginación</param>
        /// <returns>Lista paginada o completa de campos</returns>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResponse<VistaCampo>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PagedResponse<VistaCampo>>> GetAllCampos(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 50,
            [FromQuery] bool all = false,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!all)
                {
                    if (page < 1)
                    {
                        return BadRequest("El número de página debe ser mayor o igual a 1");
                    }

                    if (pageSize < 1 || pageSize > 100)
                    {
                        return BadRequest("El tamaño de página debe estar entre 1 y 100");
                    }
                }

                var campos = await _campoService.GetAllCamposAsync(page, pageSize, all, cancellationToken);
                return Ok(campos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todos los campos");
                return StatusCode(500, ErrorMessages.InternalServerError);
            }
        }

        /// <summary>
        /// Obtiene un campo por su código
        /// </summary>
        /// <param name="campo">Código del campo (máximo 6 caracteres)</param>
        /// <returns>Campo encontrado</returns>
        [HttpGet("{campo}")]
        [ProducesResponseType(typeof(VistaCampo), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCampoByCampo(string campo)
        {
            try
            {
                var result = await _campoService.GetCampoByCampoAsync(campo);
                if (result == null)
                {
                    return NotFound($"No se encontró el campo: {campo}");
                }
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Parámetro inválido para obtener el campo");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el campo {Campo}", campo);
                return StatusCode(500, ErrorMessages.InternalServerError);
            }
        }
    }
}