using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudioCG.Web.Models
{
    public class ClienteAttivitaValore
    {
        public int Id { get; set; }

        [Required]
        public int ClienteAttivitaId { get; set; }

        [Required]
        public int AttivitaCampoId { get; set; }

        public string? Valore { get; set; }

        // Navigation properties
        [ForeignKey("ClienteAttivitaId")]
        public virtual ClienteAttivita? ClienteAttivita { get; set; }

        [ForeignKey("AttivitaCampoId")]
        public virtual AttivitaCampo? AttivitaCampo { get; set; }
    }
}

