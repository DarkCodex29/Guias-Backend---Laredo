using GuiasBackend.Models;
using GuiasBackend.Models.Common;

namespace GuiasBackend.Services.Interfaces
{
    public interface ITransportistaService
    {
        Task<PagedResponse<VistaTransportista>> GetAllTransportistasAsync(int page = 1, int pageSize = 50, bool all = false, CancellationToken cancellationToken = default);
        Task<VistaTransportista?> GetTransportistaByCodTranspAsync(int codTransp);
        Task<VistaTransportista?> GetTransportistaByRucAsync(string ruc);
    }
}