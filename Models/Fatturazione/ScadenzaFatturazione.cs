using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudioCG.Web.Models.Fatturazione
{
    /// <summary>
    /// Scadenza per fatturazione
    /// Può provenire da mandato, spese pratiche, accessi, fatture cloud, bilanci
    /// </summary>
    public class ScadenzaFatturazione
    {
        public int Id { get; set; }

        // Riferimento al mandato (opzionale, potrebbe essere una scadenza manuale)
        public int? MandatoClienteId { get; set; }

        [ForeignKey("MandatoClienteId")]
        public MandatoCliente? MandatoCliente { get; set; }

        [Required]
        public int ClienteId { get; set; }

        [ForeignKey("ClienteId")]
        public Cliente? Cliente { get; set; }

        [Required]
        [Display(Name = "Anno")]
        public int Anno { get; set; }

        [Required]
        [Display(Name = "Data Scadenza")]
        [DataType(DataType.Date)]
        public DateTime DataScadenza { get; set; }

        [Display(Name = "Importo Mandato")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ImportoMandato { get; set; }

        [Display(Name = "Rimborso Spese")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal RimborsoSpese { get; set; } = 0;

        // Proforma
        [Display(Name = "N. Proforma")]
        public int? NumeroProforma { get; set; }

        [Display(Name = "Data Proforma")]
        [DataType(DataType.Date)]
        public DateTime? DataProforma { get; set; }

        // Fattura
        [Display(Name = "N. Fattura")]
        public int? NumeroFattura { get; set; }

        [Display(Name = "Data Fattura")]
        [DataType(DataType.Date)]
        public DateTime? DataFattura { get; set; }

        [Display(Name = "Stato")]
        public StatoScadenza Stato { get; set; } = StatoScadenza.Aperta;

        [Display(Name = "Stato Incasso")]
        public StatoIncasso StatoIncasso { get; set; } = StatoIncasso.DaIncassare;

        [Display(Name = "Note")]
        [StringLength(500)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // Navigation per spese collegate
        public virtual ICollection<SpesaPratica> SpesePratiche { get; set; } = new List<SpesaPratica>();
        public virtual ICollection<AccessoCliente> AccessiClienti { get; set; } = new List<AccessoCliente>();
        public virtual ICollection<FatturaCloud> FattureCloud { get; set; } = new List<FatturaCloud>();
        public virtual ICollection<BilancioCEE> BilanciCEE { get; set; } = new List<BilancioCEE>();
        public virtual ICollection<IncassoFattura> Incassi { get; set; } = new List<IncassoFattura>();

        // Computed properties
        [NotMapped]
        public decimal TotaleSpesePratiche => SpesePratiche?.Sum(s => s.Importo) ?? 0;

        [NotMapped]
        public decimal TotaleAccessiClienti => AccessiClienti?.Sum(a => a.TotaleImporto) ?? 0;

        [NotMapped]
        public decimal TotaleFattureCloud => FattureCloud?.Sum(f => f.Importo) ?? 0;

        [NotMapped]
        public decimal TotaleBilanciCEE => BilanciCEE?.Sum(b => b.Importo) ?? 0;

        [NotMapped]
        public decimal TotaleSpese => TotaleSpesePratiche + TotaleAccessiClienti + TotaleFattureCloud + TotaleBilanciCEE;

        [NotMapped]
        public decimal TotaleScadenza => ImportoMandato + RimborsoSpese + TotaleSpese;

        [NotMapped]
        public decimal TotaleIncassato => Incassi?.Sum(i => i.ImportoIncassato) ?? 0;

        [NotMapped]
        public decimal ResiduoDaIncassare => TotaleScadenza - TotaleIncassato;

        [NotMapped]
        public string NumeroProformaFormattato => NumeroProforma.HasValue ? $"P-{Anno}/{NumeroProforma:D4}" : "-";

        [NotMapped]
        public string NumeroFatturaFormattato => NumeroFattura.HasValue ? $"F-{Anno}/{NumeroFattura:D4}" : "-";
    }
}

