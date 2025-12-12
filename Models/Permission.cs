using System.ComponentModel.DataAnnotations;

namespace StudioCG.Web.Models
{
    public class Permission
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Nome Pagina")]
        public string PageName { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        [Display(Name = "URL Pagina")]
        public string PageUrl { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Descrizione")]
        public string? Description { get; set; }

        [Display(Name = "Icona")]
        [StringLength(100)]
        public string? Icon { get; set; }

        [Display(Name = "Ordine")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Visibile nel Menu")]
        public bool ShowInMenu { get; set; } = true;

        /// <summary>
        /// Categoria per organizzare le pagine nel menu
        /// Valori: "DatiRiservati", "DatiGenerali", "Admin", null (root)
        /// </summary>
        [StringLength(50)]
        [Display(Name = "Categoria")]
        public string? Category { get; set; }

        // Navigation property
        public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
    }

    /// <summary>
    /// Categorie disponibili per le pagine
    /// </summary>
    public static class PermissionCategories
    {
        public const string DatiRiservati = "DatiRiservati";
        public const string DatiGenerali = "DatiGenerali";
        public const string Admin = "Admin";
    }
}
