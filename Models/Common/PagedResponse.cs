namespace GuiasBackend.Models.Common
{
    /// <summary>
    /// Modelo genérico para respuestas paginadas
    /// </summary>
    /// <typeparam name="T">Tipo de datos en la colección</typeparam>
    public class PagedResponse<T>
    {
        /// <summary>
        /// Número de página actual
        /// </summary>
        public int Page { get; set; }
        
        /// <summary>
        /// Tamaño de página
        /// </summary>
        public int PageSize { get; set; }
        
        /// <summary>
        /// Total de registros
        /// </summary>
        public int TotalCount { get; set; }
        
        /// <summary>
        /// Total de páginas
        /// </summary>
        public int TotalPages { get; set; }
        
        /// <summary>
        /// ¿Hay una página anterior?
        /// </summary>
        public bool HasPrevious { get; set; }
        
        /// <summary>
        /// ¿Hay una página siguiente?
        /// </summary>
        public bool HasNext { get; set; }
        
        /// <summary>
        /// Colección de datos
        /// </summary>
        public IEnumerable<T> Data { get; set; } = Enumerable.Empty<T>();
        
        /// <summary>
        /// Constructor
        /// </summary>
        public PagedResponse() { }

        public PagedResponse(IEnumerable<T> data, int page, int pageSize, int totalCount)
        {
            Page = page;
            PageSize = pageSize;
            TotalCount = totalCount;
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            HasPrevious = page > 1;
            HasNext = page < TotalPages;
            Data = data;
        }
    }
}
