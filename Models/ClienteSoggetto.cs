using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudioCG.Web.Models
{
    public enum TipoSoggetto
    {
        [Display(Name = "Legale Rappresentante")]
        LegaleRappresentante = 0,

        [Display(Name = "Consigliere")]
        Consigliere = 1,

        [Display(Name = "Socio")]
        Socio = 2
    }

    public class ClienteSoggetto
    {
        public int Id { get; set; }

        [Required]
        public int ClienteId { get; set; }

        [Required]
        [Display(Name = "Tipo")]
        public TipoSoggetto TipoSoggetto { get; set; }

        [Required(ErrorMessage = "Il Nome è obbligatorio")]
        [StringLength(100)]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Il Cognome è obbligatorio")]
        [StringLength(100)]
        public string Cognome { get; set; } = string.Empty;

        [StringLength(16)]
        [Display(Name = "Codice Fiscale")]
        public string? CodiceFiscale { get; set; }

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
        [EmailAddress]
        public string? Email { get; set; }

        [StringLength(20)]
        public string? Telefono { get; set; }

        // Solo per Soci - Quota capitale in euro o percentuale
        [Display(Name = "Quota")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? QuotaPercentuale { get; set; }

        [Display(Name = "Ordine")]
        public int DisplayOrder { get; set; } = 0;

        // Navigation property
        [ForeignKey("ClienteId")]
        public virtual Cliente? Cliente { get; set; }

        // Helper property
        [NotMapped]
        public string NomeCompleto => $"{Cognome} {Nome}";
    }
}

