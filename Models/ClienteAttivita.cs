using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudioCG.Web.Models
{
    // Enum mantenuto per retrocompatibilità durante la migrazione
    public enum StatoAttivita
    {
        [Display(Name = "Da Fare")]
        DaFare = 0,

        [Display(Name = "Completata")]
        Completata = 1,

        [Display(Name = "Da inviare Entratel")]
        DaInviareEntratel = 2,

        [Display(Name = "DR Inviate")]
        DRInviate = 3,

        [Display(Name = "Sospesa")]
        Sospesa = 4
    }

    public class ClienteAttivita
    {
        public int Id { get; set; }

        [Required]
        public int ClienteId { get; set; }

        [Required]
        public int AttivitaAnnualeId { get; set; }

        // Vecchio campo enum - mantenuto per retrocompatibilità durante migrazione
        [Display(Name = "Stato (Legacy)")]
        public StatoAttivita Stato { get; set; } = StatoAttivita.DaFare;

        // Nuovo campo FK per stati dinamici
        [Display(Name = "Stato")]
        public int? StatoAttivitaTipoId { get; set; }

        [Display(Name = "Data Completamento")]
        [DataType(DataType.Date)]
        public DateTime? DataCompletamento { get; set; }

        public string? Note { get; set; }

        [Display(Name = "Data Creazione")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Ultima Modifica")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("ClienteId")]
        public virtual Cliente? Cliente { get; set; }

        [ForeignKey("AttivitaAnnualeId")]
        public virtual AttivitaAnnuale? AttivitaAnnuale { get; set; }

        [ForeignKey("StatoAttivitaTipoId")]
        public virtual StatoAttivitaTipo? StatoAttivitaTipoNav { get; set; }

        public virtual ICollection<ClienteAttivitaValore> Valori { get; set; } = new List<ClienteAttivitaValore>();
    }
}

