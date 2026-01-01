using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudioCG.Web.Models
{
    /// <summary>
    /// Voce del menu dinamico
    /// </summary>
    public class VoceMenu
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Il nome è obbligatorio")]
        [StringLength(100)]
        [Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        [StringLength(255)]
        [Display(Name = "URL")]
        public string? Url { get; set; } // Null se è un gruppo/contenitore

        [StringLength(50)]
        [Display(Name = "Icona")]
        public string Icon { get; set; } = "fas fa-folder";

        [StringLength(50)]
        [Display(Name = "Categoria")]
        public string? Categoria { get; set; } // Per raggruppamento visivo

        [Display(Name = "Ordine")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Visibile")]
        public bool IsVisible { get; set; } = true;

        [Display(Name = "Attivo")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "È un Gruppo")]
        public bool IsGroup { get; set; } = false; // Se true, è un contenitore di sottovoci

        [Display(Name = "Espandi di Default")]
        public bool ExpandedByDefault { get; set; } = false;

        // Gerarchia: voce padre
        [Display(Name = "Voce Padre")]
        public int? ParentId { get; set; }

        [ForeignKey("ParentId")]
        public virtual VoceMenu? Parent { get; set; }

        public virtual ICollection<VoceMenu> Children { get; set; } = new List<VoceMenu>();

        // Tipo di voce: serve per identificare voci di sistema vs custom
        [StringLength(50)]
        [Display(Name = "Tipo")]
        public string TipoVoce { get; set; } = "custom"; // system, custom, activity, dynamic_page, entity

        // Riferimento ad entità collegate (es: AttivitaTipo.Id, DynamicPage.Id)
        [Display(Name = "ID Riferimento")]
        public int? ReferenceId { get; set; }

        [Display(Name = "Data Creazione")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Helper per ottenere le opzioni dropdown
        public List<string> GetOptionsAsList()
        {
            return string.IsNullOrEmpty(Url) 
                ? new List<string>() 
                : new List<string> { Url };
        }
    }

    /// <summary>
    /// Configurazione visibilità menu per utente (opzionale per personalizzazione per utente)
    /// </summary>
    public class ConfigurazioneMenuUtente
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int VoceMenuId { get; set; }

        [Display(Name = "Visibile")]
        public bool IsVisible { get; set; } = true;

        [Display(Name = "Ordine Personalizzato")]
        public int? CustomOrder { get; set; }

        // Navigation
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [ForeignKey("VoceMenuId")]
        public virtual VoceMenu? VoceMenu { get; set; }
    }
}
