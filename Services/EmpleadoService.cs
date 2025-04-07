using Microsoft.EntityFrameworkCore;
using GuiasBackend.Data;
using GuiasBackend.Models;
using GuiasBackend.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using GuiasBackend.Constants;
using GuiasBackend.Extensions;
using GuiasBackend.Models.Common;

namespace GuiasBackend.Services
{
    public class EmpleadoService : IEmpleadoService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EmpleadoService> _logger;

        public EmpleadoService(
            ApplicationDbContext context,
            ILogger<EmpleadoService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PagedResponse<VistaEmpleado>> GetAllEmpleadosAsync(int page = 1, int pageSize = 50, bool all = false, CancellationToken cancellationToken = default)
        {
            try
            {
                if (all)
                {
                    // Configurar parámetros explícitamente
                    var endRowParam = new OracleParameter
                    {
                        ParameterName = "endRow",
                        OracleDbType = OracleDbType.Int32,
                        Value = 100000
                    };
                    
                    var startRowParam = new OracleParameter
                    {
                        ParameterName = "startRow",
                        OracleDbType = OracleDbType.Int32,
                        Value = 0
                    };
                    
                    var allItems = await _context.VistaEmpleados
                        .FromSqlRaw(
                            SqlQueries.GetAllEmpleados,
                            endRowParam,
                            startRowParam
                        )
                        .AsNoTracking()
                        .ToListAsync(cancellationToken);
                        
                    return new PagedResponse<VistaEmpleado>(allItems, 1, allItems.Count, allItems.Count);
                }

                // Código para paginación estándar
                var endRowPagedParam = new OracleParameter
                {
                    ParameterName = "endRow",
                    OracleDbType = OracleDbType.Int32,
                    Value = pageSize * page
                };
                
                var startRowPagedParam = new OracleParameter
                {
                    ParameterName = "startRow",
                    OracleDbType = OracleDbType.Int32,
                    Value = pageSize * (page - 1)
                };
                
                var empleados = await _context.VistaEmpleados
                    .FromSqlRaw(
                        SqlQueries.GetAllEmpleados,
                        endRowPagedParam,
                        startRowPagedParam
                    )
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);
                
                // Consulta separada para contar registros
                const string countQuery = "SELECT COUNT(*) FROM PIMS_GRE.VISTA_EMPLEADO";
                var totalCount = await _context.Database.ExecuteSqlRawAsync(countQuery, cancellationToken);
                
                return new PagedResponse<VistaEmpleado>(empleados, page, pageSize, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todos los empleados (página {Page}, tamaño {PageSize}, todos: {All})", page, pageSize, all);
                throw new InvalidOperationException("Error al obtener la lista de empleados", ex);
            }
        }

        public async Task<VistaEmpleado?> GetEmpleadoByDniAsync(string dni)
        {
            if (string.IsNullOrWhiteSpace(dni))
            {
                throw new ArgumentException("El DNI no puede estar vacío", nameof(dni));
            }

            try
            {
                var empleados = await _context.VistaEmpleados
                    .FromSqlRaw(
                        SqlQueries.GetEmpleadoByDni,
                        new OracleParameter("dni_param", dni.ToUpper())
                    )
                    .AsNoTracking()
                    .ToListAsync();
                
                return empleados.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el empleado con DNI {Dni}", dni);
                throw new InvalidOperationException($"Error al obtener el empleado con DNI {dni}", ex);
            }
        }

        public async Task<VistaEmpleado?> GetEmpleadoByEmpleadoAsync(string empleado)
        {
            if (string.IsNullOrWhiteSpace(empleado))
            {
                throw new ArgumentException("El nombre del empleado no puede estar vacío", nameof(empleado));
            }

            try
            {
                var empleados = await _context.VistaEmpleados
                    .FromSqlRaw(
                        SqlQueries.GetEmpleadoByEmpleado,
                        new OracleParameter("empleado_param", empleado.ToUpper())
                    )
                    .AsNoTracking()
                    .ToListAsync();
                
                return empleados.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el empleado con nombre {Empleado}", empleado);
                throw new InvalidOperationException($"Error al obtener el empleado con nombre {empleado}", ex);
            }
        }

        public async Task<bool> ExisteEmpleadoPorCodigoAsync(string codigo)
        {
            if (!int.TryParse(codigo, out var codigoInt))
            {
                throw new ArgumentException("El código debe ser un número válido", nameof(codigo));
            }
            return await _context.VistaEmpleados.AnyAsync(e => e.Codigo == codigoInt);
        }

        public async Task<VistaEmpleado?> GetEmpleadoPorCodigoAsync(string codigo)
        {
            if (!int.TryParse(codigo, out var codigoInt))
            {
                throw new ArgumentException("El código debe ser un número válido", nameof(codigo));
            }

            try
            {
                var codigoParam = new OracleParameter("codigo_param", codigoInt);
                
                var query = @"
                    SELECT CODIGO, EMPLEADO, DNI, CD_TRANSP
                    FROM PIMS_GRE.VISTA_EMPLEADO
                    WHERE CODIGO = :codigo_param";
                
                var empleados = await _context.VistaEmpleados
                    .FromSqlRaw(query, codigoParam)
                    .AsNoTracking()
                    .ToListAsync();
                
                return empleados.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el empleado con código {Codigo}", codigo);
                throw new InvalidOperationException($"Error al obtener el empleado con código {codigo}", ex);
            }
        }
    }
}