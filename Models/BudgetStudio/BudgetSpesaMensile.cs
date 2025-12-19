using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudioCG.Web.Models.BudgetStudio
{
    public class BudgetSpesaMensile
    {
        public int Id { get; set; }

        [Required]
        public int VoceSpesaBudgetId { get; set; }

        public VoceSpesaBudget? VoceSpesaBudget { get; set; }

        [Required]
        public int Anno { get; set; }

        [Required]
        [Range(1, 12)]
        public int Mese { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Importo { get; set; }

        [Display(Name = "Pagata")]
        public bool Pagata { get; set; } = false;

        [Display(Name = "Metodo pagamento")]
        public MetodoPagamentoBudget? MetodoPagamento { get; set; }

        [StringLength(500)]
        [Display(Name = "Note")]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}


