using Microsoft.Extensions.Diagnostics.HealthChecks;
using GuiasBackend.Data;
using Microsoft.EntityFrameworkCore;

namespace GuiasBackend.Configuration.HealthChecks
{
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly ApplicationDbContext _context;

        public DatabaseHealthCheck(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // Verifica si la base de datos responde
                var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
                
                if (canConnect)
                {
                    return HealthCheckResult.Healthy("La base de datos está respondiendo normalmente.");
                }

                return HealthCheckResult.Unhealthy("No se puede conectar a la base de datos.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Error al verificar la base de datos.", ex);
            }
        }
    }
} 