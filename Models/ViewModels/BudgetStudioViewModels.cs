using StudioCG.Web.Models.BudgetStudio;

namespace StudioCG.Web.Models.ViewModels
{
    public enum BudgetPagateFiltro
    {
        NonPagate = 0,
        Pagate = 1,
        Tutte = 2
    }

    public class BudgetStudioViewModel
    {
        public int Anno { get; set; }
        public int MeseDa { get; set; } = 1;
        public int MeseA { get; set; } = 12;
        public BudgetPagateFiltro PagateFiltro { get; set; } = BudgetPagateFiltro.NonPagate;
        public bool NascondiVuote { get; set; } = true; // Default: nascondi voci senza importi

        // Macro voci (categorie) - TUTTE per anagrafica
        public List<MacroVoceBudget> MacroVoci { get; set; } = new();
        
        // Voci analitiche - TUTTE per anagrafica
        public List<VoceSpesaBudget> VociTutte { get; set; } = new();
        
        // Voci analitiche (filtrate per prospetto)
        public List<VoceSpesaBudget> Voci { get; set; } = new();
        
        // Voci senza macro voce assegnata (per prospetto)
        public List<VoceSpesaBudget> VociSenzaMacro { get; set; } = new();
        
        public List<BudgetSpesaMensile> RigheMensili { get; set; } = new();

        public Dictionary<(int voceId, int mese), BudgetSpesaMensile> Map { get; set; } = new();
        public Dictionary<int, decimal> TotaliMese { get; set; } = new();
        public decimal TotaleAnno { get; set; }

        // Banche e Cash Flow
        public List<BancaBudget> Banche { get; set; } = new();
        public List<SaldoBancaMese> SaldiBanche { get; set; } = new();
        
        // Mappa: (bancaId, mese) -> saldo
        public Dictionary<(int bancaId, int mese), decimal> MapSaldiBanche { get; set; } = new();
        
        // Totale saldi banche per mese
        public Dictionary<int, decimal> TotaliBancheMese { get; set; } = new();
        
        // Cash Flow: saldo iniziale per mese (primo mese = totale banche, altri = riporto)
        public Dictionary<int, decimal> SaldoInizialeMese { get; set; } = new();
        
        // Cash Flow: differenza per mese (saldo iniziale - spese)
        public Dictionary<int, decimal> DifferenzaMese { get; set; } = new();
    }
}


