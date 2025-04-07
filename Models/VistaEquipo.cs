using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GuiasBackend.Models
{
    [Table("VISTA_EQUIPOS", Schema = "PIMS_GRE")]
    public class VistaEquipo
    {
        [Column("COD_EQUIPO")]
        public int Codigo { get; set; }

        [Column("PLACA")]
        public string? Placa { get; set; }

        [Column("COD_TRANSP")]
        public int CodTransp { get; set; }

        [Column("TIP_EQUIPO")]
        public string? TipoEquipo { get; set; }
    }
}