using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GuiasBackend.Models
{
    [Table("VISTA_EMPLEADO", Schema = "PIMS_GRE")]
    public class VistaEmpleado
    {
        [Column("CODIGO")]
        public int Codigo { get; set; }

        [Column("EMPLEADO")]
        [StringLength(40)]
        public string? Empleado { get; set; }

        [Column("DNI")]
        [StringLength(20)]
        public string? Dni { get; set; }

        [Column("CD_TRANSP")]
        public int CdTransp { get; set; }
    }
}