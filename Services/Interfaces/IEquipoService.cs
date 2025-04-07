using GuiasBackend.Models;
using GuiasBackend.Models.Common;

namespace GuiasBackend.Services.Interfaces
{
    public interface IEquipoService
    {
        Task<PagedResponse<VistaEquipo>> GetAllEquiposAsync(int page = 1, int pageSize = 50, bool all = false, CancellationToken cancellationToken = default);
        Task<VistaEquipo?> GetEquipoByCodEquipoAsync(int codEquipo);
        Task<VistaEquipo?> GetEquipoByPlacaAsync(string placa, CancellationToken cancellationToken = default);
        Task<IEnumerable<VistaEquipo>> GetEquiposByCodTranspAsync(int codTransp);
    }
}