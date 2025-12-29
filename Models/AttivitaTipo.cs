using System.ComponentModel.DataAnnotations;

namespace StudioCG.Web.Models
{
    public class AttivitaTipo
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Il nome è obbligatorio")]
        [StringLength(100)]
        public string Nome { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Descrizione { get; set; }

        [StringLength(50)]
        public string? Icon { get; set; } = "fas fa-tasks";

        [Display(Name = "Ordine")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Attivo")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Data Creazione")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual ICollection<AttivitaCampo> Campi { get; set; } = new List<AttivitaCampo>();
        public virtual ICollection<AttivitaAnnuale> AttivitaAnnuali { get; set; } = new List<AttivitaAnnuale>();
        public virtual ICollection<StatoAttivitaTipo> Stati { get; set; } = new List<StatoAttivitaTipo>();
    }
}

