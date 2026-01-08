namespace StudioCG.Web.Models
{
    /// <summary>
    /// ViewModel unificato per le scadenze documenti identità.
    /// Può rappresentare sia un Cliente (PF, DI, PROF) che un Soggetto (Legale Rapp., Consigliere, Socio).
    /// </summary>
    public class ScadenzaDocumentoViewModel
    {
        public int Id { get; set; }
        
        /// <summary>
        /// true = Cliente diretto, false = Soggetto di un cliente
        /// </summary>
        public bool IsCliente { get; set; }
        
        /// <summary>
        /// ID del cliente (per soggetti: cliente di appartenenza, per clienti: proprio ID)
        /// </summary>
        public int ClienteId { get; set; }
        
        /// <summary>
        /// Nome del cliente / Ragione Sociale
        /// </summary>
        public string ClienteNome { get; set; } = string.Empty;
        
        /// <summary>
        /// Etichetta tipo: "PF", "DI", "PROF", "Legale Rapp.", "Consigliere", "Socio"
        /// </summary>
        public string TipoLabel { get; set; } = string.Empty;
        
        /// <summary>
        /// Classe Bootstrap per badge: "bg-dark", "bg-primary", "bg-info", "bg-success"
        /// </summary>
        public string TipoBadgeClass { get; set; } = "bg-secondary";
        
        /// <summary>
        /// Cognome (per soggetti) o parte del nome (per clienti PF)
        /// </summary>
        public string? Cognome { get; set; }
        
        /// <summary>
        /// Nome (per soggetti) o parte del nome (per clienti PF)
        /// </summary>
        public string? Nome { get; set; }
        
        public string? CodiceFiscale { get; set; }
        public string? DocumentoNumero { get; set; }
        public DateTime? DocumentoDataRilascio { get; set; }
        public string? DocumentoRilasciatoDa { get; set; }
        public DateTime? DocumentoScadenza { get; set; }
        
        /// <summary>
        /// Nome completo per visualizzazione
        /// </summary>
        public string NomeCompleto => IsCliente ? ClienteNome : $"{Cognome} {Nome}".Trim();
    }
}
