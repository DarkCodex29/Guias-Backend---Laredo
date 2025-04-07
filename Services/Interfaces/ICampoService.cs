using GuiasBackend.Models;
using GuiasBackend.Models.Common;

namespace GuiasBackend.Services.Interfaces
{
    public interface ICampoService
    {
        Task<PagedResponse<VistaCampo>> GetAllCamposAsync(int page = 1, int pageSize = 50, bool all = false, CancellationToken cancellationToken = default);
        Task<VistaCampo?> GetCampoByCampoAsync(string campo);
        Task<VistaCampo?> GetCampoByDescripcionAsync(string descripcion);
        Task<bool> ExisteCampoAsync(string campo);
    }
}