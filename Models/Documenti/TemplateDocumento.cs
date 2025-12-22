using System.ComponentModel.DataAnnotations;

namespace StudioCG.Web.Models.Documenti
{
    /// <summary>
    /// Template documento (mandato, privacy, antiriciclaggio, ecc.)
    /// </summary>
    public class TemplateDocumento
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Nome Template")]
        public string Nome { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "Categoria")]
        public string Categoria { get; set; } = string.Empty;

        [Display(Name = "Descrizione")]
        [StringLength(500)]
        public string? Descrizione { get; set; }

        [Display(Name = "Intestazione")]
        public string? Intestazione { get; set; } // HTML header con logo, dati studio, ecc.

        [Required]
        [Display(Name = "Contenuto")]
        public string Contenuto { get; set; } = string.Empty; // HTML con tag {{...}}

        [Display(Name = "Piè di Pagina")]
        public string? PiePagina { get; set; } // HTML footer con firma, note, ecc.

        [Display(Name = "Richiede Mandato")]
        public bool RichiestaMandato { get; set; } = false;

        [Display(Name = "Formato Output Default")]
        public TipoOutputDocumento TipoOutputDefault { get; set; } = TipoOutputDocumento.PDF;

        [Display(Name = "Ordine")]
        public int Ordine { get; set; } = 0;

        [Display(Name = "Attivo")]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // Navigazione
        public virtual ICollection<DocumentoGenerato> DocumentiGenerati { get; set; } = new List<DocumentoGenerato>();
    }

    /// <summary>
    /// Categorie predefinite per i template
    /// </summary>
    public static class CategorieTemplate
    {
        public const string Mandati = "Mandati";
        public const string Privacy = "Privacy";
        public const string Antiriciclaggio = "Antiriciclaggio";
        public const string Comunicazioni = "Comunicazioni";
        public const string Lettere = "Lettere";
        public const string Altro = "Altro";

        public static List<string> Tutte => new()
        {
            Mandati, Privacy, Antiriciclaggio, Comunicazioni, Lettere, Altro
        };
    }

    /// <summary>
    /// Tipo di output documento
    /// </summary>
    public enum TipoOutputDocumento
    {
        PDF = 0,
        Word = 1
    }
}

