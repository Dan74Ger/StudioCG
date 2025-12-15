using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudioCG.Web.Models
{
    public class AttivitaAnnuale
    {
        public int Id { get; set; }

        [Required]
        public int AttivitaTipoId { get; set; }

        [Required]
        public int AnnualitaFiscaleId { get; set; }

        [Display(Name = "Attivo")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Data Scadenza")]
        [DataType(DataType.Date)]
        public DateTime? DataScadenza { get; set; }

        [Display(Name = "Data Creazione")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("AttivitaTipoId")]
        public virtual AttivitaTipo? AttivitaTipo { get; set; }

        [ForeignKey("AnnualitaFiscaleId")]
        public virtual AnnualitaFiscale? AnnualitaFiscale { get; set; }

        public virtual ICollection<ClienteAttivita> ClientiAttivita { get; set; } = new List<ClienteAttivita>();
    }
}

