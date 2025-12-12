using System.ComponentModel.DataAnnotations;

namespace StudioCG.Web.Models
{
    /// <summary>
    /// Rappresenta un record (riga) di una pagina dinamica
    /// </summary>
    public class DynamicRecord
    {
        public int Id { get; set; }

        [Required]
        public int DynamicPageId { get; set; }

        [Display(Name = "Data Creazione")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Data Modifica")]
        public DateTime? UpdatedAt { get; set; }

        [StringLength(100)]
        [Display(Name = "Creato Da")]
        public string? CreatedBy { get; set; }

        // Navigation properties
        public virtual DynamicPage DynamicPage { get; set; } = null!;
        public virtual ICollection<DynamicFieldValue> FieldValues { get; set; } = new List<DynamicFieldValue>();
    }
}

