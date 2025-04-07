using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GuiasBackend.Models
{
    [Table("PASSWORD_RESET")]
    public class PasswordReset
    {
        [Key]
        [Column("ID")]
        public int ID { get; set; }

        [Column("EMAIL")]
        [Required]
        [MaxLength(100)]
        public string EMAIL { get; set; } = string.Empty;

        [Column("CODIGO")]
        [Required]
        [MaxLength(6)]
        public string CODIGO { get; set; } = string.Empty;

        [Column("FECHA_CREACION")]
        [Required]
        public DateTime FECHA_CREACION { get; set; }

        [Column("FECHA_EXPIRACION")]
        [Required]
        public DateTime FECHA_EXPIRACION { get; set; }

        [Column("USADO")]
        [Required]
        public bool USADO { get; set; }
    }
} 