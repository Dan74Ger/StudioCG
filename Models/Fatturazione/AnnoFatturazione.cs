using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudioCG.Web.Models.Fatturazione
{
    /// <summary>
    /// Anno di Fatturazione - separato dall'Anno Fiscale delle Attività
    /// Gestisce la fatturazione indipendentemente dalle attività
    /// Nota: MandatoCliente e ScadenzaFatturazione usano int Anno (non FK)
    /// Questa tabella serve per gestire quali anni esistono e qual è il corrente
    /// </summary>
    public class AnnoFatturazione
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Anno")]
        public int Anno { get; set; }

        [Display(Name = "Anno Corrente")]
        public bool IsCurrent { get; set; }

        [Display(Name = "Attivo")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Note")]
        [StringLength(500)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // Computed: conteggi per statistiche (non mappati nel DB)
        [NotMapped]
        public int NumeroMandati { get; set; }

        [NotMapped]
        public decimal TotaleFatturato { get; set; }

        [NotMapped]
        public decimal TotaleIncassato { get; set; }
    }
}

