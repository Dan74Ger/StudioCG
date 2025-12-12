using System.ComponentModel.DataAnnotations;

namespace StudioCG.Web.Models
{
    /// <summary>
    /// Rappresenta il valore di un campo per un record specifico
    /// </summary>
    public class DynamicFieldValue
    {
        public int Id { get; set; }

        [Required]
        public int DynamicRecordId { get; set; }

        [Required]
        public int DynamicFieldId { get; set; }

        /// <summary>
        /// Valore salvato come stringa (verrà convertito in base al tipo del campo)
        /// </summary>
        [Display(Name = "Valore")]
        public string? Value { get; set; }

        // Navigation properties
        public virtual DynamicRecord DynamicRecord { get; set; } = null!;
        public virtual DynamicField DynamicField { get; set; } = null!;
    }
}

