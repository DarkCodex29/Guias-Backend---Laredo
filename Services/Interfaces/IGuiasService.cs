using GuiasBackend.Models;
using GuiasBackend.Models.Common;

namespace GuiasBackend.Services.Interfaces
{
    public interface IGuiasService
    {
        Task<PagedResponse<Guia>> GetGuiasAsync(
            int page = 1, 
            int pageSize = 50, 
            bool all = false, 
            CancellationToken cancellationToken = default);
            
        Task<Guia?> GetGuiaByIdAsync(int id, CancellationToken cancellationToken = default);
        
        Task<Guia> CreateGuiaAsync(Guia guia, CancellationToken cancellationToken = default);
        
        Task<IEnumerable<Guia>> GetGuiasByUsuarioIdAsync(
            int idUsuario, 
            int page = 1, 
            int pageSize = 20, 
            bool all = false, 
            CancellationToken cancellationToken = default);
            
        Task<bool> ExisteGuiaAsync(string nombre, CancellationToken cancellationToken = default);
        
        Task<string> GenerarCorrelativoGuiaAsync(CancellationToken cancellationToken = default);
        
        Task<bool> DeleteGuiaAsync(int id, CancellationToken cancellationToken = default);
    }
}