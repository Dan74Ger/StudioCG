using System.ComponentModel.DataAnnotations;

namespace StudioCG.Web.Models.BudgetStudio
{
    public class VoceSpesaBudget
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Codice Spesa")]
        public string CodiceSpesa { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        [Display(Name = "Descrizione")]
        public string Descrizione { get; set; } = string.Empty;

        [Display(Name = "Metodo pagamento default")]
        public MetodoPagamentoBudget MetodoPagamentoDefault { get; set; } = MetodoPagamentoBudget.Bonifico;

        [StringLength(500)]
        [Display(Name = "Note default")]
        public string? NoteDefault { get; set; }

        [Display(Name = "Attiva")]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<BudgetSpesaMensile> BudgetMensile { get; set; } = new List<BudgetSpesaMensile>();
    }
}


