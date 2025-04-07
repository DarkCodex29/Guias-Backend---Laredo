using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GuiasBackend.Models
{
    [Table("VISTA_CAMPO", Schema = "PIMS_GRE")]
    public class VistaCampo
    {
        [Column("CAMPO")]
        [StringLength(6)]
        public string? Campo { get; set; }

        [Column("DESC_CAMPO")]
        [StringLength(70)]
        public string? DescCampo { get; set; }
    }
} 