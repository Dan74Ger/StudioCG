using System.ComponentModel.DataAnnotations;

namespace StudioCG.Web.Models.BudgetStudio
{
    /// <summary>
    /// Macro voce (categoria/gruppo) per raggruppare le voci analitiche di spesa
    /// </summary>
    public class MacroVoceBudget
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Codice")]
        public string Codice { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        [Display(Name = "Descrizione")]
        public string Descrizione { get; set; } = string.Empty;

        [Display(Name = "Ordine")]
        public int Ordine { get; set; } = 0;

        [Display(Name = "Attiva")]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Voci analitiche associate a questa macro voce
        /// </summary>
        public virtual ICollection<VoceSpesaBudget> VociAnalitiche { get; set; } = new List<VoceSpesaBudget>();
    }
}

