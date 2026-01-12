using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudioCG.Web.Models.Fatturazione
{
    /// <summary>
    /// Spese pratiche mensili per cliente
    /// Addebitate a una specifica scadenza
    /// </summary>
    public class SpesaPratica
    {
        public int Id { get; set; }

        [Required]
        public int ClienteId { get; set; }

        [ForeignKey("ClienteId")]
        public Cliente? Cliente { get; set; }

        [Display(Name = "Scadenza Destinazione")]
        public int? ScadenzaFatturazioneId { get; set; }

        [ForeignKey("ScadenzaFatturazioneId")]
        public ScadenzaFatturazione? ScadenzaFatturazione { get; set; }

        // Utente che ha inserito la spesa
        public int? UtenteId { get; set; }

        [ForeignKey("UtenteId")]
        public User? Utente { get; set; }

        [Required]
        [Display(Name = "Descrizione")]
        [StringLength(200)]
        public string Descrizione { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Importo")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Importo { get; set; }

        [Required]
        [Display(Name = "Data")]
        [DataType(DataType.Date)]
        public DateTime Data { get; set; } = DateTime.Today;

        [Display(Name = "Fatturazione Separata")]
        public bool FatturazioneSeparata { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}

