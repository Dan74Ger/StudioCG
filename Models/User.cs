using System.ComponentModel.DataAnnotations;

namespace StudioCG.Web.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Il nome utente è obbligatorio")]
        [StringLength(50, ErrorMessage = "Il nome utente non può superare i 50 caratteri")]
        [Display(Name = "Nome Utente")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "La password è obbligatoria")]
        [StringLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required(ErrorMessage = "Il nome è obbligatorio")]
        [StringLength(100)]
        [Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Il cognome è obbligatorio")]
        [StringLength(100)]
        [Display(Name = "Cognome")]
        public string Cognome { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Inserire un indirizzo email valido")]
        [StringLength(255)]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Display(Name = "Attivo")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "È Amministratore")]
        public bool IsAdmin { get; set; } = false;

        [Display(Name = "Data Creazione")]
        public DateTime DataCreazione { get; set; } = DateTime.Now;

        [Display(Name = "Ultimo Accesso")]
        public DateTime? UltimoAccesso { get; set; }

        // Navigation property
        public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();

        // Computed property
        [Display(Name = "Nome Completo")]
        public string NomeCompleto => $"{Nome} {Cognome}";
    }
}


