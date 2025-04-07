using GuiasBackend.Models;
using GuiasBackend.Models.Common;

namespace GuiasBackend.Services.Interfaces
{
    public interface IUsuarioService
    {
        Task<PagedResponse<Usuario>> GetUsuariosAsync(int page = 1, int pageSize = 50, bool all = false, CancellationToken cancellationToken = default);
        Task<IEnumerable<Usuario>> GetAllUsuariosAsync(); 
        Task<Usuario?> GetUsuarioByIdAsync(int id);
        Task<Usuario?> GetUsuarioByUsernameAsync(string username);
        Task<Usuario?> GetUsuarioByEmailAsync(string email);
        Task<Usuario> CreateUsuarioAsync(Usuario usuario);
        Task<bool> UpdateUsuarioAsync(Usuario usuario);
        Task<bool> DeleteUsuarioAsync(int id);
        Task<bool> ExisteUsuarioAsync(string username);
        Task<bool> ExisteEmailAsync(string email);
        Task<bool> ValidarCredencialesAsync(string username, string password);
        Task<bool> CambiarContraseñaAsync(int id, string nuevaContraseña);
        Task<bool> ActualizarEstadoAsync(int id, string nuevoEstado);
        Task<bool> ExisteUsuarioByIdAsync(int id);
    }
}