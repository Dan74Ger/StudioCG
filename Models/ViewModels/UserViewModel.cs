using System.ComponentModel.DataAnnotations;

namespace StudioCG.Web.Models.ViewModels
{
    public class UserViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Il nome utente è obbligatorio")]
        [StringLength(50, ErrorMessage = "Il nome utente non può superare i 50 caratteri")]
        [Display(Name = "Nome Utente")]
        public string Username { get; set; } = string.Empty;

        [StringLength(100, MinimumLength = 6, ErrorMessage = "La password deve essere tra 6 e 100 caratteri")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Conferma Password")]
        [Compare("Password", ErrorMessage = "Le password non coincidono")]
        public string? ConfirmPassword { get; set; }

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
    }
}


