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
    [Route("api/empleados")]
    [Produces("application/json")]
    public class EmpleadosController : ControllerBase
    {
        private readonly IEmpleadoService _empleadoService;
        private readonly ILogger<EmpleadosController> _logger;

        public EmpleadosController(
            IEmpleadoService empleadoService,
            ILogger<EmpleadosController> logger)
        {
            _empleadoService = empleadoService ?? throw new ArgumentNullException(nameof(empleadoService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResponse<VistaEmpleado>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PagedResponse<VistaEmpleado>>> GetAllEmpleados(
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

                var empleados = await _empleadoService.GetAllEmpleadosAsync(page, pageSize, all, cancellationToken);
                return Ok(empleados);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todos los empleados");
                return StatusCode(500, ErrorMessages.InternalServerError);
            }
        }

        [HttpGet("dni/{dni}")]
        [ProducesResponseType(typeof(VistaEmpleado), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetEmpleadoByDni(string dni)
        {
            try
            {
                var empleado = await _empleadoService.GetEmpleadoByDniAsync(dni);
                if (empleado == null)
                {
                    return NotFound(new { message = $"No se encontró el empleado con DNI {dni}" });
                }
                return Ok(empleado);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Parámetro inválido para obtener el empleado por DNI");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el empleado con DNI {Dni}", dni);
                return StatusCode(500, ErrorMessages.InternalServerError);
            }
        }

        [HttpGet("empleado/{empleado}")]
        [ProducesResponseType(typeof(VistaEmpleado), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetEmpleadoByEmpleado(string empleado)
        {
            try
            {
                var result = await _empleadoService.GetEmpleadoByEmpleadoAsync(empleado);
                if (result == null)
                {
                    return NotFound(new { message = $"No se encontró el empleado: {empleado}" });
                }
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Parámetro inválido para obtener el empleado por nombre");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el empleado {Empleado}", empleado);
                return StatusCode(500, ErrorMessages.InternalServerError);
            }
        }

        /// <summary>
        /// Obtiene un empleado por su código si existe.
        /// </summary>
        /// <param name="codigo">Código del empleado</param>
        /// <returns>Información del empleado si existe, NotFound en caso contrario</returns>
        [HttpGet("existe/{codigo}")]
        [ProducesResponseType(typeof(VistaEmpleado), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ExisteEmpleadoPorCodigo(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
            {
                return BadRequest("El código no puede estar vacío.");
            }

            try
            {
                var empleado = await _empleadoService.GetEmpleadoPorCodigoAsync(codigo);
                if (empleado == null)
                {
                    return NotFound(new { message = $"No se encontró el empleado con código {codigo}" });
                }
                return Ok(empleado);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el empleado con código {Codigo}", codigo);
                return StatusCode(500, ErrorMessages.InternalServerError);
            }
        }
    }
}