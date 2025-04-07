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
    [Route("api/transportistas")]
    [Produces("application/json")]
    public class TransportistasController : ControllerBase
    {
        private readonly ITransportistaService _transportistaService;
        private readonly ILogger<TransportistasController> _logger;

        public TransportistasController(
            ITransportistaService transportistaService,
            ILogger<TransportistasController> logger)
        {
            _transportistaService = transportistaService ?? throw new ArgumentNullException(nameof(transportistaService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtiene todos los transportistas con paginación opcional
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResponse<VistaTransportista>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PagedResponse<VistaTransportista>>> GetAllTransportistas(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 50,
            [FromQuery] bool all = false,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Validar parámetros de paginación solo si no se solicitan todos los registros
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

                var transportistas = await _transportistaService.GetAllTransportistasAsync(page, pageSize, all, cancellationToken);
                return Ok(transportistas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todos los transportistas");
                return StatusCode(500, new { message = ErrorMessages.InternalServerError });
            }
        }

        /// <summary>
        /// Obtiene un transportista por su código
        /// </summary>
        [HttpGet("codigo/{codTransp}")]
        [ProducesResponseType(typeof(VistaTransportista), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTransportistaByCodTransp(int codTransp)
        {
            try
            {
                var transportista = await _transportistaService.GetTransportistaByCodTranspAsync(codTransp);
                if (transportista == null)
                {
                    return NotFound(new { message = $"No se encontró el transportista con código {codTransp}" });
                }
                return Ok(transportista);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Parámetro inválido para obtener el transportista por código");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el transportista con código {CodTransp}", codTransp);
                return StatusCode(500, new { message = ErrorMessages.InternalServerError });
            }
        }

        /// <summary>
        /// Obtiene un transportista por su RUC
        /// </summary>
        [HttpGet("ruc/{ruc}")]
        [ProducesResponseType(typeof(VistaTransportista), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTransportistaByRuc(string ruc)
        {
            try
            {
                var transportista = await _transportistaService.GetTransportistaByRucAsync(ruc);
                if (transportista == null)
                {
                    return NotFound(new { message = $"No se encontró el transportista con RUC {ruc}" });
                }
                return Ok(transportista);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Parámetro inválido para obtener el transportista por RUC");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el transportista con RUC {Ruc}", ruc);
                return StatusCode(500, new { message = ErrorMessages.InternalServerError });
            }
        }
    }
}