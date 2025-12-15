using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudioCG.Web.Models
{
    public enum AttivitaFieldType
    {
        [Display(Name = "Testo")]
        Text = 0,

        [Display(Name = "Testo Lungo")]
        LongText = 1,

        [Display(Name = "Numero")]
        Number = 2,

        [Display(Name = "Decimale")]
        Decimal = 3,

        [Display(Name = "Data")]
        Date = 4,

        [Display(Name = "Sì/No")]
        Boolean = 5
    }

    public class AttivitaCampo
    {
        public int Id { get; set; }

        [Required]
        public int AttivitaTipoId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'etichetta è obbligatoria")]
        [StringLength(100)]
        [Display(Name = "Etichetta")]
        public string Label { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Tipo Campo")]
        public AttivitaFieldType FieldType { get; set; } = AttivitaFieldType.Text;

        [Display(Name = "Obbligatorio")]
        public bool IsRequired { get; set; } = false;

        [Display(Name = "Mostra in Lista")]
        public bool ShowInList { get; set; } = true;

        [Display(Name = "Ordine")]
        public int DisplayOrder { get; set; } = 0;

        // Navigation property
        [ForeignKey("AttivitaTipoId")]
        public virtual AttivitaTipo? AttivitaTipo { get; set; }
    }
}

