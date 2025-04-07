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
    public class CampoService : ICampoService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CampoService> _logger;

        public CampoService(
            ApplicationDbContext context,
            ILogger<CampoService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PagedResponse<VistaCampo>> GetAllCamposAsync(int page = 1, int pageSize = 50, bool all = false, CancellationToken cancellationToken = default)
        {
            try
            {
                if (all)
                {
                    // Modifica la creación de parámetros para asegurar compatibilidad con Oracle
                    var endRowParam = new OracleParameter();
                    endRowParam.ParameterName = "endRow";
                    endRowParam.OracleDbType = OracleDbType.Int32;
                    endRowParam.Value = 100000;
                    
                    var startRowParam = new OracleParameter();
                    startRowParam.ParameterName = "startRow";
                    startRowParam.OracleDbType = OracleDbType.Int32;
                    startRowParam.Value = 0;
                    
                    var allItems = await _context.VistaCampos
                        .FromSqlRaw(
                            SqlQueries.GetAllCampos,
                            endRowParam,
                            startRowParam
                        )
                        .AsNoTracking()
                        .ToListAsync(cancellationToken);
                        
                    return new PagedResponse<VistaCampo>(allItems, 1, allItems.Count, allItems.Count);
                }

                // También modifica esta parte para ser consistente
                var endRowPagedParam = new OracleParameter();
                endRowPagedParam.ParameterName = "endRow";
                endRowPagedParam.OracleDbType = OracleDbType.Int32;
                endRowPagedParam.Value = pageSize * page;
                
                var startRowPagedParam = new OracleParameter();
                startRowPagedParam.ParameterName = "startRow";
                startRowPagedParam.OracleDbType = OracleDbType.Int32;
                startRowPagedParam.Value = pageSize * (page - 1);
                
                var query = _context.VistaCampos
                    .FromSqlRaw(
                        SqlQueries.GetAllCampos,
                        endRowPagedParam,
                        startRowPagedParam
                    )
                    .AsNoTracking();
                
                var totalCount = await _context.VistaCampos.CountAsync(cancellationToken);
                var items = await query.ToListAsync(cancellationToken);
                
                return new PagedResponse<VistaCampo>(items, page, pageSize, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener los campos");
                throw new InvalidOperationException("Error al obtener la lista de campos", ex);
            }
        }

        public async Task<VistaCampo?> GetCampoByCampoAsync(string campo)
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
                var campos = await _context.VistaCampos
                    .FromSqlRaw(
                        SqlQueries.GetCampoByCodigo,
                        new OracleParameter("campo_param", campo.ToUpper())
                    )
                    .AsNoTracking()
                    .ToListAsync();
                
                return campos.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el campo {Campo}", campo);
                throw new InvalidOperationException($"Error al obtener el campo {campo}", ex);
            }
        }

        public async Task<VistaCampo?> GetCampoByDescripcionAsync(string descripcion)
        {
            if (string.IsNullOrWhiteSpace(descripcion))
            {
                throw new ArgumentException("La descripción no puede estar vacía", nameof(descripcion));
            }

            if (descripcion.Length > 70)
            {
                throw new ArgumentException("La descripción no puede exceder los 70 caracteres", nameof(descripcion));
            }

            try
            {
                // Usamos LIKE en SQL raw en lugar de EF.Functions.Like
                var campos = await _context.VistaCampos
                    .FromSqlRaw(
                        SqlQueries.GetCampoByDescripcion,
                        new OracleParameter("desc_param", "%" + descripcion.ToUpper() + "%")
                    )
                    .AsNoTracking()
                    .ToListAsync();
                
                return campos.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el campo con descripción {Descripcion}", descripcion);
                throw new InvalidOperationException($"Error al obtener el campo con descripción {descripcion}", ex);
            }
        }

        public async Task<bool> ExisteCampoAsync(string campo)
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
                var result = await _context.Database.ExecuteSqlRawAsync(
                    SqlQueries.ExisteCampo,
                    new OracleParameter("campo_param", campo.ToUpper())
                );
                
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar existencia del campo {Campo}", campo);
                throw new InvalidOperationException($"Error al verificar existencia del campo {campo}", ex);
            }
        }
    }
}