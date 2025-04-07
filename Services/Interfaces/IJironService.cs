using GuiasBackend.Models;
using GuiasBackend.Models.Common;

namespace GuiasBackend.Services.Interfaces
{
    public interface IJironService
    {
        Task<PagedResponse<VistaJiron>> GetAllJironesAsync(int page = 1, int pageSize = 50, bool all = false, CancellationToken cancellationToken = default);
        Task<VistaJiron?> GetJironByJironAsync(string jiron);
        Task<PagedResponse<VistaJiron>> GetJironesByCampoAsync(string campo, int page = 1, int pageSize = 50, bool all = false, CancellationToken cancellationToken = default);
    }
}