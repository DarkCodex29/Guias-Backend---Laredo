using Microsoft.AspNetCore.Http;

namespace GuiasBackend.DTOs
{
    public class GuiaCreateDto
    {
        public IFormFile Archivo { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public int IdUsuario { get; set; }
    }
} 