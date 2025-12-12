using System.ComponentModel.DataAnnotations;

namespace StudioCG.Web.Models
{
    /// <summary>
    /// Rappresenta un campo di una pagina dinamica
    /// </summary>
    public class DynamicField
    {
        public int Id { get; set; }

        [Required]
        public int DynamicPageId { get; set; }

        [Required(ErrorMessage = "Il nome del campo è obbligatorio")]
        [StringLength(100)]
        [Display(Name = "Nome Campo")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'etichetta è obbligatoria")]
        [StringLength(100)]
        [Display(Name = "Etichetta")]
        public string Label { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "Tipo")]
        public string FieldType { get; set; } = FieldTypes.Text; // Text, TextArea, Number, Decimal, Date, Boolean

        [Display(Name = "Obbligatorio")]
        public bool IsRequired { get; set; } = false;

        [Display(Name = "Mostra in Lista")]
        public bool ShowInList { get; set; } = true;

        [Display(Name = "Ordine")]
        public int DisplayOrder { get; set; } = 0;

        [StringLength(500)]
        [Display(Name = "Valore Default")]
        public string? DefaultValue { get; set; }

        [StringLength(200)]
        [Display(Name = "Placeholder")]
        public string? Placeholder { get; set; }

        // Navigation property
        public virtual DynamicPage DynamicPage { get; set; } = null!;
        public virtual ICollection<DynamicFieldValue> FieldValues { get; set; } = new List<DynamicFieldValue>();
    }

    /// <summary>
    /// Tipi di campo disponibili
    /// </summary>
    public static class FieldTypes
    {
        public const string Text = "Text";
        public const string TextArea = "TextArea";
        public const string Number = "Number";
        public const string Decimal = "Decimal";
        public const string Date = "Date";
        public const string Boolean = "Boolean";

        public static List<(string Value, string Label)> GetAll()
        {
            return new List<(string, string)>
            {
                (Text, "Testo"),
                (TextArea, "Testo Lungo"),
                (Number, "Numero"),
                (Decimal, "Decimale"),
                (Date, "Data"),
                (Boolean, "Sì/No")
            };
        }
    }
}

