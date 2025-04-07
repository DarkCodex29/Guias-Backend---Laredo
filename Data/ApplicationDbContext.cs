using Microsoft.EntityFrameworkCore;
using GuiasBackend.Models;

namespace GuiasBackend.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Guia> Guias { get; set; }
        public DbSet<VistaEquipo> VistaEquipos { get; set; }
        public DbSet<VistaCuartel> VistaCuarteles { get; set; }
        public DbSet<VistaEmpleado> VistaEmpleados { get; set; }
        public DbSet<VistaJiron> VistaJirones { get; set; }
        public DbSet<VistaTransportista> VistaTransportistas { get; set; }
        public DbSet<PasswordReset> PasswordResets { get; set; }

        // Vistas
        public DbSet<VistaCampo> VistaCampos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.ToTable("USUARIO");
                entity.HasKey(e => e.ID);

                entity.Property(e => e.ID)
                    .HasColumnName("ID")
                    .IsRequired();

                entity.Property(e => e.USERNAME)
                    .HasColumnName("USERNAME")
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.CONTRASEÑA)
                    .HasColumnName("CONTRASEÑA")
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.NOMBRES)
                    .HasColumnName("NOMBRES")
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.APELLIDOS)
                    .HasColumnName("APELLIDOS")
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.ROL)
                    .HasColumnName("ROL")
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.EMAIL)
                    .HasColumnName("EMAIL")
                    .HasMaxLength(100);

                entity.Property(e => e.ESTADO)
                    .HasColumnName("ESTADO")
                    .HasMaxLength(1)
                    .HasDefaultValue("1");

                entity.Property(e => e.FECHA_CREACION)
                    .HasColumnName("FECHA_CREACION")
                    .IsRequired();

                entity.Property(e => e.FECHA_ACTUALIZACION)
                    .HasColumnName("FECHA_ACTUALIZACION");

                // Configurar índices únicos
                entity.HasIndex(e => e.USERNAME).IsUnique();
                entity.HasIndex(e => e.EMAIL).IsUnique();
            });

            modelBuilder.Entity<Guia>(entity =>
            {
                entity.ToTable("GUIAS", schema: "PIMS_GRE");
                entity.HasKey(e => e.ID);

                entity.Property(e => e.ID)
                    .HasColumnName("ID")
                    .IsRequired();

                entity.Property(e => e.NOMBRE)
                    .HasColumnName("NOMBRE")
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.ARCHIVO)
                    .HasColumnName("ARCHIVO");

                entity.Property(e => e.FECHA_SUBIDA)
                    .HasColumnName("FECHA_SUBIDA")
                    .IsRequired();

                entity.Property(e => e.ID_USUARIO)
                    .HasColumnName("ID_USUARIO")
                    .IsRequired();

                // Configurar la relación con Usuario
                entity.HasOne(g => g.Usuario)
                    .WithMany(u => u.Guias)
                    .HasForeignKey(g => g.ID_USUARIO)
                    .OnDelete(DeleteBehavior.Restrict);

                // Configurar índices únicos
                entity.HasIndex(e => e.NOMBRE).IsUnique();
            });

            modelBuilder.Entity<PasswordReset>(entity =>
            {
                entity.ToTable("PASSWORD_RESET");
                entity.HasKey(e => e.ID);

                entity.Property(e => e.ID)
                    .HasColumnName("ID")
                    .IsRequired();

                entity.Property(e => e.EMAIL)
                    .HasColumnName("EMAIL")
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.CODIGO)
                    .HasColumnName("CODIGO")
                    .IsRequired()
                    .HasMaxLength(6);

                entity.Property(e => e.FECHA_CREACION)
                    .HasColumnName("FECHA_CREACION")
                    .IsRequired();

                entity.Property(e => e.FECHA_EXPIRACION)
                    .HasColumnName("FECHA_EXPIRACION")
                    .IsRequired();

                entity.Property(e => e.USADO)
                    .HasColumnName("USADO")
                    .IsRequired();

                // Índice para búsquedas rápidas por email
                entity.HasIndex(e => e.EMAIL);
            });

            // Configuración de las vistas
            modelBuilder.Entity<VistaCampo>().ToView("VISTA_CAMPO").HasNoKey();
            modelBuilder.Entity<VistaCuartel>().ToView("VISTA_CUARTEL").HasNoKey();
            modelBuilder.Entity<VistaJiron>().ToView("VISTA_JIRON").HasNoKey();
            modelBuilder.Entity<VistaEmpleado>().ToView("VISTA_EMPLEADO").HasNoKey();

            modelBuilder.Entity<VistaEquipo>(entity =>
            {
                entity.ToView("VISTA_EQUIPOS").HasNoKey();
                entity.Property(e => e.Codigo).HasColumnName("COD_EQUIPO");
                entity.Property(e => e.Placa).HasColumnName("PLACA");
                entity.Property(e => e.CodTransp).HasColumnName("COD_TRANSP");
                entity.Property(e => e.TipoEquipo).HasColumnName("TIP_EQUIPO");
            });

            modelBuilder.Entity<VistaTransportista>().ToView("VISTA_TRANSPORTISTA").HasNoKey();

            // Configurar ordenamiento predeterminado para Usuarios
            modelBuilder.Entity<Usuario>()
                .HasQueryFilter(u => u.ESTADO == "1");
        }
    }
}