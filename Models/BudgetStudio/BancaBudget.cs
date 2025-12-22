using System.ComponentModel.DataAnnotations;

namespace StudioCG.Web.Models.BudgetStudio
{
    /// <summary>
    /// Rappresenta una banca/conto corrente per il Budget Studio
    /// </summary>
    public class BancaBudget
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Nome Banca")]
        public string Nome { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "IBAN")]
        public string? Iban { get; set; }

        [Display(Name = "Attiva")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Ordine")]
        public int Ordine { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<SaldoBancaMese> SaldiMensili { get; set; } = new List<SaldoBancaMese>();
    }

    /// <summary>
    /// Saldo iniziale di una banca per un determinato mese/anno
    /// </summary>
    public class SaldoBancaMese
    {
        public int Id { get; set; }

        [Required]
        public int BancaBudgetId { get; set; }

        [Required]
        [Range(2020, 2100)]
        public int Anno { get; set; }

        [Required]
        [Range(1, 12)]
        public int Mese { get; set; }

        [Display(Name = "Saldo")]
        public decimal Saldo { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        public virtual BancaBudget? BancaBudget { get; set; }
    }
}
