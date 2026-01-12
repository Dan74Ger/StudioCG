using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudioCG.Web.Models.Fatturazione
{
    /// <summary>
    /// Bilanci CEE - solo per clienti SC (Società Capitali)
    /// Default scadenza 31/03, importo variabile per anno/cliente
    /// </summary>
    public class BilancioCEE
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
        [Display(Name = "Importo")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Importo { get; set; }

        [Required]
        [Display(Name = "Data Scadenza")]
        [DataType(DataType.Date)]
        public DateTime DataScadenza { get; set; } // Default 31/03

        // Scadenza a cui è associata per la fatturazione
        public int? ScadenzaFatturazioneId { get; set; }

        [ForeignKey("ScadenzaFatturazioneId")]
        public ScadenzaFatturazione? ScadenzaFatturazione { get; set; }

        [Display(Name = "Note")]
        [StringLength(200)]
        public string? Note { get; set; }

        [Display(Name = "Fatturazione Separata")]
        public bool FatturazioneSeparata { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}

