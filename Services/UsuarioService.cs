using Microsoft.EntityFrameworkCore;
using GuiasBackend.Data;
using GuiasBackend.Models;
using GuiasBackend.Services.Interfaces;
using BCrypt.Net;
using Microsoft.Extensions.Logging;
using GuiasBackend.Extensions;
using GuiasBackend.Models.Common;
using Oracle.ManagedDataAccess.Client;
using GuiasBackend.Constants;

namespace GuiasBackend.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UsuarioService> _logger;

        public UsuarioService(ApplicationDbContext context, ILogger<UsuarioService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Método para obtener todos los usuarios sin paginación
        public async Task<IEnumerable<Usuario>> GetAllUsuariosAsync()
        {
            try
            {
                return await _context.Usuarios
                    .OrderBy(u => u.NOMBRES)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todos los usuarios");
                throw new InvalidOperationException("Error al obtener la lista de usuarios", ex);
            }
        }

        // Método con paginación - Corrigiendo advertencias S1006 (valores por defecto) y S1481 (variable no utilizada)
        public async Task<PagedResponse<Usuario>> GetUsuariosAsync(int page = 1, int pageSize = 50, bool all = false, bool includeInactive = false, CancellationToken cancellationToken = default)
        {
            try
            {
                var totalRecords = await _context.Usuarios.CountAsync(cancellationToken);
                
                // Cambiado el ordenamiento a descendente por ID (los más recientes primero)
                IQueryable<Usuario> query;
                
                if (all)
                {
                    // Si se solicitan todos los registros, ignoramos la paginación
                    query = _context.Usuarios
                        .Where(u => includeInactive || u.ESTADO == "1")
                        .OrderByDescending(u => u.ID)
                        .AsNoTracking();
                }
                else
                {
                    // Paginación con ordenamiento descendente
                    int startRow = (page - 1) * pageSize;
                    int endRow = page * pageSize;

                    if (includeInactive)
                    {
                        query = _context.Usuarios
                            .FromSql($@"
                                SELECT *
                                FROM (
                                    SELECT u.*, ROWNUM rn
                                    FROM (
                                        SELECT *
                                        FROM USUARIO
                                        ORDER BY ID DESC
                                    ) u
                                    WHERE ROWNUM <= {endRow}
                                )
                                WHERE rn > {startRow}")
                            .AsNoTracking();
                    }
                    else
                    {
                        query = _context.Usuarios
                            .FromSql($@"
                                SELECT *
                                FROM (
                                    SELECT u.*, ROWNUM rn
                                    FROM (
                                        SELECT *
                                        FROM USUARIO
                                        WHERE ESTADO = '1'
                                        ORDER BY ID DESC
                                    ) u
                                    WHERE ROWNUM <= {endRow}
                                )
                                WHERE rn > {startRow}")
                            .AsNoTracking();
                    }
                }
                
                var usuarios = await query.ToListAsync(cancellationToken);
                
                // Actualizar el conteo total si estamos filtrando por estado
                if (!includeInactive)
                {
                    totalRecords = await _context.Usuarios.CountAsync(u => u.ESTADO == "1", cancellationToken);
                }
                
                return new PagedResponse<Usuario>(
                    data: usuarios,
                    page: page,
                    pageSize: all ? totalRecords : pageSize,
                    totalCount: totalRecords
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuarios paginados");
                throw new InvalidOperationException("Error al obtener usuarios paginados", ex);
            }
        }

        public async Task<Usuario?> GetUsuarioByIdAsync(int id)
        {
            try
            {
                var idParam = new OracleParameter();
                idParam.ParameterName = "id";
                idParam.OracleDbType = OracleDbType.Int32;
                idParam.Value = id;
                
                var usuarios = await _context.Usuarios
                    .FromSqlRaw(SqlQueries.GetUsuarioById, idParam)
                    .AsNoTracking()
                    .ToListAsync();
                
                return usuarios.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el usuario con ID {Id}", id);
                throw new InvalidOperationException($"Error al obtener el usuario con ID {id}", ex);
            }
        }

        public async Task<Usuario?> GetUsuarioByUsernameAsync(string username)
        {
            try
            {
                var usernameParam = new OracleParameter();
                usernameParam.ParameterName = "username";
                usernameParam.OracleDbType = OracleDbType.Varchar2;
                usernameParam.Value = username;
                
                var usuarios = await _context.Usuarios
                    .FromSqlRaw(SqlQueries.GetUsuarioByUsername, usernameParam)
                    .AsNoTracking()
                    .ToListAsync();
                
                return usuarios.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el usuario con username {Username}", username);
                throw new InvalidOperationException($"Error al obtener el usuario con username {username}", ex);
            }
        }

        public async Task<Usuario?> GetUsuarioByEmailAsync(string email)
        {
            try
            {
                var emailParam = new OracleParameter();
                emailParam.ParameterName = "email";
                emailParam.OracleDbType = OracleDbType.Varchar2;
                emailParam.Value = email;
                
                var usuarios = await _context.Usuarios
                    .FromSqlRaw(SqlQueries.GetUsuarioByEmail, emailParam)
                    .AsNoTracking()
                    .ToListAsync();
                
                return usuarios.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el usuario con email {Email}", email);
                throw new InvalidOperationException($"Error al obtener el usuario con email {email}", ex);
            }
        }

        public async Task<Usuario> CreateUsuarioAsync(Usuario usuario)
        {
            try
            {
                if (await ExisteUsuarioAsync(usuario.USERNAME))
                {
                    throw new InvalidOperationException($"Ya existe un usuario con el username {usuario.USERNAME}");
                }

                if (!string.IsNullOrWhiteSpace(usuario.EMAIL) && await ExisteEmailAsync(usuario.EMAIL))
                {
                    throw new InvalidOperationException($"Ya existe un usuario con el email {usuario.EMAIL}");
                }

                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();
                return usuario;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear el usuario");
                throw new InvalidOperationException("Error al crear el usuario", ex);
            }
        }

        public async Task<bool> UpdateUsuarioAsync(Usuario usuario)
        {
            try
            {
                // Verificar si el usuario existe
                var existingUsuario = await GetUsuarioByIdAsync(usuario.ID);
                if (existingUsuario == null)
                {
                    throw new InvalidOperationException($"No existe un usuario con ID {usuario.ID}");
                }

                // Verificar username único si ha cambiado
                if (usuario.USERNAME != existingUsuario.USERNAME && await ExisteUsuarioAsync(usuario.USERNAME))
                {
                    throw new InvalidOperationException($"Ya existe un usuario con el username {usuario.USERNAME}");
                }

                // Verificar email único si ha cambiado
                if (!string.IsNullOrWhiteSpace(usuario.EMAIL) && usuario.EMAIL != existingUsuario.EMAIL && await ExisteEmailAsync(usuario.EMAIL))
                {
                    throw new InvalidOperationException($"Ya existe un usuario con el email {usuario.EMAIL}");
                }

                // Construir la consulta SQL base
                var sql = @"
                    UPDATE PIMS_GRE.USUARIO 
                    SET USERNAME = :username,
                        NOMBRES = :nombres,
                        APELLIDOS = :apellidos,
                        EMAIL = :email,
                        ROL = :rol,
                        ESTADO = :estado,
                        FECHA_ACTUALIZACION = SYSDATE";

                var parameters = new List<OracleParameter>
                {
                    new OracleParameter("username", usuario.USERNAME),
                    new OracleParameter("nombres", usuario.NOMBRES),
                    new OracleParameter("apellidos", usuario.APELLIDOS),
                    new OracleParameter("email", usuario.EMAIL),
                    new OracleParameter("rol", usuario.ROL),
                    new OracleParameter("estado", usuario.ESTADO),
                    new OracleParameter("id", usuario.ID)
                };

                // Si se proporciona una nueva contraseña, hashearla y agregarla a la actualización
                if (!string.IsNullOrEmpty(usuario.CONTRASEÑA) && usuario.CONTRASEÑA != existingUsuario.CONTRASEÑA)
                {
                    sql += ", CONTRASEÑA = :contraseña";
                    parameters.Add(new OracleParameter("contraseña", BCrypt.Net.BCrypt.HashPassword(usuario.CONTRASEÑA)));
                }

                sql += " WHERE ID = :id";

                var rowsAffected = await _context.Database.ExecuteSqlRawAsync(sql, parameters.ToArray());
                
                _logger.LogInformation("Actualización de usuario {Id} completada. Filas afectadas: {RowsAffected}", usuario.ID, rowsAffected);
                
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar el usuario {Id}", usuario.ID);
                throw new InvalidOperationException($"Error al actualizar el usuario con ID {usuario.ID}", ex);
            }
        }

        public async Task<bool> DeleteUsuarioAsync(int id)
        {
            try
            {
                var usuario = await GetUsuarioByIdAsync(id);
                if (usuario == null)
                {
                    throw new InvalidOperationException($"No existe un usuario con ID {id}");
                }

                _context.Usuarios.Remove(usuario);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar el usuario {Id}", id);
                throw new InvalidOperationException($"Error al eliminar el usuario con ID {id}", ex);
            }
        }

        public async Task<bool> ExisteUsuarioAsync(string username)
        {
            try
            {
                var usernameParam = new OracleParameter();
                usernameParam.ParameterName = "username";
                usernameParam.OracleDbType = OracleDbType.Varchar2;
                usernameParam.Value = username;
                
                var result = await _context.Database.ExecuteSqlRawAsync(
                    SqlQueries.ExisteUsuario, 
                    usernameParam);
                
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar existencia del usuario {Username}", username);
                throw new InvalidOperationException($"Error al verificar existencia del usuario {username}", ex);
            }
        }

        public async Task<bool> ExisteEmailAsync(string email)
        {
            try
            {
                var emailParam = new OracleParameter();
                emailParam.ParameterName = "email";
                emailParam.OracleDbType = OracleDbType.Varchar2;
                emailParam.Value = email;
                
                var result = await _context.Database.ExecuteSqlRawAsync(
                    SqlQueries.ExisteEmail, 
                    emailParam);
                
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar existencia del email {Email}", email);
                throw new InvalidOperationException($"Error al verificar existencia del email {email}", ex);
            }
        }

        public async Task<bool> ExisteUsuarioByIdAsync(int id)
        {
            try
            {
                var idParam = new OracleParameter
                {
                    ParameterName = "id",
                    OracleDbType = OracleDbType.Int32,
                    Value = id
                };
                
                // CORREGIDO: Eliminamos el filtro ESTADO='1' que estaba causando el problema
                var query = "SELECT COUNT(*) FROM USUARIO WHERE ID = :id";
                var result = await _context.Database.ExecuteSqlRawAsync(query, idParam);
                
                _logger.LogInformation("Verificando existencia del usuario ID {Id}: Resultado = {Result}", id, result);
                
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar existencia del usuario con ID {Id}", id);
                throw new InvalidOperationException($"Error al verificar existencia del usuario con ID {id}", ex);
            }
        }

        public async Task<bool> ValidarCredencialesAsync(string username, string password)
        {
            try
            {
                var usuario = await GetUsuarioByUsernameAsync(username);
                if (usuario == null)
                {
                    return false;
                }

                return BCrypt.Net.BCrypt.Verify(password, usuario.CONTRASEÑA);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar credenciales para el usuario {Username}", username);
                throw new InvalidOperationException($"Error al validar credenciales para el usuario {username}", ex);
            }
        }

        public async Task<bool> CambiarContraseñaAsync(int id, string nuevaContraseña)
        {
            try
            {
                var usuario = await GetUsuarioByIdAsync(id);
                if (usuario == null)
                {
                    throw new InvalidOperationException($"No existe un usuario con ID {id}");
                }

                // Hashear la nueva contraseña
                string contraseñaHasheada = BCrypt.Net.BCrypt.HashPassword(nuevaContraseña);

                // Actualizar usando SQL nativo
                var sql = @"
                    UPDATE PIMS_GRE.USUARIO 
                    SET CONTRASEÑA = :contraseña,
                        FECHA_ACTUALIZACION = SYSDATE
                    WHERE ID = :id";

                var parameters = new[]
                {
                    new OracleParameter("contraseña", contraseñaHasheada),
                    new OracleParameter("id", id)
                };

                var rowsAffected = await _context.Database.ExecuteSqlRawAsync(sql, parameters);
                
                _logger.LogInformation("Cambio de contraseña para usuario {Id} completado. Filas afectadas: {RowsAffected}", id, rowsAffected);
                
                return rowsAffected > 0;
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                _logger.LogError(ex, "Error al cambiar la contraseña del usuario {Id}", id);
                throw new InvalidOperationException($"Error al cambiar la contraseña del usuario con ID {id}", ex);
            }
        }

        public async Task<bool> ActualizarEstadoAsync(int id, string nuevoEstado)
        {
            try
            {
                if (nuevoEstado != "0" && nuevoEstado != "1")
                {
                    throw new InvalidOperationException("El estado debe ser '0' (inactivo) o '1' (activo)");
                }

                var usuario = await GetUsuarioByIdAsync(id);
                if (usuario == null)
                {
                    throw new InvalidOperationException($"No existe un usuario con ID {id}");
                }

                // Mejor enfoque: usar Entity Framework directamente
                usuario.ESTADO = nuevoEstado;
                usuario.FECHA_ACTUALIZACION = DateTime.Now;
                await _context.SaveChangesAsync();
                
                return true;
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                _logger.LogError(ex, "Error al actualizar el estado del usuario {Id}", id);
                throw new InvalidOperationException($"Error al actualizar el estado del usuario con ID {id}", ex);
            }
        }
    }
}