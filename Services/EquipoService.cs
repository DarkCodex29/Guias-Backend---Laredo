using Microsoft.EntityFrameworkCore;
using GuiasBackend.Data;
using GuiasBackend.Models;
using GuiasBackend.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using GuiasBackend.Constants;
using GuiasBackend.Models.Common;

namespace GuiasBackend.Services
{
    public class EquipoService : IEquipoService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EquipoService> _logger;

        public EquipoService(
            ApplicationDbContext context,
            ILogger<EquipoService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PagedResponse<VistaEquipo>> GetAllEquiposAsync(int page = 1, int pageSize = 50, bool all = false, CancellationToken cancellationToken = default)
        {
            try
            {
                // Consulta SQL directa usando los nombres correctos de columnas
                var customQuery = all ?
                    @"SELECT COD_EQUIPO, NVL(PLACA, '') as PLACA, COD_TRANSP, NVL(TIP_EQUIPO, '') as TIP_EQUIPO 
                      FROM PIMS_GRE.VISTA_EQUIPOS 
                      ORDER BY COD_EQUIPO" :
                    @"SELECT COD_EQUIPO, NVL(PLACA, '') as PLACA, COD_TRANSP, NVL(TIP_EQUIPO, '') as TIP_EQUIPO
                      FROM (
                          SELECT t.*, ROWNUM rn
                          FROM (
                              SELECT COD_EQUIPO, PLACA, COD_TRANSP, TIP_EQUIPO 
                              FROM PIMS_GRE.VISTA_EQUIPOS
                              ORDER BY COD_EQUIPO
                          ) t
                          WHERE ROWNUM <= :endRow
                      )
                      WHERE rn > :startRow";

                List<VistaEquipo> equipos;
                
                if (all)
                {
                    equipos = await _context.VistaEquipos
                        .FromSqlRaw(customQuery)
                        .AsNoTracking()
                        .ToListAsync(cancellationToken);
                        
                    return new PagedResponse<VistaEquipo>(equipos, 1, equipos.Count, equipos.Count);
                }
                else
                {
                    var endRowParam = new OracleParameter
                    {
                        ParameterName = "endRow",
                        OracleDbType = OracleDbType.Int32,
                        Value = pageSize * page
                    };
                    
                    var startRowParam = new OracleParameter
                    {
                        ParameterName = "startRow",
                        OracleDbType = OracleDbType.Int32,
                        Value = pageSize * (page - 1)
                    };
                    
                    equipos = await _context.VistaEquipos
                        .FromSqlRaw(customQuery, endRowParam, startRowParam)
                        .AsNoTracking()
                        .ToListAsync(cancellationToken);
                        
                    // Para el conteo total, hacemos una consulta separada
                    var countQuery = "SELECT COUNT(*) FROM PIMS_GRE.VISTA_EQUIPOS";
                    var totalCount = await _context.Database.ExecuteSqlRawAsync(countQuery);
                    
                    return new PagedResponse<VistaEquipo>(equipos, page, pageSize, totalCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todos los equipos");
                throw new InvalidOperationException("Error al obtener la lista de equipos", ex);
            }
        }

        public async Task<VistaEquipo?> GetEquipoByCodEquipoAsync(int codEquipo)
        {
            if (codEquipo <= 0)
            {
                throw new ArgumentException("El código del equipo debe ser mayor a 0", nameof(codEquipo));
            }

            try
            {
                var query = @"
                    SELECT COD_EQUIPO, 
                           NVL(PLACA, '') as PLACA, 
                           COD_TRANSP, 
                           NVL(TIP_EQUIPO, '') as TIP_EQUIPO
                    FROM PIMS_GRE.VISTA_EQUIPOS
                    WHERE COD_EQUIPO = :cod_equipo_param";
                    
                var equipos = await _context.VistaEquipos
                    .FromSqlRaw(
                        query,
                        new OracleParameter("cod_equipo_param", codEquipo)
                    )
                    .AsNoTracking()
                    .ToListAsync();
                
                var equipo = equipos.FirstOrDefault();
                if (equipo != null)
                {
                    equipo.Placa = string.IsNullOrEmpty(equipo.Placa) ? null : equipo.Placa;
                    equipo.TipoEquipo = string.IsNullOrEmpty(equipo.TipoEquipo) ? null : equipo.TipoEquipo;
                }
                
                return equipo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el equipo con código {CodEquipo}", codEquipo);
                throw new InvalidOperationException($"Error al obtener el equipo con código {codEquipo}", ex);
            }
        }

        public async Task<VistaEquipo?> GetEquipoByPlacaAsync(string placa, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Buscando equipo con placa: {Placa}", placa);

                if (string.IsNullOrWhiteSpace(placa))
                {
                    throw new ArgumentException("La placa no puede estar vacía", nameof(placa));
                }

                var placaParam = new OracleParameter
                {
                    ParameterName = "placa_param",
                    OracleDbType = OracleDbType.Varchar2,
                    Value = placa.Trim().ToUpper()
                };

                var query = @"
                    SELECT COD_EQUIPO, 
                           NVL(PLACA, '') as PLACA, 
                           COD_TRANSP, 
                           NVL(TIP_EQUIPO, '') as TIP_EQUIPO
                    FROM PIMS_GRE.VISTA_EQUIPOS
                    WHERE UPPER(PLACA) = UPPER(:placa_param)";

                var equipos = await _context.VistaEquipos
                    .FromSqlRaw(query, placaParam)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                var equipo = equipos.FirstOrDefault();
                if (equipo != null)
                {
                    equipo.Placa = string.IsNullOrEmpty(equipo.Placa) ? null : equipo.Placa;
                    equipo.TipoEquipo = string.IsNullOrEmpty(equipo.TipoEquipo) ? null : equipo.TipoEquipo;
                }

                if (equipo == null)
                {
                    _logger.LogInformation("No se encontró ningún equipo con la placa: {Placa}", placa);
                }
                else
                {
                    _logger.LogInformation("Equipo encontrado con placa {Placa}, código: {Codigo}", placa, equipo.Codigo);
                }

                return equipo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar equipo con placa {Placa}", placa);
                throw new InvalidOperationException($"Error al buscar equipo con placa {placa}", ex);
            }
        }

        public async Task<IEnumerable<VistaEquipo>> GetEquiposByCodTranspAsync(int codTransp)
        {
            if (codTransp <= 0)
            {
                throw new ArgumentException("El código del transportista debe ser mayor a 0", nameof(codTransp));
            }

            try
            {
                var query = @"
                    SELECT COD_EQUIPO, 
                           NVL(PLACA, '') as PLACA, 
                           COD_TRANSP, 
                           NVL(TIP_EQUIPO, '') as TIP_EQUIPO
                    FROM PIMS_GRE.VISTA_EQUIPOS
                    WHERE COD_TRANSP = :cod_transp_param";
                    
                var equipos = await _context.VistaEquipos
                    .FromSqlRaw(
                        query,
                        new OracleParameter("cod_transp_param", codTransp)
                    )
                    .AsNoTracking()
                    .ToListAsync();
                
                // Procesar valores nulos para cada equipo
                foreach (var equipo in equipos)
                {
                    equipo.Placa = string.IsNullOrEmpty(equipo.Placa) ? null : equipo.Placa;
                    equipo.TipoEquipo = string.IsNullOrEmpty(equipo.TipoEquipo) ? null : equipo.TipoEquipo;
                }
                
                return equipos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener equipos del transportista con código {CodTransp}", codTransp);
                throw new InvalidOperationException($"Error al obtener equipos del transportista con código {codTransp}", ex);
            }
        }
    }
}