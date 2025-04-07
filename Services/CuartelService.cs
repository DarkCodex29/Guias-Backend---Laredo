using Microsoft.EntityFrameworkCore;
using GuiasBackend.Data;
using GuiasBackend.Models;
using GuiasBackend.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using Dapper;
using GuiasBackend.Constants;
using GuiasBackend.Extensions;
using GuiasBackend.Models.Common;

namespace GuiasBackend.Services
{
    public class CuartelService : ICuartelService
    {
        // Constantes locales para parámetros
        private const string PARAM_END_ROW = "endRow";
        private const string PARAM_START_ROW = "startRow";
        private const string PARAM_CAMPO = "campo_param";
        private const string PARAM_CUARTEL = "cuartel_param";

        private readonly ApplicationDbContext _context;
        private readonly ILogger<CuartelService> _logger;

        public CuartelService(
            ApplicationDbContext context,
            ILogger<CuartelService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PagedResponse<VistaCuartel>> GetAllCuartelesAsync(int page = 1, int pageSize = 50, bool all = false, CancellationToken cancellationToken = default)
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
                    
                    var allItems = await _context.VistaCuarteles
                        .FromSqlRaw(
                            SqlQueries.GetAllCuarteles,
                            endRowParam,
                            startRowParam
                        )
                        .AsNoTracking()
                        .ToListAsync(cancellationToken);
                        
                    return new PagedResponse<VistaCuartel>(allItems, 1, allItems.Count, allItems.Count);
                }

                // Usar constantes locales
                var endRowPagedParam = new OracleParameter();
                endRowPagedParam.ParameterName = PARAM_END_ROW;
                endRowPagedParam.OracleDbType = OracleDbType.Int32;
                endRowPagedParam.Value = pageSize * page;
                
                var startRowPagedParam = new OracleParameter();
                startRowPagedParam.ParameterName = PARAM_START_ROW;
                startRowPagedParam.OracleDbType = OracleDbType.Int32;
                startRowPagedParam.Value = pageSize * (page - 1);
                
                var cuarteles = await _context.VistaCuarteles
                    .FromSqlRaw(
                        SqlQueries.GetAllCuarteles,
                        endRowPagedParam,
                        startRowPagedParam
                    )
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);
                
                // Como estamos usando SQL raw, no podemos usar ToPagedResponseAsync directamente
                var totalCount = await _context.VistaCuarteles.CountAsync(cancellationToken);
                
                return new PagedResponse<VistaCuartel>(cuarteles, page, pageSize, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todos los cuarteles (todos: {All})", all);
                throw new InvalidOperationException("Error al obtener la lista de cuarteles", ex);
            }
        }

        public async Task<VistaCuartel?> GetCuartelByCuartelAsync(string cuartel)
        {
            if (string.IsNullOrWhiteSpace(cuartel))
            {
                throw new ArgumentException("El código del cuartel no puede estar vacío", nameof(cuartel));
            }

            if (cuartel.Length > 6)
            {
                throw new ArgumentException("El código del cuartel no puede exceder los 6 caracteres", nameof(cuartel));
            }

            try
            {
                var cuarteles = await _context.VistaCuarteles
                    .FromSqlRaw(
                        SqlQueries.GetCuartelByCuartel,
                        new OracleParameter(PARAM_CUARTEL, cuartel.ToUpper())
                    )
                    .AsNoTracking()
                    .ToListAsync();
                
                return cuarteles.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el cuartel {Cuartel}", cuartel);
                throw new InvalidOperationException($"Error al obtener el cuartel {cuartel}", ex);
            }
        }

        public async Task<PagedResponse<VistaCuartel>> GetCuartelesByCampoAsync(string campo, int page = 1, int pageSize = 50, bool all = false, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(campo))
            {
                throw new ArgumentException("El código del campo no puede estar vacío", nameof(campo));
            }

            if (campo.Length > 6)
            {
                throw new ArgumentException("El código del campo no puede exceder los 6 caracteres", nameof(campo));
            }

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
                    
                    var allItems = await _context.VistaCuarteles
                        .FromSqlRaw(
                            SqlQueries.GetCuartelesByCampo,
                            campoParamAll,
                            endRowParam,
                            startRowParam
                        )
                        .AsNoTracking()
                        .ToListAsync(cancellationToken);
                        
                    return new PagedResponse<VistaCuartel>(allItems, 1, allItems.Count, allItems.Count);
                }

                // Usar constantes locales
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
                
                var cuarteles = await _context.VistaCuarteles
                    .FromSqlRaw(
                        SqlQueries.GetCuartelesByCampo,
                        campoParam,
                        endRowPagedParam,
                        startRowPagedParam
                    )
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);
                
                // Consulta separada para contar registros
                const string countSql = "SELECT COUNT(*) FROM PIMS_GRE.VISTA_CUARTEL WHERE CAMPO = :campo_param";
                var totalCount = await _context.Database.ExecuteSqlRawAsync(
                    countSql, 
                    new OracleParameter(PARAM_CAMPO, campo.ToUpper())
                );
                
                return new PagedResponse<VistaCuartel>(cuarteles, page, pageSize, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener los cuarteles del campo {Campo} (todos: {All})", campo, all);
                throw new InvalidOperationException($"Error al obtener los cuarteles del campo {campo}", ex);
            }
        }
    }
}