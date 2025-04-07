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
    public class TransportistaService : ITransportistaService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TransportistaService> _logger;

        public TransportistaService(
            ApplicationDbContext context,
            ILogger<TransportistaService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PagedResponse<VistaTransportista>> GetAllTransportistasAsync(int page = 1, int pageSize = 50, bool all = false, CancellationToken cancellationToken = default)
        {
            try
            {
                if (all)
                {
                    // Configurar parámetros explícitamente
                    var endRowParam = new OracleParameter();
                    endRowParam.ParameterName = "endRow";
                    endRowParam.OracleDbType = OracleDbType.Int32;
                    endRowParam.Value = 100000;
                    
                    var startRowParam = new OracleParameter();
                    startRowParam.ParameterName = "startRow";
                    startRowParam.OracleDbType = OracleDbType.Int32;
                    startRowParam.Value = 0;
                    
                    var allItems = await _context.VistaTransportistas
                        .FromSqlRaw(
                            SqlQueries.GetAllTransportistas,
                            endRowParam,
                            startRowParam
                        )
                        .AsNoTracking()
                        .ToListAsync(cancellationToken);
                    
                    return new PagedResponse<VistaTransportista>(allItems, 1, allItems.Count, allItems.Count);
                }

                // Código existente para paginación
                var endRowPagedParam = new OracleParameter();
                endRowPagedParam.ParameterName = "endRow";
                endRowPagedParam.OracleDbType = OracleDbType.Int32;
                endRowPagedParam.Value = pageSize * page;
                
                var startRowPagedParam = new OracleParameter();
                startRowPagedParam.ParameterName = "startRow";
                startRowPagedParam.OracleDbType = OracleDbType.Int32;
                startRowPagedParam.Value = pageSize * (page - 1);
                
                var pagedItems = await _context.VistaTransportistas
                    .FromSqlRaw(
                        SqlQueries.GetAllTransportistas,
                        endRowPagedParam,
                        startRowPagedParam
                    )
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);
                
                var totalCount = await _context.VistaTransportistas.CountAsync(cancellationToken);
                
                return new PagedResponse<VistaTransportista>(pagedItems, page, pageSize, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todos los transportistas");
                throw new InvalidOperationException("Error al obtener la lista de transportistas", ex);
            }
        }

        public async Task<VistaTransportista?> GetTransportistaByCodTranspAsync(int codTransp)
        {
            if (codTransp <= 0)
            {
                throw new ArgumentException("El código del transportista debe ser mayor a 0", nameof(codTransp));
            }

            try
            {
                // Definir la consulta SQL directamente aquí ya que no está en SqlQueries
                const string query = "SELECT COD_TRANSP, TRANSPORTISTA, RUC FROM PIMS_GRE.VISTA_TRANSPORTISTA WHERE COD_TRANSP = :cod_transp_param";
                
                var codTranspParam = new OracleParameter();
                codTranspParam.ParameterName = "cod_transp_param";
                codTranspParam.OracleDbType = OracleDbType.Int32;
                codTranspParam.Value = codTransp;
                
                var transportistas = await _context.VistaTransportistas
                    .FromSqlRaw(
                        query,
                        codTranspParam
                    )
                    .AsNoTracking()
                    .ToListAsync();
                
                return transportistas.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el transportista con código {CodTransp}", codTransp);
                throw new InvalidOperationException($"Error al obtener el transportista con código {codTransp}", ex);
            }
        }

        public async Task<VistaTransportista?> GetTransportistaByRucAsync(string ruc)
        {
            if (string.IsNullOrWhiteSpace(ruc))
            {
                throw new ArgumentException("El RUC no puede estar vacío", nameof(ruc));
            }

            if (ruc.Length > 18)
            {
                throw new ArgumentException("El RUC no puede exceder los 18 caracteres", nameof(ruc));
            }

            try
            {
                // Definir la consulta SQL directamente aquí ya que no está en SqlQueries
                const string query = "SELECT COD_TRANSP, TRANSPORTISTA, RUC FROM PIMS_GRE.VISTA_TRANSPORTISTA WHERE RUC = :ruc_param";
                
                var rucParam = new OracleParameter();
                rucParam.ParameterName = "ruc_param";
                rucParam.OracleDbType = OracleDbType.Varchar2;
                rucParam.Value = ruc.ToUpper();
                
                var transportistas = await _context.VistaTransportistas
                    .FromSqlRaw(
                        query,
                        rucParam
                    )
                    .AsNoTracking()
                    .ToListAsync();
                
                return transportistas.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el transportista con RUC {Ruc}", ruc);
                throw new InvalidOperationException($"Error al obtener el transportista con RUC {ruc}", ex);
            }
        }
    }
}