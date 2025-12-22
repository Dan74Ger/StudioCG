using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StudioCG.Web.Models.Fatturazione;

namespace StudioCG.Web.Models.Documenti
{
    /// <summary>
    /// Documento generato e salvato in archivio
    /// </summary>
    public class DocumentoGenerato
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Template")]
        public int TemplateDocumentoId { get; set; }

        [Required]
        [Display(Name = "Cliente")]
        public int ClienteId { get; set; }

        [Display(Name = "Mandato")]
        public int? MandatoClienteId { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Nome File")]
        public string NomeFile { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Contenuto")]
        public byte[] Contenuto { get; set; } = Array.Empty<byte>();

        [Required]
        [StringLength(100)]
        [Display(Name = "Tipo File")]
        public string ContentType { get; set; } = string.Empty;

        [Display(Name = "Tipo Output")]
        public TipoOutputDocumento TipoOutput { get; set; }

        [Display(Name = "Generato da")]
        public int? GeneratoDaUserId { get; set; }

        [Display(Name = "Data Generazione")]
        public DateTime GeneratoIl { get; set; } = DateTime.Now;

        [Display(Name = "Note")]
        [StringLength(500)]
        public string? Note { get; set; }

        // Navigazione
        [ForeignKey("TemplateDocumentoId")]
        public virtual TemplateDocumento? TemplateDocumento { get; set; }

        [ForeignKey("ClienteId")]
        public virtual Cliente? Cliente { get; set; }

        [ForeignKey("MandatoClienteId")]
        public virtual MandatoCliente? MandatoCliente { get; set; }

        [ForeignKey("GeneratoDaUserId")]
        public virtual User? GeneratoDa { get; set; }
    }
}

