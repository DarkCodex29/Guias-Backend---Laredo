namespace GuiasBackend.Models.BulkUpload
{
    public class BulkUploadResult
    {
        public int TotalRegistros { get; set; }
        public int RegistrosExitosos { get; set; }
        public int RegistrosFallidos { get; set; }
        public List<string> Errores { get; set; } = [];  // Simplificando la inicialización
    }
}
