using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudioCG.Web.Models.AttivitaPeriodiche
{
    /// <summary>
    /// Definizione di una sezione di Attività Periodica (es: LIPE, Budget Mensile, ecc.)
    /// Completamente configurabile dall'utente
    /// </summary>
    public class AttivitaPeriodica
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Il nome è obbligatorio")]
        [StringLength(100)]
        [Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Nome Plurale")]
        public string NomePlurale { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Descrizione")]
        public string? Descrizione { get; set; }

        [StringLength(50)]
        [Display(Name = "Icona")]
        public string Icona { get; set; } = "fas fa-calendar-alt";

        [StringLength(20)]
        [Display(Name = "Colore")]
        public string Colore { get; set; } = "#17a2b8";

        [Display(Name = "Collegata a Cliente")]
        public bool CollegataACliente { get; set; } = true;

        [Display(Name = "Ordine Menu")]
        public int OrdineMenu { get; set; } = 0;

        [Display(Name = "Attiva")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Data Creazione")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // ===== LARGHEZZE COLONNE GRIGLIA =====

        [Display(Name = "Largh. Cliente (px)")]
        public int LarghezzaColonnaCliente { get; set; } = 150;

        [Display(Name = "Largh. Titolo (px)")]
        public int LarghezzaColonnaTitolo { get; set; } = 200;

        // Navigation
        public virtual ICollection<TipoPeriodo> TipiPeriodo { get; set; } = new List<TipoPeriodo>();
        public virtual ICollection<ClienteAttivitaPeriodica> ClientiAssociati { get; set; } = new List<ClienteAttivitaPeriodica>();
    }

    /// <summary>
    /// Tipo di periodo (Mensile, Trimestrale, Semestrale, Annuale, Custom...)
    /// </summary>
    public class TipoPeriodo
    {
        public int Id { get; set; }

        [Required]
        public int AttivitaPeriodicaId { get; set; }

        [Required(ErrorMessage = "Il nome è obbligatorio")]
        [StringLength(100)]
        [Display(Name = "Nome Tipo")]
        public string Nome { get; set; } = string.Empty;

        [Display(Name = "Numero Periodi")]
        public int NumeroPeriodi { get; set; } = 12; // 12=mensile, 4=trimestrale, 2=semestrale, 1=annuale

        /// <summary>
        /// Etichette dei periodi in formato JSON
        /// Es: ["Gennaio","Febbraio",...] o ["Q1","Q2","Q3","Q4"]
        /// </summary>
        [Display(Name = "Etichette Periodi")]
        public string EtichettePeriodi { get; set; } = "[]";

        /// <summary>
        /// Date inizio periodi in formato JSON
        /// Es: ["01/01","01/02",...] o ["01/01","01/04","01/07","01/10"]
        /// </summary>
        [Display(Name = "Date Inizio Periodi")]
        public string DateInizioPeriodi { get; set; } = "[]";

        /// <summary>
        /// Date fine periodi in formato JSON
        /// Es: ["31/01","28/02",...] o ["31/03","30/06","30/09","31/12"]
        /// </summary>
        [Display(Name = "Date Fine Periodi")]
        public string DateFinePeriodi { get; set; } = "[]";

        [StringLength(50)]
        [Display(Name = "Icona")]
        public string Icona { get; set; } = "fas fa-calendar";

        [StringLength(20)]
        [Display(Name = "Colore")]
        public string Colore { get; set; } = "#007bff";

        /// <summary>
        /// Mostra campo percentuale interessi (per trimestrali LIPE)
        /// </summary>
        [Display(Name = "Mostra Campo Interessi")]
        public bool MostraInteressi { get; set; } = false;

        [Display(Name = "% Interessi Default")]
        public decimal PercentualeInteressiDefault { get; set; } = 1.0m;

        [Display(Name = "Mostra Accordion Dettagli")]
        public bool MostraAccordion { get; set; } = true;

        [Display(Name = "Ordine")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Attivo")]
        public bool IsActive { get; set; } = true;

        // Navigation
        [ForeignKey("AttivitaPeriodicaId")]
        public virtual AttivitaPeriodica? AttivitaPeriodica { get; set; }

        public virtual ICollection<CampoPeriodico> Campi { get; set; } = new List<CampoPeriodico>();
        public virtual ICollection<ClienteAttivitaPeriodica> ClientiAssociati { get; set; } = new List<ClienteAttivitaPeriodica>();
    }

    /// <summary>
    /// Campo dinamico per un tipo di periodo
    /// Simile a CampoEntita ma per attività periodiche
    /// </summary>
    public class CampoPeriodico
    {
        public int Id { get; set; }

        [Required]
        public int TipoPeriodoId { get; set; }

        [Required(ErrorMessage = "Il nome è obbligatorio")]
        [StringLength(100)]
        [Display(Name = "Nome Campo")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'etichetta è obbligatoria")]
        [StringLength(100)]
        [Display(Name = "Etichetta")]
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Etichetta alternativa per il primo periodo (es: "Cred. Iva Anno Prec.")
        /// Se vuoto, usa Label standard
        /// </summary>
        [StringLength(100)]
        [Display(Name = "Etichetta Primo Periodo")]
        public string? LabelPrimoPeriodo { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Tipo Campo")]
        public string TipoCampo { get; set; } = "decimal";
        // Tipi: text, textarea, number, decimal, date, checkbox, dropdown

        [Display(Name = "Obbligatorio")]
        public bool IsRequired { get; set; } = false;

        [Display(Name = "Mostra in Griglia")]
        public bool ShowInList { get; set; } = true;

        [Display(Name = "Usa come Filtro")]
        public bool UseAsFilter { get; set; } = false;

        /// <summary>
        /// Se true, il campo viene mostrato una sola volta accanto al nome del cliente
        /// invece che ripetuto per ogni periodo (es: COD.COGE)
        /// </summary>
        [Display(Name = "Campo Cliente (vicino al nome)")]
        public bool IsCampoCliente { get; set; } = false;

        [StringLength(1000)]
        [Display(Name = "Opzioni (separate da |)")]
        public string? Options { get; set; }

        [StringLength(200)]
        [Display(Name = "Valore Default")]
        public string? DefaultValue { get; set; } = "0";

        [StringLength(200)]
        [Display(Name = "Placeholder")]
        public string? Placeholder { get; set; }

        [Display(Name = "Larghezza Form (1-12)")]
        public int ColWidth { get; set; } = 4;

        [Display(Name = "Larghezza Griglia (px)")]
        public int ColumnWidth { get; set; } = 100;

        [Display(Name = "Ordine")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Attivo")]
        public bool IsActive { get; set; } = true;

        // ===== CAMPI CALCOLATI =====

        [Display(Name = "Campo Calcolato")]
        public bool IsCalculated { get; set; } = false;

        /// <summary>
        /// Formula del calcolo. I campi sono referenziati con [NomeCampo].
        /// Es: "[CREDITO_PREC] + [IMPORTO_CREDITO] - [IMPORTO_DEBITO]"
        /// </summary>
        [StringLength(500)]
        [Display(Name = "Formula")]
        public string? Formula { get; set; }

        // ===== INDICATORI SPECIALI =====

        /// <summary>
        /// Se true, questo campo determina se un periodo è "compilato"
        /// (es: "Ultima FT" - se ha valore, il periodo è considerato completato)
        /// </summary>
        [Display(Name = "Indica Completamento")]
        public bool IsCompletionIndicator { get; set; } = false;

        /// <summary>
        /// Se true, questo campo viene usato per mostrare il risultato (credito/debito)
        /// nel badge del cliente (verde se >= 0, rosso se < 0)
        /// </summary>
        [Display(Name = "Indica Risultato (Credito/Debito)")]
        public bool IsResultIndicator { get; set; } = false;

        /// <summary>
        /// Periodi in cui il campo è visibile, separati da virgola (es: "1,12" per Gen e Dic).
        /// Vuoto o null = visibile in tutti i periodi.
        /// </summary>
        [StringLength(100)]
        [Display(Name = "Visibile Solo in Periodi")]
        public string? PeriodiVisibili { get; set; }

        // Navigation
        [ForeignKey("TipoPeriodoId")]
        public virtual TipoPeriodo? TipoPeriodo { get; set; }

        public virtual ICollection<RegolaCampo> Regole { get; set; } = new List<RegolaCampo>();
    }
}
