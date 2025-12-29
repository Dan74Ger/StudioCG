using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudioCG.Web.Models
{
    /// <summary>
    /// Stato personalizzabile per ogni tipo di attività.
    /// Ogni AttivitaTipo può avere i propri stati con colori e icone personalizzati.
    /// </summary>
    public class StatoAttivitaTipo
    {
        public int Id { get; set; }

        [Required]
        public int AttivitaTipoId { get; set; }

        [Required(ErrorMessage = "Il nome dello stato è obbligatorio")]
        [StringLength(100)]
        [Display(Name = "Nome Stato")]
        public string Nome { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Icona")]
        public string? Icon { get; set; } = "fas fa-circle";

        [StringLength(20)]
        [Display(Name = "Colore Testo")]
        public string ColoreTesto { get; set; } = "#FFFFFF";

        [StringLength(20)]
        [Display(Name = "Colore Sfondo")]
        public string ColoreSfondo { get; set; } = "#6c757d";

        [Display(Name = "Ordine")]
        public int DisplayOrder { get; set; } = 0;

        /// <summary>
        /// Se true, questo stato viene assegnato automaticamente ai nuovi ClienteAttivita
        /// </summary>
        [Display(Name = "Stato Default")]
        public bool IsDefault { get; set; } = false;

        /// <summary>
        /// Se true, questo stato indica che l'attività è completata (per statistiche)
        /// </summary>
        [Display(Name = "Stato Finale")]
        public bool IsFinale { get; set; } = false;

        [Display(Name = "Attivo")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Data Creazione")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation property
        [ForeignKey("AttivitaTipoId")]
        public virtual AttivitaTipo? AttivitaTipo { get; set; }
    }
}

