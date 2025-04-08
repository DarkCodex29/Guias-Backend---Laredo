using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GuiasBackend.Models;
using GuiasBackend.Services.Interfaces;
using GuiasBackend.Constants;
using System.Security.Claims;
using GuiasBackend.Models.Auth;
using GuiasBackend.Services;
using BCrypt.Net;
using GuiasBackend.Models.Common;
using NPOI.XSSF.UserModel;
using NPOI.SS.UserModel;
using GuiasBackend.Models.BulkUpload;
using System.Text.RegularExpressions;
using GuiasBackend.Helpers;

namespace GuiasBackend.Controllers
{
    // Solo los administradores pueden gestionar usuarios
    [Authorize(Policy = "RequireAdminRole")]
    [ApiController]
    [Route("api/usuarios")]
    [Produces("application/json")]
    public class UsuariosController : ControllerBase
    {
        private readonly IUsuarioService _usuarioService;
        private readonly IPasswordService _passwordService;
        private readonly ILogger<UsuariosController> _logger;

        public UsuariosController(
            IUsuarioService usuarioService,
            IPasswordService passwordService,
            ILogger<UsuariosController> logger)
        {
            _usuarioService = usuarioService ?? throw new ArgumentNullException(nameof(usuarioService));
            _passwordService = passwordService ?? throw new ArgumentNullException(nameof(passwordService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Refactorizar el método Register para reducir complejidad cognitiva
        [HttpPost("registro")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(Usuario), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { message = "La solicitud no puede ser nula" });
                }

                var validationResult = ValidateRegisterRequest(request);
                if (validationResult != null)
                {
                    return BadRequest(new { message = validationResult });
                }

                if (await _usuarioService.ExisteEmailAsync(request.Email))
                {
                    return BadRequest(new { message = $"El email {request.Email} ya está registrado" });
                }

                (bool isValid, string? passwordWarning) = _passwordService.ValidatePassword(request.Password);

                if (!isValid)
                {
                    return BadRequest(new { message = passwordWarning });
                }

                var usuario = new Usuario
                {
                    USERNAME = request.Username,
                    CONTRASEÑA = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    NOMBRES = request.Names,
                    APELLIDOS = request.Surnames,
                    EMAIL = request.Email,
                    ROL = request.Role.ToUpper().Trim(),
                    ESTADO = "1",
                    FECHA_CREACION = DateTime.Now
                };

                var createdUser = await _usuarioService.CreateUsuarioAsync(usuario);

                var response = new
                {
                    username = createdUser.USERNAME,
                    email = createdUser.EMAIL,
                    role = createdUser.ROL,
                    passwordWarning = !string.IsNullOrEmpty(passwordWarning) ? passwordWarning : null
                };

                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el registro de usuario");
                return StatusCode(500, new { message = ErrorMessages.InternalServerError });
            }
        }

        // Método auxiliar para validar el request
        [NonAction]
        private static string? ValidateRegisterRequest(RegisterRequest request)  // Marcado como static porque no usa estado de la instancia
        {
            // Validación del rol
            var role = request.Role.ToUpper().Trim();
            if (role != Roles.ADMINISTRADOR && role != Roles.USUARIO)
            {
                return $"Rol inválido. Los roles permitidos son: {Roles.ADMINISTRADOR} o {Roles.USUARIO}";
            }

            if (string.IsNullOrWhiteSpace(request.Username))
            {
                return "El username es requerido";
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return "El email es requerido";
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return "La contraseña es requerida";
            }

            return null;
        }

