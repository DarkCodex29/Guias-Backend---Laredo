using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GuiasBackend.Models
{
    [Table("VISTA_TRANSPORTISTA", Schema = "PIMS_GRE")]
    public class VistaTransportista
    {
        [Key]
        [Column("COD_TRANSP")]
        public int CodTransp { get; set; }

        [Column("TRANSPORTISTA")]
        [StringLength(40)]
        public string? Transportista { get; set; }

        [Column("RUC")]
        [StringLength(18)]
        public string? Ruc { get; set; }
    }
} 