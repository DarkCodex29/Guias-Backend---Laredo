using GuiasBackend.Models;
using GuiasBackend.Models.Common;

namespace GuiasBackend.Services.Interfaces
{
    public interface ICuartelService
    {
        Task<PagedResponse<VistaCuartel>> GetAllCuartelesAsync(int page = 1, int pageSize = 50, bool all = false, CancellationToken cancellationToken = default);
        Task<VistaCuartel?> GetCuartelByCuartelAsync(string cuartel);
        Task<PagedResponse<VistaCuartel>> GetCuartelesByCampoAsync(string campo, int page = 1, int pageSize = 50, bool all = false, CancellationToken cancellationToken = default);
    }
}