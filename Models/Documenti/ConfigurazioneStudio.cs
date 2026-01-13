using System.ComponentModel.DataAnnotations;

namespace StudioCG.Web.Models.Documenti
{
    /// <summary>
    /// Configurazione dello studio professionale (logo, dati, firma)
    /// </summary>
    public class ConfigurazioneStudio
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Nome Studio")]
        public string NomeStudio { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "Indirizzo")]
        public string? Indirizzo { get; set; }

        [StringLength(100)]
        [Display(Name = "Città")]
        public string? Citta { get; set; }

        [StringLength(10)]
        [Display(Name = "CAP")]
        public string? CAP { get; set; }

        [StringLength(50)]
        [Display(Name = "Provincia")]
        public string? Provincia { get; set; }

        [StringLength(16)]
        [Display(Name = "Partita IVA")]
        public string? PIVA { get; set; }

        [StringLength(16)]
        [Display(Name = "Codice Fiscale")]
        public string? CF { get; set; }

        [StringLength(100)]
        [EmailAddress]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [StringLength(100)]
        [EmailAddress]
        [Display(Name = "PEC")]
        public string? PEC { get; set; }

        [StringLength(20)]
        [Display(Name = "Telefono")]
        public string? Telefono { get; set; }

        // Logo dello studio
        [Display(Name = "Logo")]
        public byte[]? Logo { get; set; }

        [StringLength(100)]
        public string? LogoContentType { get; set; }

        [StringLength(200)]
        public string? LogoFileName { get; set; }

        // Firma digitalizzata
        [Display(Name = "Firma")]
        public byte[]? Firma { get; set; }

        [StringLength(100)]
        public string? FirmaContentType { get; set; }

        [StringLength(200)]
        public string? FirmaFileName { get; set; }

        // Scadenza Antiriciclaggio
        [Display(Name = "Data Limite Antiriciclaggio")]
        [DataType(DataType.Date)]
        public DateTime? DataLimiteAntiriciclaggio { get; set; }

        /// <summary>
        /// Giorni dopo i quali un documento antiriciclaggio è considerato scaduto
        /// Default: 1095 giorni (36 mesi / 3 anni)
        /// </summary>
        [Display(Name = "Giorni Scadenza Antiriciclaggio")]
        public int GiorniScadenzaAntiriciclaggio { get; set; } = 1095;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}

