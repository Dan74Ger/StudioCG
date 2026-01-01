using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudioCG.Web.Models.Entita
{
    /// <summary>
    /// Definizione di un'entità dinamica personalizzata
    /// </summary>
    public class EntitaDinamica
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Il nome è obbligatorio")]
        [StringLength(100)]
        [Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Nome Plurale")]
        public string NomePluruale { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Descrizione")]
        public string? Descrizione { get; set; }

        [StringLength(50)]
        [Display(Name = "Icona")]
        public string Icon { get; set; } = "fas fa-folder";

        [StringLength(20)]
        [Display(Name = "Colore")]
        public string Colore { get; set; } = "#6c757d";

        [Display(Name = "Collegata a Cliente")]
        public bool CollegataACliente { get; set; } = true;

        [Display(Name = "Ordine")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Attiva")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Data Creazione")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public virtual ICollection<CampoEntita> Campi { get; set; } = new List<CampoEntita>();
        public virtual ICollection<StatoEntita> Stati { get; set; } = new List<StatoEntita>();
        public virtual ICollection<RecordEntita> Records { get; set; } = new List<RecordEntita>();
    }

    /// <summary>
    /// Campo di un'entità dinamica
    /// </summary>
    public class CampoEntita
    {
        public int Id { get; set; }

        [Required]
        public int EntitaDinamicaId { get; set; }

        [Required(ErrorMessage = "Il nome è obbligatorio")]
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
        public string TipoCampo { get; set; } = "text";
        // Tipi: text, textarea, number, decimal, date, datetime, checkbox, dropdown, email, phone, url, cliente, file

        [Display(Name = "Obbligatorio")]
        public bool IsRequired { get; set; } = false;

        [Display(Name = "Mostra in Lista")]
        public bool ShowInList { get; set; } = true;

        [Display(Name = "Usa come Filtro")]
        public bool UseAsFilter { get; set; } = false;

        [StringLength(1000)]
        [Display(Name = "Opzioni (separate da |)")]
        public string? Options { get; set; }

        [StringLength(200)]
        [Display(Name = "Valore Default")]
        public string? DefaultValue { get; set; }

        [StringLength(200)]
        [Display(Name = "Placeholder")]
        public string? Placeholder { get; set; }

        [Display(Name = "Ordine")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Attivo")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Larghezza Colonna (1-12)")]
        public int ColWidth { get; set; } = 4;

        // Navigation
        [ForeignKey("EntitaDinamicaId")]
        public virtual EntitaDinamica? EntitaDinamica { get; set; }

        public virtual ICollection<ValoreCampoEntita> Valori { get; set; } = new List<ValoreCampoEntita>();
    }

    /// <summary>
    /// Stato di un'entità dinamica
    /// </summary>
    public class StatoEntita
    {
        public int Id { get; set; }

        [Required]
        public int EntitaDinamicaId { get; set; }

        [Required(ErrorMessage = "Il nome è obbligatorio")]
        [StringLength(100)]
        [Display(Name = "Nome Stato")]
        public string Nome { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Icona")]
        public string? Icon { get; set; } = "fas fa-circle";

        [StringLength(20)]
        [Display(Name = "Colore Testo")]
        public string ColoreTesto { get; set; } = "#FFFFFF";

        [StringLength(20)]
        [Display(Name = "Colore Sfondo")]
        public string ColoreSfondo { get; set; } = "#6c757d";

        [Display(Name = "Ordine")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Stato Default")]
        public bool IsDefault { get; set; } = false;

        [Display(Name = "Stato Finale")]
        public bool IsFinale { get; set; } = false;

        [Display(Name = "Attivo")]
        public bool IsActive { get; set; } = true;

        // Navigation
        [ForeignKey("EntitaDinamicaId")]
        public virtual EntitaDinamica? EntitaDinamica { get; set; }
    }

    /// <summary>
    /// Record di un'entità dinamica
    /// </summary>
    public class RecordEntita
    {
        public int Id { get; set; }

        [Required]
        public int EntitaDinamicaId { get; set; }

        [Display(Name = "Cliente")]
        public int? ClienteId { get; set; }

        [Display(Name = "Stato")]
        public int? StatoEntitaId { get; set; }

        [Display(Name = "Titolo/Riferimento")]
        [StringLength(200)]
        public string? Titolo { get; set; }

        [Display(Name = "Note")]
        public string? Note { get; set; }

        [Display(Name = "Data Creazione")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Ultima Modifica")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Creato Da")]
        public int? CreatedByUserId { get; set; }

        // Navigation
        [ForeignKey("EntitaDinamicaId")]
        public virtual EntitaDinamica? EntitaDinamica { get; set; }

        [ForeignKey("ClienteId")]
        public virtual Cliente? Cliente { get; set; }

        [ForeignKey("StatoEntitaId")]
        public virtual StatoEntita? StatoEntita { get; set; }

        [ForeignKey("CreatedByUserId")]
        public virtual User? CreatedByUser { get; set; }

        public virtual ICollection<ValoreCampoEntita> Valori { get; set; } = new List<ValoreCampoEntita>();
    }

    /// <summary>
    /// Valore di un campo per un record specifico
    /// </summary>
    public class ValoreCampoEntita
    {
        public int Id { get; set; }

        [Required]
        public int RecordEntitaId { get; set; }

        [Required]
        public int CampoEntitaId { get; set; }

        [Display(Name = "Valore")]
        public string? Valore { get; set; }

        [Display(Name = "Ultima Modifica")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation
        [ForeignKey("RecordEntitaId")]
        public virtual RecordEntita? RecordEntita { get; set; }

        [ForeignKey("CampoEntitaId")]
        public virtual CampoEntita? CampoEntita { get; set; }
    }
}
