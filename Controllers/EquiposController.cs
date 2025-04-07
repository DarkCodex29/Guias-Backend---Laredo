using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GuiasBackend.Services.Interfaces;
using GuiasBackend.Models;
using GuiasBackend.Constants;
using GuiasBackend.Models.Common;

namespace GuiasBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/equipos")]
    [Produces("application/json")]
    public class EquiposController : ControllerBase
    {
        private readonly IEquipoService _equipoService;
        private readonly ILogger<EquiposController> _logger;

        public EquiposController(IEquipoService equipoService, ILogger<EquiposController> logger)
        {
            _equipoService = equipoService ?? throw new ArgumentNullException(nameof(equipoService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResponse<VistaEquipo>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PagedResponse<VistaEquipo>>> GetAllEquipos(
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

                var equipos = await _equipoService.GetAllEquiposAsync(page, pageSize, all, cancellationToken);
                return Ok(equipos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todos los equipos");
                return StatusCode(500, new { message = ErrorMessages.InternalServerError });
            }
        }

        [HttpGet("{codEquipo}")]
        [ProducesResponseType(typeof(VistaEquipo), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetEquipoByCodEquipo(int codEquipo)
        {
            try
            {
                var equipo = await _equipoService.GetEquipoByCodEquipoAsync(codEquipo);
                if (equipo == null)
                {
                    return NotFound(new { message = $"No se encontró el equipo con código {codEquipo}" });
                }
                return Ok(equipo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el equipo con código {CodEquipo}", codEquipo);
                return StatusCode(500, new { message = ErrorMessages.InternalServerError });
            }
        }

        /// <summary>
        /// Obtiene todos los equipos de un transportista específico por su código
        /// </summary>
        [HttpGet("transportista/{codTransp}")]
        [ProducesResponseType(typeof(IEnumerable<VistaEquipo>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetEquiposByCodTransp(int codTransp)
        {
            try
            {
                if (codTransp <= 0)
                {
                    return BadRequest(new { message = "El código del transportista debe ser mayor a 0" });
                }

                var equipos = await _equipoService.GetEquiposByCodTranspAsync(codTransp);
                
                if (!equipos.Any())
                {
                    return NotFound(new { message = $"No se encontraron equipos para el transportista con código {codTransp}" });
                }
                
                return Ok(equipos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener equipos del transportista con código {CodTransp}", codTransp);
                return StatusCode(500, ErrorMessages.InternalServerError);
            }
        }

        /// <summary>
        /// Obtiene información de un equipo por su número de placa
        /// </summary>
        [HttpGet("placa/{placa}")]
        [ProducesResponseType(typeof(VistaEquipo), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetEquipoByPlaca(string placa, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(placa))
                {
                    return BadRequest(new { message = "La placa no puede estar vacía" });
                }

                var equipo = await _equipoService.GetEquipoByPlacaAsync(placa, cancellationToken);
                
                if (equipo == null)
                {
                    return NotFound(new { message = $"No se encontró ningún equipo con la placa {placa}" });
                }

                return Ok(equipo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener equipo por placa: {Placa}", placa);
                return StatusCode(500, new { message = ErrorMessages.InternalServerError });
            }
        }
    }
}