        [HttpGet]
        [Authorize(Roles = "ADMINISTRADOR")]
        [ProducesResponseType(typeof(PagedResponse<Usuario>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllUsersAsync(
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

                var usuarios = await _usuarioService.GetUsuariosAsync(page, pageSize, all, cancellationToken);
                return Ok(usuarios);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todos los usuarios");
                return StatusCode(500, new { message = ErrorMessages.InternalServerError });
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Usuario), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUserByIdAsync(int id)
        {
            try
            {
                var usuario = await _usuarioService.GetUsuarioByIdAsync(id);
                if (usuario == null)
                {
                    return NotFound(new { message = "Usuario no encontrado" });
                }

                if (usuario.ROL != Roles.ADMINISTRADOR)
                {
                    return Forbid();
                }

                return Ok(usuario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuario por ID: {Id}", id);
                return StatusCode(500, new { message = ErrorMessages.InternalServerError });
            }
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateUsuario(int id, [FromBody] Usuario usuario)
        {
            try
            {
                if (usuario == null || usuario.ID <= 0)
                {
                    return BadRequest("El usuario es inválido o el ID no está especificado o es inválido");
                }
                
                // Validar que el ID en la ruta coincide con el del usuario
                if (id != usuario.ID)
                {
                    return BadRequest($"El ID en la ruta ({id}) no coincide con el ID en el cuerpo ({usuario.ID})");
                }
                
                var result = await _usuarioService.UpdateUsuarioAsync(usuario);
                if (!result)
                {
                    return NotFound($"No se encontró un usuario con ID {usuario.ID}");
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar usuario con ID {Id}", usuario?.ID);
                return StatusCode(500, new { message = "Error interno del servidor al actualizar el usuario" });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "ADMINISTRADOR")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteUserAsync(int id)
        {
            try
            {
                var success = await _usuarioService.DeleteUsuarioAsync(id);
                if (!success)
                {
                    return NotFound(new { message = $"No se encontró el usuario con ID: {id}" });
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar usuario: {Id}", id);
                return StatusCode(500, new { message = ErrorMessages.InternalServerError });
            }
        }

        [HttpPost("carga-masiva")]
        [Authorize(Roles = "ADMINISTRADOR")]
        [ProducesResponseType(typeof(BulkUploadResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UploadUsersFromExcel(IFormFile file)
        {
            try
            {
                // Validar archivo
                var validationResult = ValidateExcelFile(file);
                if (validationResult != null)
                {
                    return BadRequest(new { message = validationResult });
                }

                // Procesar el archivo
                var result = new BulkUploadResult();
                using var stream = file.OpenReadStream();
                
                var fileExtension = Path.GetExtension(file.FileName).ToLower();
                IWorkbook workbook = CreateWorkbook(stream, fileExtension);
                
                // Obtener y validar estructura del Excel
                ISheet sheet = workbook.GetSheetAt(0);
                if (sheet.LastRowNum < 1)
                {
                    return BadRequest(new { message = "El archivo está vacío o no contiene datos" });
                }

                // Procesar encabezados
                var headerIndexes = GetHeaderIndexes(sheet);
                if (headerIndexes == null)
                {
                    return BadRequest(new { message = "El archivo no contiene todos los encabezados requeridos" });
                }

                // Procesar filas de datos
                result.TotalRegistros = sheet.LastRowNum;
                await ProcessExcelRows(sheet, headerIndexes, result);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en la carga masiva de usuarios");
                return StatusCode(500, new { message = ErrorMessages.InternalServerError });
            }
        }

        [NonAction]
        private static string? ValidateExcelFile(IFormFile file)  // Marcado como static porque no usa estado de la instancia
        {
            if (file == null || file.Length == 0)
                return "No se ha proporcionado un archivo";

            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            if (fileExtension != ".xlsx" && fileExtension != ".xls")
                return "El archivo debe ser de formato Excel (.xlsx o .xls)";

            return null;
        }

        [NonAction]
        private static IWorkbook CreateWorkbook(Stream stream, string fileExtension)
        {
            return fileExtension == ".xlsx" 
                ? new XSSFWorkbook(stream) 
                : (IWorkbook)new NPOI.HSSF.UserModel.HSSFWorkbook(stream);
        }

        [NonAction]
        private static Dictionary<string, int>? GetHeaderIndexes(ISheet sheet)
        {
            var headerRow = sheet.GetRow(0);
            if (headerRow == null) return null;

            var expectedHeaders = new[] { "username", "password", "email", "names", "surnames", "role" };
            var headerIndexes = new Dictionary<string, int>();
            
            for (int i = 0; i < headerRow.LastCellNum; i++)
            {
                var cell = headerRow.GetCell(i);
                if (cell != null && !string.IsNullOrWhiteSpace(cell.StringCellValue))
                {
                    var header = cell.StringCellValue.ToLower().Trim();
                    if (expectedHeaders.Contains(header))
                    {
                        headerIndexes[header] = i;
                    }
                }
            }

            // Verificar que todos los encabezados están presentes
            return expectedHeaders.All(h => headerIndexes.ContainsKey(h)) ? headerIndexes : null;
        }

        [NonAction]
        private async Task ProcessExcelRows(ISheet sheet, Dictionary<string, int> headerIndexes, BulkUploadResult result)
        {
            for (int i = 1; i <= sheet.LastRowNum; i++)
            {
                var row = sheet.GetRow(i);
                if (row == null || ExcelHelper.IsRowEmpty(row)) continue;

                await ProcessSingleRow(row, i, headerIndexes, result);
            }
        }

        [NonAction]
        private async Task ProcessSingleRow(IRow row, int rowIndex, Dictionary<string, int> headerIndexes, BulkUploadResult result)
        {
            try
            {
                // Extraer datos de la fila
                var request = ExtractUserDataFromRow(row, headerIndexes);
                
                // Validar datos
                var validationError = await ValidateUserData(request, rowIndex);
                if (validationError != null)
                {
                    result.Errores.Add(validationError);
                    result.RegistrosFallidos++;
                    return;
                }
                
                // Crear usuario
                await CreateUserFromRequest(request);
                result.RegistrosExitosos++;
            }
            catch (Exception ex)
            {
                result.Errores.Add($"Error en la fila {rowIndex + 1}: {ex.Message}");
                result.RegistrosFallidos++;
            }
        }

        [NonAction]
        private static RegisterRequest ExtractUserDataFromRow(IRow row, Dictionary<string, int> headerIndexes)
        {
            return new RegisterRequest
            {
                Username = ExcelHelper.GetCellValueAsString(row.GetCell(headerIndexes["username"])),
                Password = ExcelHelper.GetCellValueAsString(row.GetCell(headerIndexes["password"])),
                Email = ExcelHelper.GetCellValueAsString(row.GetCell(headerIndexes["email"])),
                Names = ExcelHelper.GetCellValueAsString(row.GetCell(headerIndexes["names"])),
                Surnames = ExcelHelper.GetCellValueAsString(row.GetCell(headerIndexes["surnames"])),
                Role = ExcelHelper.GetCellValueAsString(row.GetCell(headerIndexes["role"]))
            };
        }

        [NonAction]
        private async Task<string?> ValidateUserData(RegisterRequest request, int rowIndex)
        {
            // Validar formato general
            var validationResult = ValidateRegisterRequest(request);
            if (validationResult != null)
            {
                return $"Error en la fila {rowIndex + 1}: {validationResult}";
            }

            // Validar si el email ya existe
            if (await _usuarioService.ExisteEmailAsync(request.Email))
            {
                return $"Error en la fila {rowIndex + 1}: El email {request.Email} ya está registrado";
            }

            // Validar contraseña
            (bool isValid, string? passwordWarning) = _passwordService.ValidatePassword(request.Password);
            if (!isValid)
            {
                return $"Error en la fila {rowIndex + 1}: {passwordWarning}";
            }
               
            return null;
        }

        [NonAction]
        private async Task<Usuario> CreateUserFromRequest(RegisterRequest request)
        {
            var usuario = new Usuario
            {
                USERNAME = request.Username,
                CONTRASEÑA = BCrypt.Net.BCrypt.HashPassword(request.Password),
                NOMBRES = request.Names,
                APELLIDOS = request.Surnames,
                EMAIL = request.Email,
                ROL = request.Role.ToUpper().Trim(),
                ESTADO = "1",
                FECHA_CREACION = DateTime.Now
            };

            return await _usuarioService.CreateUsuarioAsync(usuario);
        }
    }
}