using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudioCG.Web.Models
{
    /// <summary>
    /// Definizione di un campo personalizzato per i clienti
    /// </summary>
    public class CampoCustomCliente
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Il nome del campo è obbligatorio")]
        [StringLength(100)]
        [Display(Name = "Nome Campo")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'etichetta è obbligatoria")]
        [StringLength(100)]
        [Display(Name = "Etichetta")]
        public string Label { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "Tipo Campo")]
        public string TipoCampo { get; set; } = "text"; // text, number, decimal, date, datetime, checkbox, dropdown, textarea, email, phone, url

        [Display(Name = "Obbligatorio")]
        public bool IsRequired { get; set; } = false;

        [Display(Name = "Mostra in Lista")]
        public bool ShowInList { get; set; } = false;

        [Display(Name = "Usa come Filtro")]
        public bool UseAsFilter { get; set; } = false;

        [StringLength(500)]
        [Display(Name = "Opzioni (per dropdown, separate da |)")]
        public string? Options { get; set; } // Per dropdown: "Opzione1|Opzione2|Opzione3"

        [StringLength(200)]
        [Display(Name = "Valore Default")]
        public string? DefaultValue { get; set; }

        [StringLength(200)]
        [Display(Name = "Suggerimento")]
        public string? Placeholder { get; set; }

        [Display(Name = "Ordine")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Attivo")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Data Creazione")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public virtual ICollection<ValoreCampoCustomCliente> Valori { get; set; } = new List<ValoreCampoCustomCliente>();
    }

    /// <summary>
    /// Valore di un campo personalizzato per un cliente specifico
    /// </summary>
    public class ValoreCampoCustomCliente
    {
        public int Id { get; set; }

        [Required]
        public int ClienteId { get; set; }

        [Required]
        public int CampoCustomClienteId { get; set; }

        [Display(Name = "Valore")]
        public string? Valore { get; set; }

        [Display(Name = "Ultima Modifica")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation
        [ForeignKey("ClienteId")]
        public virtual Cliente? Cliente { get; set; }

        [ForeignKey("CampoCustomClienteId")]
        public virtual CampoCustomCliente? CampoCustom { get; set; }
    }
}
