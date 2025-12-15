using Microsoft.EntityFrameworkCore;
using StudioCG.Web.Models;

namespace StudioCG.Web.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<UserPermission> UserPermissions { get; set; }
        
        // Tabelle per sistema dinamico
        public DbSet<DynamicPage> DynamicPages { get; set; }
        public DbSet<DynamicField> DynamicFields { get; set; }
        public DbSet<DynamicRecord> DynamicRecords { get; set; }
        public DbSet<DynamicFieldValue> DynamicFieldValues { get; set; }

        // Tabelle Anagrafica
        public DbSet<Cliente> Clienti { get; set; }
        public DbSet<ClienteSoggetto> ClientiSoggetti { get; set; }
        public DbSet<AnnualitaFiscale> AnnualitaFiscali { get; set; }

        // Tabelle Attività
        public DbSet<AttivitaTipo> AttivitaTipi { get; set; }
        public DbSet<AttivitaCampo> AttivitaCampi { get; set; }
        public DbSet<AttivitaAnnuale> AttivitaAnnuali { get; set; }
        public DbSet<ClienteAttivita> ClientiAttivita { get; set; }
        public DbSet<ClienteAttivitaValore> ClientiAttivitaValori { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Cognome).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).HasMaxLength(255);
            });

            // Permission configuration
            modelBuilder.Entity<Permission>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.PageUrl).IsUnique();
                entity.Property(e => e.PageName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PageUrl).IsRequired().HasMaxLength(255);
            });

            // UserPermission configuration
            modelBuilder.Entity<UserPermission>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.UserId, e.PermissionId }).IsUnique();

                entity.HasOne(e => e.User)
                    .WithMany(u => u.UserPermissions)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Permission)
                    .WithMany(p => p.UserPermissions)
                    .HasForeignKey(e => e.PermissionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // DynamicPage configuration
            modelBuilder.Entity<DynamicPage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.TableName).IsUnique();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
                entity.Property(e => e.TableName).IsRequired().HasMaxLength(100);
            });

            // DynamicField configuration
            modelBuilder.Entity<DynamicField>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Label).IsRequired().HasMaxLength(100);
                entity.Property(e => e.FieldType).IsRequired().HasMaxLength(50);

                entity.HasOne(e => e.DynamicPage)
                    .WithMany(p => p.Fields)
                    .HasForeignKey(e => e.DynamicPageId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // DynamicRecord configuration
            modelBuilder.Entity<DynamicRecord>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.DynamicPage)
                    .WithMany(p => p.Records)
                    .HasForeignKey(e => e.DynamicPageId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // DynamicFieldValue configuration
            modelBuilder.Entity<DynamicFieldValue>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.DynamicRecordId, e.DynamicFieldId }).IsUnique();

                entity.HasOne(e => e.DynamicRecord)
                    .WithMany(r => r.FieldValues)
                    .HasForeignKey(e => e.DynamicRecordId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.DynamicField)
                    .WithMany(f => f.FieldValues)
                    .HasForeignKey(e => e.DynamicFieldId)
                    .OnDelete(DeleteBehavior.NoAction); // Evita cicli di cascade
            });

            // ============ NUOVE CONFIGURAZIONI ============

            // Cliente configuration
            modelBuilder.Entity<Cliente>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.RagioneSociale).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Indirizzo).HasMaxLength(200);
                entity.Property(e => e.Citta).HasMaxLength(100);
                entity.Property(e => e.Provincia).HasMaxLength(2);
                entity.Property(e => e.CAP).HasMaxLength(5);
                entity.Property(e => e.Email).HasMaxLength(255);
                entity.Property(e => e.PEC).HasMaxLength(50);
                entity.Property(e => e.Telefono).HasMaxLength(20);
                entity.Property(e => e.CodiceFiscale).HasMaxLength(16);
                entity.Property(e => e.PartitaIVA).HasMaxLength(11);
            });

            // ClienteSoggetto configuration
            modelBuilder.Entity<ClienteSoggetto>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Cognome).IsRequired().HasMaxLength(100);
                entity.Property(e => e.CodiceFiscale).HasMaxLength(16);
                entity.Property(e => e.Indirizzo).HasMaxLength(200);
                entity.Property(e => e.Citta).HasMaxLength(100);
                entity.Property(e => e.Provincia).HasMaxLength(2);
                entity.Property(e => e.CAP).HasMaxLength(5);
                entity.Property(e => e.Email).HasMaxLength(255);
                entity.Property(e => e.Telefono).HasMaxLength(20);
                entity.Property(e => e.QuotaPercentuale).HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.Cliente)
                    .WithMany(c => c.Soggetti)
                    .HasForeignKey(e => e.ClienteId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // AnnualitaFiscale configuration
            modelBuilder.Entity<AnnualitaFiscale>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Anno).IsUnique();
                entity.Property(e => e.Anno).IsRequired();
                entity.Property(e => e.Descrizione).HasMaxLength(100);
            });

            // AttivitaTipo configuration
            modelBuilder.Entity<AttivitaTipo>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Descrizione).HasMaxLength(500);
                entity.Property(e => e.Icon).HasMaxLength(50);
            });

            // AttivitaCampo configuration
            modelBuilder.Entity<AttivitaCampo>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Label).IsRequired().HasMaxLength(100);

                entity.HasOne(e => e.AttivitaTipo)
                    .WithMany(t => t.Campi)
                    .HasForeignKey(e => e.AttivitaTipoId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // AttivitaAnnuale configuration
            modelBuilder.Entity<AttivitaAnnuale>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.AttivitaTipoId, e.AnnualitaFiscaleId }).IsUnique();

                entity.HasOne(e => e.AttivitaTipo)
                    .WithMany(t => t.AttivitaAnnuali)
                    .HasForeignKey(e => e.AttivitaTipoId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.AnnualitaFiscale)
                    .WithMany(a => a.AttivitaAnnuali)
                    .HasForeignKey(e => e.AnnualitaFiscaleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ClienteAttivita configuration
            modelBuilder.Entity<ClienteAttivita>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.ClienteId, e.AttivitaAnnualeId }).IsUnique();

                entity.HasOne(e => e.Cliente)
                    .WithMany(c => c.Attivita)
                    .HasForeignKey(e => e.ClienteId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.AttivitaAnnuale)
                    .WithMany(a => a.ClientiAttivita)
                    .HasForeignKey(e => e.AttivitaAnnualeId)
                    .OnDelete(DeleteBehavior.NoAction); // Evita cicli
            });

            // ClienteAttivitaValore configuration
            modelBuilder.Entity<ClienteAttivitaValore>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.ClienteAttivitaId, e.AttivitaCampoId }).IsUnique();

                entity.HasOne(e => e.ClienteAttivita)
                    .WithMany(ca => ca.Valori)
                    .HasForeignKey(e => e.ClienteAttivitaId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.AttivitaCampo)
                    .WithMany()
                    .HasForeignKey(e => e.AttivitaCampoId)
                    .OnDelete(DeleteBehavior.NoAction); // Evita cicli
            });

            // ============ SEED DATA ============

            // Seed admin user (password: 123456)
            modelBuilder.Entity<User>().HasData(new User
            {
                Id = 1,
                Username = "admin",
                PasswordHash = "8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92",
                Nome = "Amministratore",
                Cognome = "Sistema",
                Email = "admin@studiocg.it",
                IsActive = true,
                IsAdmin = true,
                DataCreazione = new DateTime(2024, 1, 1)
            });

            // Seed default permissions
            modelBuilder.Entity<Permission>().HasData(
                new Permission
                {
                    Id = 1,
                    PageName = "Dashboard",
                    PageUrl = "/Home",
                    Description = "Pagina principale",
                    Icon = "fas fa-home",
                    DisplayOrder = 1,
                    ShowInMenu = true
                },
                new Permission
                {
                    Id = 2,
                    PageName = "Gestione Utenti",
                    PageUrl = "/Users",
                    Description = "Gestione utenti del sistema",
                    Icon = "fas fa-users",
                    DisplayOrder = 2,
                    ShowInMenu = true
                },
                // Nuovi permessi per Anagrafica (ID alti per evitare conflitti)
                new Permission
                {
                    Id = 100,
                    PageName = "Clienti",
                    PageUrl = "/Clienti",
                    Description = "Gestione anagrafica clienti",
                    Icon = "fas fa-building",
                    Category = "ANAGRAFICA",
                    DisplayOrder = 10,
                    ShowInMenu = true
                },
                new Permission
                {
                    Id = 101,
                    PageName = "Annualità Fiscali",
                    PageUrl = "/Annualita",
                    Description = "Gestione annualità fiscali",
                    Icon = "fas fa-calendar-alt",
                    Category = "ANAGRAFICA",
                    DisplayOrder = 11,
                    ShowInMenu = true
                },
                new Permission
                {
                    Id = 102,
                    PageName = "Tipi Attività",
                    PageUrl = "/AttivitaTipi",
                    Description = "Gestione tipi attività",
                    Icon = "fas fa-cogs",
                    Category = "ANAGRAFICA",
                    DisplayOrder = 12,
                    ShowInMenu = true
                }
            );

            // Seed anno fiscale corrente
            modelBuilder.Entity<AnnualitaFiscale>().HasData(new AnnualitaFiscale
            {
                Id = 1,
                Anno = 2025,
                Descrizione = "Anno Fiscale 2025",
                IsActive = true,
                IsCurrent = true,
                CreatedAt = new DateTime(2025, 1, 1)
            });
        }
    }
}
