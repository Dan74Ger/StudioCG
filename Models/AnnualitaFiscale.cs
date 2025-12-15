using System.ComponentModel.DataAnnotations;

namespace StudioCG.Web.Models
{
    public class AnnualitaFiscale
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "L'anno è obbligatorio")]
        [Display(Name = "Anno")]
        public int Anno { get; set; }

        [StringLength(100)]
        public string? Descrizione { get; set; }

        [Display(Name = "Attivo")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Anno Corrente")]
        public bool IsCurrent { get; set; } = false;

        [Display(Name = "Data Creazione")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual ICollection<AttivitaAnnuale> AttivitaAnnuali { get; set; } = new List<AttivitaAnnuale>();
    }
}

