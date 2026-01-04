using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudioCG.Web.Models.AttivitaPeriodiche
{
    /// <summary>
    /// Associazione tra un cliente e un tipo di periodo di un'attività periodica
    /// Es: Cliente X iscritto a "LIPE Mensile" per anno 2026
    /// </summary>
    public class ClienteAttivitaPeriodica
    {
        public int Id { get; set; }

        [Required]
        public int AttivitaPeriodicaId { get; set; }

        [Required]
        public int TipoPeriodoId { get; set; }

        [Required]
        public int ClienteId { get; set; }

        [Required]
        [Display(Name = "Anno Fiscale")]
        public int AnnoFiscale { get; set; }

        /// <summary>
        /// Codice contabile o altro campo identificativo
        /// </summary>
        [StringLength(50)]
        [Display(Name = "Cod. COGE")]
        public string? CodCoge { get; set; }

        /// <summary>
        /// Percentuale interessi specifica per questo cliente (se diversa dal default)
        /// </summary>
        [Display(Name = "% Interessi")]
        public decimal? PercentualeInteressi { get; set; }

        [Display(Name = "Note")]
        public string? Note { get; set; }

        [Display(Name = "Attivo")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Data Creazione")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Ultima Modifica")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation
        [ForeignKey("AttivitaPeriodicaId")]
        public virtual AttivitaPeriodica? AttivitaPeriodica { get; set; }

        [ForeignKey("TipoPeriodoId")]
        public virtual TipoPeriodo? TipoPeriodo { get; set; }

        [ForeignKey("ClienteId")]
        public virtual Cliente? Cliente { get; set; }

        public virtual ICollection<ValorePeriodo> ValoriPeriodi { get; set; } = new List<ValorePeriodo>();
    }

    /// <summary>
    /// Valori dei campi per un singolo periodo di un cliente
    /// Es: Valori di Gennaio 2026 per Cliente X in LIPE Mensile
    /// </summary>
    public class ValorePeriodo
    {
        public int Id { get; set; }

        [Required]
        public int ClienteAttivitaPeriodicaId { get; set; }

        /// <summary>
        /// Numero del periodo (1-12 per mensili, 1-4 per trimestrali, ecc.)
        /// </summary>
        [Required]
        [Display(Name = "Numero Periodo")]
        public int NumeroPeriodo { get; set; }

        /// <summary>
        /// Valori dei campi in formato JSON
        /// Es: {"IMPORTO_CREDITO": "1000.50", "IMPORTO_DEBITO": "500.00", ...}
        /// </summary>
        [Display(Name = "Valori")]
        public string Valori { get; set; } = "{}";

        /// <summary>
        /// Valori calcolati salvati per storico (in JSON)
        /// Es: {"LIQUIDAZIONE": "500.50", "INTERESSI": "5.00", ...}
        /// </summary>
        [Display(Name = "Valori Calcolati")]
        public string ValoriCalcolati { get; set; } = "{}";

        [Display(Name = "Data Aggiornamento")]
        public DateTime? DataAggiornamento { get; set; }

        [Display(Name = "Note Periodo")]
        public string? Note { get; set; }

        // Navigation
        [ForeignKey("ClienteAttivitaPeriodicaId")]
        public virtual ClienteAttivitaPeriodica? ClienteAttivitaPeriodica { get; set; }
    }
}
