using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GuiasBackend.Models
{
    [Table("USUARIO", Schema = "PIMS_GRE")]
    public class Usuario
    {
        [Key]
        [Column("ID")]
        public int ID { get; set; }

        [StringLength(50)]
        [Column("USERNAME")]
        public string? USERNAME { get; set; } = string.Empty;

        [StringLength(100)]
        [Column("NOMBRES")]
        public string? NOMBRES { get; set; } = string.Empty;

        [StringLength(100)]
        [Column("APELLIDOS")]
        public string? APELLIDOS { get; set; } = string.Empty;

        [StringLength(225)]
        [Column("CONTRASEÑA")]
        public string? CONTRASEÑA { get; set; } = string.Empty;

        [StringLength(50)]
        [Column("ROL")]
        public string? ROL { get; set; } = string.Empty;

        [StringLength(100)]
        [Column("EMAIL")]
        public string? EMAIL { get; set; }

        [StringLength(1)]
        [Column("ESTADO")]
        public string? ESTADO { get; set; } = "1";

        [Column("FECHA_CREACION")]
        public DateTime FECHA_CREACION { get; set; } = DateTime.Now;

        [Column("FECHA_ACTUALIZACION")]
        public DateTime? FECHA_ACTUALIZACION { get; set; }

        public virtual ICollection<Guia> Guias { get; set; } = new List<Guia>();
    }
}