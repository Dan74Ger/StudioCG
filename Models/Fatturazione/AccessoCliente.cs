using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudioCG.Web.Models.Fatturazione
{
    /// <summary>
    /// Accesso cliente presso lo studio
    /// Registra ore e tariffa per calcolo automatico
    /// </summary>
    public class AccessoCliente
    {
        public int Id { get; set; }

        [Required]
        public int ClienteId { get; set; }

        [ForeignKey("ClienteId")]
        public Cliente? Cliente { get; set; }

        [Required]
        [Display(Name = "Scadenza Destinazione")]
        public int ScadenzaFatturazioneId { get; set; }

        [ForeignKey("ScadenzaFatturazioneId")]
        public ScadenzaFatturazione? ScadenzaFatturazione { get; set; }

        // Utente/professionista che ha gestito l'accesso
        public int? UtenteId { get; set; }

        [ForeignKey("UtenteId")]
        public User? Utente { get; set; }

        [Required]
        [Display(Name = "Data Accesso")]
        [DataType(DataType.Date)]
        public DateTime Data { get; set; } = DateTime.Today;

        // Orari mattino
        [Display(Name = "Inizio Mattino")]
        public TimeSpan? OraInizioMattino { get; set; }

        [Display(Name = "Fine Mattino")]
        public TimeSpan? OraFineMattino { get; set; }

        // Orari pomeriggio
        [Display(Name = "Inizio Pomeriggio")]
        public TimeSpan? OraInizioPomeriggio { get; set; }

        [Display(Name = "Fine Pomeriggio")]
        public TimeSpan? OraFinePomeriggio { get; set; }

        [Required]
        [Display(Name = "Tariffa €/h")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TariffaOraria { get; set; }

        [Display(Name = "Note")]
        [StringLength(500)]
        public string? Note { get; set; }

        [Display(Name = "Fatturazione Separata")]
        public bool FatturazioneSeparata { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // Computed properties
        [NotMapped]
        public decimal OreMattino
        {
            get
            {
                if (!OraInizioMattino.HasValue || !OraFineMattino.HasValue) return 0;
                var diff = OraFineMattino.Value - OraInizioMattino.Value;
                return (decimal)diff.TotalHours;
            }
        }

        [NotMapped]
        public decimal OrePomeriggio
        {
            get
            {
                if (!OraInizioPomeriggio.HasValue || !OraFinePomeriggio.HasValue) return 0;
                var diff = OraFinePomeriggio.Value - OraInizioPomeriggio.Value;
                return (decimal)diff.TotalHours;
            }
        }

        [NotMapped]
        public decimal TotaleOre => OreMattino + OrePomeriggio;

        [NotMapped]
        public decimal TotaleImporto => Math.Round(TotaleOre * TariffaOraria, 2);
    }
}

