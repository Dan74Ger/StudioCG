using System.ComponentModel.DataAnnotations;

namespace StudioCG.Web.Models.Fatturazione
{
    /// <summary>
    /// Contatore automatico per numerazione Proforma/Fatture
    /// Si auto-corregge se l'ultimo viene annullato
    /// </summary>
    public class ContatoreDocumento
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Anno")]
        public int Anno { get; set; }

        [Required]
        [Display(Name = "Tipo Documento")]
        public TipoDocumento TipoDocumento { get; set; }

        [Required]
        [Display(Name = "Ultimo Numero")]
        public int UltimoNumero { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}

