using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudioCG.Web.Models.Fatturazione
{
    /// <summary>
    /// Mandato professionale per cliente
    /// Contiene l'importo annuo e il tipo di scadenza
    /// </summary>
    public class MandatoCliente
    {
        public int Id { get; set; }

        [Required]
        public int ClienteId { get; set; }

        [ForeignKey("ClienteId")]
        public Cliente? Cliente { get; set; }

        [Required]
        [Display(Name = "Anno")]
        public int Anno { get; set; }

        [Required]
        [Display(Name = "Importo Annuo")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ImportoAnnuo { get; set; }

        [Display(Name = "Rimborso Spese")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal RimborsoSpese { get; set; } = 0;

        [Required]
        [Display(Name = "Tipo Scadenza")]
        public TipoScadenzaMandato TipoScadenza { get; set; }

        [Display(Name = "Attivo")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Note")]
        [StringLength(500)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public virtual ICollection<ScadenzaFatturazione> Scadenze { get; set; } = new List<ScadenzaFatturazione>();

        // Computed properties
        [NotMapped]
        public int NumeroRate => TipoScadenza switch
        {
            TipoScadenzaMandato.Mensile => 12,
            TipoScadenzaMandato.Bimestrale => 6,
            TipoScadenzaMandato.Trimestrale => 4,
            TipoScadenzaMandato.Semestrale => 2,
            TipoScadenzaMandato.Annuale => 1,
            _ => 1
        };

        [NotMapped]
        public decimal ImportoRata => NumeroRate > 0 ? Math.Round(ImportoAnnuo / NumeroRate, 2) : 0;
    }
}

