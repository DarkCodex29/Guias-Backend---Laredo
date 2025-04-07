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
    public class JironService : IJironService
    {
        // Constantes locales para parámetros
        private const string PARAM_END_ROW = "endRow";
        private const string PARAM_START_ROW = "startRow";
        private const string PARAM_CAMPO = "campo_param";
        private const string PARAM_JIRON = "jiron_param";

        private readonly ApplicationDbContext _context;
        private readonly ILogger<JironService> _logger;

        public JironService(
            ApplicationDbContext context,
            ILogger<JironService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PagedResponse<VistaJiron>> GetAllJironesAsync(int page = 1, int pageSize = 50, bool all = false, CancellationToken cancellationToken = default)
        {
            try
            {
                if (all)
                {
                    // Usar constantes locales
                    var endRowParam = new OracleParameter();
                    endRowParam.ParameterName = PARAM_END_ROW;
                    endRowParam.OracleDbType = OracleDbType.Int32;
                    endRowParam.Value = 100000;
                    
                    var startRowParam = new OracleParameter();
                    startRowParam.ParameterName = PARAM_START_ROW;
                    startRowParam.OracleDbType = OracleDbType.Int32;
                    startRowParam.Value = 0;
                    
                    var allItems = await _context.VistaJirones
                        .FromSqlRaw(
                            SqlQueries.GetAllJirones,
                            endRowParam,
                            startRowParam
                        )
                        .AsNoTracking()
                        .ToListAsync(cancellationToken);
                        
                    return new PagedResponse<VistaJiron>(allItems, 1, allItems.Count, allItems.Count);
                }

                // Usar constantes locales en todas partes
                var endRowPagedParam = new OracleParameter();
                endRowPagedParam.ParameterName = PARAM_END_ROW;
                endRowPagedParam.OracleDbType = OracleDbType.Int32;
                endRowPagedParam.Value = pageSize * page;
                
                var startRowPagedParam = new OracleParameter();
                startRowPagedParam.ParameterName = PARAM_START_ROW;
                startRowPagedParam.OracleDbType = OracleDbType.Int32;
                startRowPagedParam.Value = pageSize * (page - 1);
                
                var query = _context.VistaJirones
                    .FromSqlRaw(
                        SqlQueries.GetAllJirones,
                        endRowPagedParam,
                        startRowPagedParam
                    )
                    .AsNoTracking();
                
                var totalCount = await _context.VistaJirones.CountAsync(cancellationToken);
                var items = await query.ToListAsync(cancellationToken);
                
                return new PagedResponse<VistaJiron>(items, page, pageSize, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todos los jirones (página {Page}, tamaño {PageSize}, todos: {All})", page, pageSize, all);
                throw new InvalidOperationException("Error al obtener la lista de jirones", ex);
            }
        }

        public async Task<VistaJiron?> GetJironByJironAsync(string jiron)
        {
            if (string.IsNullOrWhiteSpace(jiron))
            {
                throw new ArgumentException("El código del jirón no puede estar vacío", nameof(jiron));
            }

            if (jiron.Length > 8)
            {
                throw new ArgumentException("El código del jirón no puede exceder los 8 caracteres", nameof(jiron));
            }

            try
            {
                var jirones = await _context.VistaJirones
                    .FromSqlRaw(
                        SqlQueries.GetJironByJiron,
                        new OracleParameter(PARAM_JIRON, jiron.ToUpper())
                    )
                    .AsNoTracking()
                    .ToListAsync();
                
                return jirones.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el jirón {Jiron}", jiron);
                throw new InvalidOperationException($"Error al obtener el jirón {jiron}", ex);
            }
        }

        public async Task<PagedResponse<VistaJiron>> GetJironesByCampoAsync(string campo, int page = 1, int pageSize = 50, bool all = false, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(campo)) throw new ArgumentException("El código del campo no puede estar vacío", nameof(campo));
            
            if (campo.Length > 6) throw new ArgumentException("El código del campo no puede exceder los 6 caracteres", nameof(campo));
           

            try
            {
                if (all)
                {
                    // Usar constantes locales
                    var campoParamAll = new OracleParameter();
                    campoParamAll.ParameterName = PARAM_CAMPO;
                    campoParamAll.OracleDbType = OracleDbType.Varchar2;
                    campoParamAll.Value = campo.ToUpper();
                    
                    var endRowParam = new OracleParameter();
                    endRowParam.ParameterName = PARAM_END_ROW;
                    endRowParam.OracleDbType = OracleDbType.Int32;
                    endRowParam.Value = 100000;
                    
                    var startRowParam = new OracleParameter();
                    startRowParam.ParameterName = PARAM_START_ROW;
                    startRowParam.OracleDbType = OracleDbType.Int32;
                    startRowParam.Value = 0;
                    
                    var allItems = await _context.VistaJirones
                        .FromSqlRaw(
                            SqlQueries.GetJironesByCampo,
                            campoParamAll,
                            endRowParam,
                            startRowParam
                        )
                        .AsNoTracking()
                        .ToListAsync(cancellationToken);
                        
                    return new PagedResponse<VistaJiron>(allItems, 1, allItems.Count, allItems.Count);
                }

                // Código existente para paginación
                var campoParam = new OracleParameter();
                campoParam.ParameterName = PARAM_CAMPO;
                campoParam.OracleDbType = OracleDbType.Varchar2;
                campoParam.Value = campo.ToUpper();
                
                var endRowPagedParam = new OracleParameter();
                endRowPagedParam.ParameterName = PARAM_END_ROW;
                endRowPagedParam.OracleDbType = OracleDbType.Int32;
                endRowPagedParam.Value = pageSize * page;
                
                var startRowPagedParam = new OracleParameter();
                startRowPagedParam.ParameterName = PARAM_START_ROW;
                startRowPagedParam.OracleDbType = OracleDbType.Int32;
                startRowPagedParam.Value = pageSize * (page - 1);
                
                var jirones = await _context.VistaJirones
                    .FromSqlRaw(
                        SqlQueries.GetJironesByCampo,
                        campoParam,
                        endRowPagedParam,
                        startRowPagedParam
                    )
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);
                
                // Consulta separada para contar registros
                const string countSql = "SELECT COUNT(*) FROM PIMS_GRE.VISTA_JIRON WHERE CAMPO = :campo_param";
                var totalCount = await _context.Database.ExecuteSqlRawAsync(
                    countSql, 
                    new OracleParameter(PARAM_CAMPO, campo.ToUpper())
                );
                
                return new PagedResponse<VistaJiron>(jirones, page, pageSize, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener los jirones del campo {Campo} (todos: {All})", campo, all);
                throw new InvalidOperationException($"Error al obtener los jirones del campo {campo}", ex);
            }
        }
    }
}