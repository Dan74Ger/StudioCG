using System.ComponentModel.DataAnnotations;

namespace StudioCG.Web.Models.Documenti
{
    /// <summary>
    /// Clausole/blocchi di testo riutilizzabili nei template
    /// </summary>
    public class ClausolaDocumento
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Nome Clausola")]
        public string Nome { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "Categoria")]
        public string Categoria { get; set; } = string.Empty;

        [Display(Name = "Descrizione")]
        [StringLength(500)]
        public string? Descrizione { get; set; }

        [Required]
        [Display(Name = "Contenuto")]
        public string Contenuto { get; set; } = string.Empty; // HTML

        [Display(Name = "Ordine")]
        public int Ordine { get; set; } = 0;

        [Display(Name = "Attiva")]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Categorie predefinite per le clausole
    /// </summary>
    public static class CategorieClausole
    {
        public const string Privacy = "Privacy";
        public const string Pagamenti = "Pagamenti";
        public const string Obblighi = "Obblighi";
        public const string Recesso = "Recesso";
        public const string Firme = "Firme";
        public const string Antiriciclaggio = "Antiriciclaggio";
        public const string Generale = "Generale";

        public static List<string> Tutte => new()
        {
            Privacy, Pagamenti, Obblighi, Recesso, Firme, Antiriciclaggio, Generale
        };
    }
}

