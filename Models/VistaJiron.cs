using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GuiasBackend.Models
{
    [Table("VISTA_JIRON", Schema = "PIMS_GRE")]
    public class VistaJiron
    {
        [Column("CAMPO")]
        [StringLength(6)]
        public string? Campo { get; set; }

        [Column("JIRON")]
        [StringLength(8)]
        public string? Jiron { get; set; }
    }
} 