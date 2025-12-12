using System.ComponentModel.DataAnnotations;

namespace StudioCG.Web.Models
{
    public class UserPermission
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int PermissionId { get; set; }

        [Display(Name = "Può Visualizzare")]
        public bool CanView { get; set; } = true;

        [Display(Name = "Può Modificare")]
        public bool CanEdit { get; set; } = false;

        [Display(Name = "Può Eliminare")]
        public bool CanDelete { get; set; } = false;

        [Display(Name = "Può Creare")]
        public bool CanCreate { get; set; } = false;

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual Permission Permission { get; set; } = null!;
    }
}


