using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudioCG.Web.Models.Fatturazione
{
    /// <summary>
    /// Incasso di una fattura
    /// Gestito nella pagina INCASSI (separata da Fatturazione)
    /// Supporta incassi parziali (più incassi per una fattura)
    /// </summary>
    public class IncassoFattura
    {
        public int Id { get; set; }

        [Required]
        public int ScadenzaFatturazioneId { get; set; }

        [ForeignKey("ScadenzaFatturazioneId")]
        public ScadenzaFatturazione? ScadenzaFatturazione { get; set; }

        [Required]
        [Display(Name = "Data Incasso")]
        [DataType(DataType.Date)]
        public DateTime DataIncasso { get; set; } = DateTime.Today;

        [Required]
        [Display(Name = "Importo Incassato")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ImportoIncassato { get; set; }

        [Display(Name = "Note")]
        [StringLength(500)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // Navigation - suddivisione tra professionisti
        public virtual ICollection<IncassoProfessionista> SuddivisioneProfessionisti { get; set; } = new List<IncassoProfessionista>();

        // Computed
        [NotMapped]
        public decimal TotaleSuddiviso => SuddivisioneProfessionisti?.Sum(s => s.Importo) ?? 0;

        [NotMapped]
        public bool SuddivisioneCompleta => Math.Abs(TotaleSuddiviso - ImportoIncassato) < 0.01m;
    }
}

