using GuiasBackend.Models;
using GuiasBackend.Models.Common;

namespace GuiasBackend.Services.Interfaces
{
    public interface IEmpleadoService
    {
        Task<PagedResponse<VistaEmpleado>> GetAllEmpleadosAsync(int page = 1, int pageSize = 50, bool all = false, CancellationToken cancellationToken = default);
        Task<VistaEmpleado?> GetEmpleadoByDniAsync(string dni);
        Task<VistaEmpleado?> GetEmpleadoByEmpleadoAsync(string empleado);
        Task<bool> ExisteEmpleadoPorCodigoAsync(string codigo);
    }
}