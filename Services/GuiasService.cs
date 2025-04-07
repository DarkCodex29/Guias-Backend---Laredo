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
    public class GuiasService : IGuiasService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<GuiasService> _logger;

        public GuiasService(ApplicationDbContext context, ILogger<GuiasService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Método para obtener todas las guías - corregido para Oracle
        public async Task<PagedResponse<Guia>> GetGuiasAsync(int page = 1, int pageSize = 50, bool all = false, CancellationToken cancellationToken = default)
        {
            try
            {
                // Obtener el total de registros usando SQL nativo
                int totalRecords = 0;
                var countSql = "SELECT COUNT(*) AS TOTAL FROM PIMS_GRE.GUIAS";
                var countResult = await _context.Database
                    .SqlQueryRaw<CountResult>(countSql)
                    .ToListAsync(cancellationToken);
                if (countResult.Count > 0)
                {
                    totalRecords = countResult[0].TOTAL;
                }
                
                List<Guia> guias;
                
                if (all)
                {
                    // Para todos los registros, usamos SQL nativo
                    var allQuery = "SELECT ID, NOMBRE, ARCHIVO, FECHA_SUBIDA, ID_USUARIO FROM PIMS_GRE.GUIAS ORDER BY ID DESC";
                    guias = await _context.Guias
                        .FromSqlRaw(allQuery)
                        .AsNoTracking()
                        .ToListAsync(cancellationToken);
                }
                else
                {
                    // Para paginación usamos SQL nativo con ROWNUM que es compatible con Oracle
                    var pagedQuery = @"
                        SELECT ID, NOMBRE, ARCHIVO, FECHA_SUBIDA, ID_USUARIO
                        FROM (
                            SELECT t.*, ROWNUM rn
                            FROM (
                                SELECT ID, NOMBRE, ARCHIVO, FECHA_SUBIDA, ID_USUARIO 
                                FROM PIMS_GRE.GUIAS
                                ORDER BY ID DESC
                            ) t
                            WHERE ROWNUM <= :endRow
                        )
                        WHERE rn > :startRow";

                    var endRowParam = new OracleParameter();
                    endRowParam.ParameterName = "endRow";
                    endRowParam.OracleDbType = OracleDbType.Int32;
                    endRowParam.Value = pageSize * page;
                    
                    var startRowParam = new OracleParameter();
                    startRowParam.ParameterName = "startRow";
                    startRowParam.OracleDbType = OracleDbType.Int32;
                    startRowParam.Value = pageSize * (page - 1);

                    guias = await _context.Guias
                        .FromSqlRaw(pagedQuery, endRowParam, startRowParam)
                        .AsNoTracking()
                        .ToListAsync(cancellationToken);
                }
                
                // Cargar usuarios utilizando SQL nativo si hay guías
                if (guias.Count > 0)
                {
                    // Extraer los IDs de usuarios únicos
                    var userIds = new HashSet<int>();
                    foreach (var guia in guias)
                    {
                        userIds.Add(guia.ID_USUARIO);
                    }
                    
                    // Preparar el parámetro IN para la consulta SQL
                    var idsList = string.Join(",", userIds);
                    
                    // Ejecutar la consulta SQL nativa para obtener los usuarios
                    var usuariosSql = $@"
                        SELECT 
                            ID, 
                            USERNAME
                        FROM PIMS_GRE.USUARIO 
                        WHERE ID IN ({idsList}) 
                        AND ESTADO = '1'";
                    
                    var usuariosSimples = await _context.Database
                        .SqlQueryRaw<UsuarioSimple>(usuariosSql)
                        .ToListAsync(cancellationToken);
                    
                    // Crear un mapa de usuarios simplificados por ID para asignación eficiente
                    var usuariosMap = new Dictionary<int, UsuarioSimple>();
                    foreach (var usuario in usuariosSimples)
                    {
                        usuariosMap[usuario.ID] = usuario;
                    }
                    
                    // Asignar usuarios simplificados a guías
                    guias.Where(g => usuariosMap.ContainsKey(g.ID_USUARIO))
                         .ToList()
                         .ForEach(g => {
                             var usuarioSimple = usuariosMap[g.ID_USUARIO];
                             g.Usuario = new Usuario 
                             { 
                                 ID = usuarioSimple.ID,
                                 USERNAME = usuarioSimple.USERNAME
                             };
                             g.UsernameUsuario = usuarioSimple.USERNAME;
                         });
                }
                
                return new PagedResponse<Guia>(
                    data: guias,
                    page: page,
                    pageSize: all ? totalRecords : pageSize,
                    totalCount: totalRecords
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener guías paginadas");
                throw new InvalidOperationException("Error al obtener guías paginadas", ex);
            }
        }

        // Método para obtener una guía por ID 
        public async Task<Guia?> GetGuiaByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var idParam = new OracleParameter();
                idParam.ParameterName = "id_param";
                idParam.OracleDbType = OracleDbType.Int32;
                idParam.Value = id;
                
                var query = "SELECT ID, NOMBRE, ARCHIVO, FECHA_SUBIDA, ID_USUARIO FROM PIMS_GRE.GUIAS WHERE ID = :id_param";
                
                var guias = await _context.Guias
                    .FromSqlRaw(query, idParam)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);
                
                var guia = guias.FirstOrDefault();
                if (guia != null)
                {
                    // Cargar el usuario de la guía
                    guia.Usuario = await _context.Usuarios
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.ID == guia.ID_USUARIO, cancellationToken);
                }
                
                return guia;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener la guía con ID {Id}", id);
                throw new InvalidOperationException($"Error al obtener la guía con ID {id}", ex);
            }
        }

        // Método para guardar guía con exactamente los campos del esquema DB
        public async Task<Guia> CreateGuiaAsync(Guia guia, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validación básica
                if (string.IsNullOrEmpty(guia.NOMBRE))
                {
                    throw new ArgumentException("El nombre de la guía no puede estar vacío");
                }

                if (guia.ID_USUARIO <= 0)
                {
                    throw new ArgumentException("ID de usuario inválido");
                }

                // Verificar si existe el nombre
                if (await ExisteGuiaAsync(guia.NOMBRE, cancellationToken))
                {
                    throw new InvalidOperationException($"Ya existe una guía con el nombre {guia.NOMBRE}");
                }

                // Establecer fecha de subida si no se proporcionó
                if (guia.FECHA_SUBIDA == default)
                {
                    guia.FECHA_SUBIDA = DateTime.UtcNow;
                }

                _context.Guias.Add(guia);
                await _context.SaveChangesAsync(cancellationToken);
                return guia;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear la guía");
                throw new InvalidOperationException("Error al crear la guía", ex);
            }
        }

        // Método optimizado para obtener guías por ID de usuario
        public async Task<IEnumerable<Guia>> GetGuiasByUsuarioIdAsync(int idUsuario, int page = 1, int pageSize = 20, bool all = false, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Obteniendo guías para el usuario con ID: {IdUsuario}", idUsuario);
                
                // Eliminamos la validación de usuario, ya lo hace el controlador
                
                // Usar SQL nativo para esta consulta también
                if (all)
                {
                    var allQuery = @"
                        SELECT ID, NOMBRE, ARCHIVO, FECHA_SUBIDA, ID_USUARIO 
                        FROM PIMS_GRE.GUIAS
                        WHERE ID_USUARIO = :id_usuario_param
                        ORDER BY FECHA_SUBIDA DESC";

                    var idUsuarioParamAll = new OracleParameter
                    {
                        ParameterName = "id_usuario_param",
                        OracleDbType = OracleDbType.Int32,
                        Value = idUsuario
                    };

                    var guias = await _context.Guias
                        .FromSqlRaw(allQuery, idUsuarioParamAll)
                        .AsNoTracking()
                        .ToListAsync(cancellationToken);
                        
                    if (guias.Count == 0)
                    {
                        _logger.LogInformation("El usuario con ID {IdUsuario} no tiene guías asignadas", idUsuario);
                    }
                    
                    // Cargar el usuario para cada guía
                    var usuario = await _context.Usuarios
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.ID == idUsuario, cancellationToken);
                        
                    foreach (var guia in guias)
                    {
                        guia.Usuario = usuario;
                    }
                        
                    return guias;
                }
                else
                {
                    var pagedQuery = @"
                        SELECT ID, NOMBRE, ARCHIVO, FECHA_SUBIDA, ID_USUARIO
                        FROM (
                            SELECT t.*, ROWNUM rn
                            FROM (
                                SELECT ID, NOMBRE, ARCHIVO, FECHA_SUBIDA, ID_USUARIO 
                                FROM PIMS_GRE.GUIAS
                                WHERE ID_USUARIO = :id_usuario_param
                                ORDER BY FECHA_SUBIDA DESC
                            ) t
                            WHERE ROWNUM <= :endRow
                        )
                        WHERE rn > :startRow";

                    var idUsuarioParam = new OracleParameter
                    {
                        ParameterName = "id_usuario_param",
                        OracleDbType = OracleDbType.Int32,
                        Value = idUsuario
                    };
                    
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

                    var guias = await _context.Guias
                        .FromSqlRaw(pagedQuery, idUsuarioParam, endRowParam, startRowParam)
                        .AsNoTracking()
                        .ToListAsync(cancellationToken);
                        
                    if (guias.Count == 0)
                    {
                        _logger.LogInformation("El usuario con ID {IdUsuario} no tiene guías asignadas en la página {Page}", idUsuario, page);
                    }
                        
                    // Cargar el usuario para cada guía
                    var usuario = await _context.Usuarios
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.ID == idUsuario, cancellationToken);
                        
                    foreach (var guia in guias)
                    {
                        guia.Usuario = usuario;
                    }
                        
                    return guias;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener las guías del usuario {IdUsuario}", idUsuario);
                throw new InvalidOperationException($"Error al obtener las guías del usuario {idUsuario}", ex);
            }
        }

        public async Task<bool> ExisteGuiaAsync(string nombre, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _context.Database.ExecuteSqlRawAsync(
                    SqlQueries.ExisteGuiaByNombre,
                    new OracleParameter("nombre_param", nombre)
                );
                
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar existencia de la guía {Nombre}", nombre);
                throw new InvalidOperationException($"Error al verificar existencia de la guía {nombre}", ex);
            }
        }

        public async Task<string> GenerarCorrelativoGuiaAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                string? ultimoNombre = await ObtenerUltimoNombreAsync(cancellationToken);
                int siguienteNumero = ObtenerSiguienteNumero(ultimoNombre);
                
                // Formatear el nuevo correlativo
                return $"T002-{siguienteNumero:D8}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar el correlativo de guía");
                throw new InvalidOperationException("Error al generar el correlativo de guía", ex);
            }
        }
        
        private async Task<string?> ObtenerUltimoNombreAsync(CancellationToken cancellationToken)
        {
            // Consulta directa con ExecuteSqlInterpolatedAsync para obtener solo los datos que necesitamos
            var sql = @"
                SELECT NOMBRE 
                FROM (
                    SELECT NOMBRE 
                    FROM PIMS_GRE.GUIAS 
                    ORDER BY ID DESC
                ) 
                WHERE ROWNUM = 1";

            // Ejecutar directamente con ADO.NET sin mapear a entidad
            string? ultimoNombre = null;
            using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = sql;
                
                if (command.Connection != null && command.Connection.State != System.Data.ConnectionState.Open)
                {
                    await command.Connection.OpenAsync(cancellationToken);
                }
                
                using var reader = await command.ExecuteReaderAsync(cancellationToken);
                if (await reader.ReadAsync(cancellationToken))
                {
                    ultimoNombre = !(await reader.IsDBNullAsync(0, cancellationToken)) ? reader.GetString(0) : null;
                }
            }
            
            return ultimoNombre;
        }
        
        private int ObtenerSiguienteNumero(string? ultimoNombre)
        {
            int siguienteNumero = 100; // Comenzamos desde 100 si no hay guías

            if (string.IsNullOrEmpty(ultimoNombre))
            {
                _logger.LogInformation("No se encontraron guías previas, se comenzará con el número {SiguienteNumero}", siguienteNumero);
                return siguienteNumero;
            }
            
            _logger.LogInformation("Último nombre de guía encontrado: {UltimoNombre}", ultimoNombre);
            
            // Buscar el patrón T002-XXXXXXXX en el nombre
            int indexT002 = ultimoNombre.IndexOf("T002-");
            if (indexT002 < 0)
            {
                _logger.LogWarning("No se encontró el patrón T002- en el nombre: {UltimoNombre}", ultimoNombre);
                return siguienteNumero;
            }
            
            // Extraer la parte después de "T002-"
            string numeroStr = ultimoNombre.Substring(indexT002 + 5);
            
            // Si hay una extensión, quitarla
            int indexPunto = numeroStr.IndexOf('.');
            if (indexPunto >= 0)
            {
                numeroStr = numeroStr.Substring(0, indexPunto);
            }
            
            // Intentar convertir a número
            if (int.TryParse(numeroStr, out int ultimoNumero))
            {
                siguienteNumero = ultimoNumero + 1;
                _logger.LogInformation("Número extraído: {UltimoNumero}, Siguiente: {SiguienteNumero}", ultimoNumero, siguienteNumero);
            }
            else
            {
                _logger.LogWarning("No se pudo convertir a número: {NumeroStr}", numeroStr);
            }
            
            return siguienteNumero;
        }

        public async Task<bool> DeleteGuiaAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var guia = await _context.Guias.FindAsync(new object[] { id }, cancellationToken);
                if (guia == null)
                {
                    return false;
                }

                _context.Guias.Remove(guia);
                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar la guía con ID {Id}", id);
                throw new InvalidOperationException($"Error al eliminar la guía con ID {id}", ex);
            }
        }

        private sealed class CountResult
        {
            public int TOTAL { get; set; } = 0;
        }

        private sealed class UsuarioSimple
        {
            public int ID { get; set; } = 0;
            public string USERNAME { get; set; } = string.Empty;
        }
    }
}