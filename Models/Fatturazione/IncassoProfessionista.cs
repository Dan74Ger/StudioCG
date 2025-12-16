using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudioCG.Web.Models.Fatturazione
{
    /// <summary>
    /// Suddivisione di un incasso tra professionisti
    /// Ogni incasso può essere suddiviso con percentuali variabili
    /// </summary>
    public class IncassoProfessionista
    {
        public int Id { get; set; }

        [Required]
        public int IncassoFatturaId { get; set; }

        [ForeignKey("IncassoFatturaId")]
        public IncassoFattura? IncassoFattura { get; set; }

        [Required]
        public int UtenteId { get; set; }

        [ForeignKey("UtenteId")]
        public User? Utente { get; set; }

        [Required]
        [Display(Name = "Percentuale")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal Percentuale { get; set; }

        [Required]
        [Display(Name = "Importo")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Importo { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

