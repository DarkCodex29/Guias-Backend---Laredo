using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GuiasBackend.Services.Interfaces;
using GuiasBackend.Models;
using Microsoft.Extensions.Logging;
using GuiasBackend.Constants;
using GuiasBackend.Models.Common;

namespace GuiasBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/jirones")]
    [Produces("application/json")]
    public class JironesController : ControllerBase
    {
        private readonly IJironService _jironService;
        private readonly ILogger<JironesController> _logger;

        public JironesController(
            IJironService jironService,
            ILogger<JironesController> logger)
        {
            _jironService = jironService ?? throw new ArgumentNullException(nameof(jironService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResponse<VistaJiron>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PagedResponse<VistaJiron>>> GetAllJirones(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 50,
            [FromQuery] bool all = false,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Validamos los parámetros de paginación solo si no se solicitan todos los registros
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

                var jirones = await _jironService.GetAllJironesAsync(page, pageSize, all, cancellationToken);
                return Ok(jirones);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todos los jirones");
                return StatusCode(500, ErrorMessages.InternalServerError);
            }
        }

        [HttpGet("{jiron}")]
        [ProducesResponseType(typeof(VistaJiron), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetJironByJiron(string jiron)
        {
            try
            {
                var result = await _jironService.GetJironByJironAsync(jiron);
                if (result == null)
                {
                    return NotFound(new { message = $"No se encontró el jirón: {jiron}" });
                }
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Parámetro inválido para obtener el jirón");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el jirón {Jiron}", jiron);
                return StatusCode(500, ErrorMessages.InternalServerError);
            }
        }

        [HttpGet("campo/{campo}")]
        [ProducesResponseType(typeof(PagedResponse<VistaJiron>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PagedResponse<VistaJiron>>> GetJironesByCampo(
            string campo,
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 50,
            [FromQuery] bool all = false,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Validamos los parámetros de paginación solo si no se solicitan todos los registros
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

                var jirones = await _jironService.GetJironesByCampoAsync(campo, page, pageSize, all, cancellationToken);
                return Ok(jirones);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Parámetro inválido para obtener los jirones por campo");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener los jirones del campo {Campo}", campo);
                return StatusCode(500, ErrorMessages.InternalServerError);
            }
        }
    }
}