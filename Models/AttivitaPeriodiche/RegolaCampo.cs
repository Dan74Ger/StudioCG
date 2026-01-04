using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudioCG.Web.Models.AttivitaPeriodiche
{
    /// <summary>
    /// Regola per un campo: può essere di tipo Riporto o Colore
    /// </summary>
    public class RegolaCampo
    {
        public int Id { get; set; }

        [Required]
        public int CampoPeriodicoId { get; set; }

        /// <summary>
        /// Tipo di regola: "riporto" o "colore"
        /// </summary>
        [Required]
        [StringLength(20)]
        [Display(Name = "Tipo Regola")]
        public string TipoRegola { get; set; } = "colore"; // "riporto" o "colore"

        [StringLength(100)]
        [Display(Name = "Nome Regola")]
        public string? NomeRegola { get; set; }

        // ===== SOLO PER REGOLE RIPORTO =====

        /// <summary>
        /// ID del campo da cui prendere il valore per il riporto
        /// (può essere lo stesso campo o un altro)
        /// </summary>
        [Display(Name = "Campo Origine Riporto")]
        public int? CampoOrigineId { get; set; }

        /// <summary>
        /// ID del campo di destinazione nel periodo successivo
        /// </summary>
        [Display(Name = "Campo Destinazione Riporto")]
        public int? CampoDestinazioneId { get; set; }

        /// <summary>
        /// Condizione per il riporto: "sempre", "se_positivo", "se_negativo", "se_diverso_zero"
        /// </summary>
        [StringLength(50)]
        [Display(Name = "Condizione Riporto")]
        public string? CondizioneRiporto { get; set; } = "sempre";

        // ===== SOLO PER REGOLE COLORE =====

        /// <summary>
        /// Operatore di confronto: "<", ">", "=", "<=", ">=", "!="
        /// </summary>
        [StringLength(10)]
        [Display(Name = "Operatore")]
        public string? Operatore { get; set; }

        /// <summary>
        /// Valore da confrontare
        /// </summary>
        [StringLength(100)]
        [Display(Name = "Valore Confronto")]
        public string? ValoreConfronto { get; set; }

        /// <summary>
        /// Colore del testo (es: #dc3545 per rosso)
        /// </summary>
        [StringLength(20)]
        [Display(Name = "Colore Testo")]
        public string? ColoreTesto { get; set; }

        /// <summary>
        /// Colore dello sfondo (es: #f8d7da per rosso chiaro)
        /// </summary>
        [StringLength(20)]
        [Display(Name = "Colore Sfondo")]
        public string? ColoreSfondo { get; set; }

        [Display(Name = "Grassetto")]
        public bool Grassetto { get; set; } = false;

        /// <summary>
        /// Icona FontAwesome da mostrare
        /// </summary>
        [StringLength(50)]
        [Display(Name = "Icona")]
        public string? Icona { get; set; }

        /// <summary>
        /// Dove applicare la regola: "campo" o "riga"
        /// </summary>
        [StringLength(20)]
        [Display(Name = "Applica A")]
        public string ApplicaA { get; set; } = "campo";

        [Display(Name = "Priorità")]
        public int Priorita { get; set; } = 0;

        [Display(Name = "Attiva")]
        public bool IsActive { get; set; } = true;

        // Navigation
        [ForeignKey("CampoPeriodicoId")]
        public virtual CampoPeriodico? CampoPeriodico { get; set; }

        [ForeignKey("CampoOrigineId")]
        public virtual CampoPeriodico? CampoOrigine { get; set; }

        [ForeignKey("CampoDestinazioneId")]
        public virtual CampoPeriodico? CampoDestinazione { get; set; }
    }
}
