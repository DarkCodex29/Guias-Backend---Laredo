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
    [Route("api/cuarteles")]
    [Produces("application/json")]
    public class CuartelesController : ControllerBase
    {
        private readonly ICuartelService _cuartelService;
        private readonly ILogger<CuartelesController> _logger;

        public CuartelesController(
            ICuartelService cuartelService,
            ILogger<CuartelesController> logger)
        {
            _cuartelService = cuartelService ?? throw new ArgumentNullException(nameof(cuartelService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResponse<VistaCuartel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PagedResponse<VistaCuartel>>> GetAllCuarteles(
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

                var cuarteles = await _cuartelService.GetAllCuartelesAsync(page, pageSize, all, cancellationToken);
                return Ok(cuarteles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todos los cuarteles");
                return StatusCode(500, ErrorMessages.InternalServerError);
            }
        }

        [HttpGet("{cuartel}")]
        [ProducesResponseType(typeof(VistaCuartel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCuartelByCuartel(string cuartel)
        {
            try
            {
                var result = await _cuartelService.GetCuartelByCuartelAsync(cuartel);
                if (result == null)
                {
                    return NotFound(new { message = $"No se encontró el cuartel: {cuartel}" });
                }
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Parámetro inválido para obtener el cuartel");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el cuartel {Cuartel}", cuartel);
                return StatusCode(500, ErrorMessages.InternalServerError);
            }
        }

        [HttpGet("campo/{campo}")]
        [ProducesResponseType(typeof(PagedResponse<VistaCuartel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PagedResponse<VistaCuartel>>> GetCuartelesByCampo(
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

                var cuarteles = await _cuartelService.GetCuartelesByCampoAsync(campo, page, pageSize, all, cancellationToken);
                return Ok(cuarteles);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Parámetro inválido para obtener los cuarteles por campo");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener los cuarteles del campo {Campo}", campo);
                return StatusCode(500, ErrorMessages.InternalServerError);
            }
        }
    }
}