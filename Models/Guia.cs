using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace GuiasBackend.Models
{
    [Table("GUIAS", Schema = "PIMS_GRE")]
    public class Guia
    {
        [Key]
        [Column("ID")]
        public int ID { get; set; }

        [Required]
        [Column("NOMBRE")]
        [StringLength(50)]
        public string NOMBRE { get; set; } = string.Empty;

        [Column("ARCHIVO", TypeName = "BLOB")]
        public byte[]? ARCHIVO { get; set; }

        [Required]
        [Column("FECHA_SUBIDA")]
        public DateTime FECHA_SUBIDA { get; set; } = DateTime.UtcNow;

        [Required]
        [Column("ID_USUARIO")]
        public int ID_USUARIO { get; set; }

        [JsonIgnore]
        [ForeignKey("ID_USUARIO")]
        public virtual Usuario? Usuario { get; set; }
        
        [NotMapped]
        public string UsernameUsuario 
        { 
            get => Usuario?.USERNAME ?? string.Empty;
            set => _ = value; // Solo para serialización
        }
    }
}