namespace StudioCG.Web.Models.Fatturazione
{
    /// <summary>
    /// DTO per la suddivisione incasso tra professionisti
    /// </summary>
    public class ProfessionistaIncassoDto
    {
        public int UtenteId { get; set; }
        public bool Selezionato { get; set; }
        public string? Importo { get; set; }
    }
}

