using System.ComponentModel.DataAnnotations;

namespace StudioCG.Web.Models.Fatturazione
{
    /// <summary>
    /// Tipo di periodicità delle scadenze del mandato
    /// </summary>
    public enum TipoScadenzaMandato
    {
        [Display(Name = "Mensile")]
        Mensile = 0,        // 12 rate

        [Display(Name = "Bimestrale")]
        Bimestrale = 1,     // 6 rate

        [Display(Name = "Trimestrale")]
        Trimestrale = 2,    // 4 rate

        [Display(Name = "Semestrale")]
        Semestrale = 3,     // 2 rate

        [Display(Name = "Annuale")]
        Annuale = 4         // 1 rata
    }

    /// <summary>
    /// Stato della scadenza nella pagina Fatturazione
    /// </summary>
    public enum StatoScadenza
    {
        [Display(Name = "Aperta")]
        Aperta = 0,

        [Display(Name = "Proforma")]
        Proforma = 1,

        [Display(Name = "Fatturata")]
        Fatturata = 2
    }

    /// <summary>
    /// Stato dell'incasso nella pagina Incassi (separata)
    /// </summary>
    public enum StatoIncasso
    {
        [Display(Name = "Da Incassare")]
        DaIncassare = 0,

        [Display(Name = "Parzialmente Incassata")]
        ParzialmenteIncassata = 1,

        [Display(Name = "Incassata")]
        Incassata = 2
    }

    /// <summary>
    /// Tipo di documento per contatori automatici
    /// </summary>
    public enum TipoDocumento
    {
        [Display(Name = "Proforma")]
        Proforma = 0,

        [Display(Name = "Fattura")]
        Fattura = 1
    }
}

