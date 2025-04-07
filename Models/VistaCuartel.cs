using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GuiasBackend.Models
{
    [Table("VISTA_CUARTEL", Schema = "PIMS_GRE")]
    public class VistaCuartel
    {
        [Column("CAMPO")]
        [StringLength(6)]
        public string? Campo { get; set; }

        [Column("JIRON")]
        [StringLength(8)]
        public string? Jiron { get; set; }

        [Column("CUARTEL")]
        [StringLength(6)]
        public string? Cuartel { get; set; }
    }
} 