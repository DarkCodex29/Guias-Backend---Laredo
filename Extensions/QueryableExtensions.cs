using GuiasBackend.Models.Common;
using Microsoft.EntityFrameworkCore;

namespace GuiasBackend.Extensions
{
    public static class QueryableExtensions
    {
        /// <summary>
        /// Crea una respuesta paginada a partir de un IQueryable
        /// </summary>
        public static async Task<PagedResponse<T>> ToPagedResponseAsync<T>(
            this IQueryable<T> query, 
            int page, 
            int pageSize, 
            CancellationToken cancellationToken = default)
        {
            var totalCount = await query.CountAsync(cancellationToken);
            
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
                
            return new PagedResponse<T>(items, page, pageSize, totalCount);
        }
    }
}
