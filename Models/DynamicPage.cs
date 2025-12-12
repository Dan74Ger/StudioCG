using System.ComponentModel.DataAnnotations;

namespace StudioCG.Web.Models
{
    /// <summary>
    /// Rappresenta una pagina dinamica creata dall'utente
    /// </summary>
    public class DynamicPage
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Il nome della pagina è obbligatorio")]
        [StringLength(100)]
        [Display(Name = "Nome Pagina")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Descrizione")]
        public string? Description { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Categoria")]
        public string Category { get; set; } = "DatiGenerali"; // DatiRiservati o DatiGenerali

        [StringLength(100)]
        [Display(Name = "Icona")]
        public string Icon { get; set; } = "fas fa-file-alt";

        [Display(Name = "Ordine")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Attiva")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Data Creazione")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Nome della tabella generata (slug del nome)
        /// </summary>
        [StringLength(100)]
        public string TableName { get; set; } = string.Empty;

        // Navigation properties
        public virtual ICollection<DynamicField> Fields { get; set; } = new List<DynamicField>();
        public virtual ICollection<DynamicRecord> Records { get; set; } = new List<DynamicRecord>();
    }
}

