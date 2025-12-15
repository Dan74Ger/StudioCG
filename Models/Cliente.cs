using System.ComponentModel.DataAnnotations;

namespace StudioCG.Web.Models
{
    public class Cliente
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "La Ragione Sociale è obbligatoria")]
        [StringLength(200)]
        [Display(Name = "Ragione Sociale")]
        public string RagioneSociale { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Indirizzo { get; set; }

        [StringLength(100)]
        [Display(Name = "Città")]
        public string? Citta { get; set; }

        [StringLength(2)]
        public string? Provincia { get; set; }

        [StringLength(5)]
        public string? CAP { get; set; }

        [StringLength(255)]
        [EmailAddress(ErrorMessage = "Email non valida")]
        public string? Email { get; set; }

        [StringLength(50)]
        public string? PEC { get; set; }

        [StringLength(20)]
        public string? Telefono { get; set; }

        [StringLength(16)]
        [Display(Name = "Codice Fiscale")]
        public string? CodiceFiscale { get; set; }

        [StringLength(11)]
        [Display(Name = "Partita IVA")]
        public string? PartitaIVA { get; set; }

        [StringLength(10)]
        [Display(Name = "Codice Ateco")]
        public string? CodiceAteco { get; set; }

        [Display(Name = "Attivo")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Data Creazione")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string? Note { get; set; }

        // Navigation properties
        public virtual ICollection<ClienteSoggetto> Soggetti { get; set; } = new List<ClienteSoggetto>();
        public virtual ICollection<ClienteAttivita> Attivita { get; set; } = new List<ClienteAttivita>();
    }
}

