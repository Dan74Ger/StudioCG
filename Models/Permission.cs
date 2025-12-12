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

        // Navigation property
        public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
    }
}


