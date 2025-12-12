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

            // Seed admin user (password: 123456)
            // Hash generato con SHA256
            modelBuilder.Entity<User>().HasData(new User
            {
                Id = 1,
                Username = "admin",
                PasswordHash = "8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92", // SHA256 di "123456"
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
                }
            );
        }
    }
}


