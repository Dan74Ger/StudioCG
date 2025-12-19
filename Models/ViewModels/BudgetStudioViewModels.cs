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

        public List<VoceSpesaBudget> Voci { get; set; } = new();
        public List<BudgetSpesaMensile> RigheMensili { get; set; } = new();

        public Dictionary<(int voceId, int mese), BudgetSpesaMensile> Map { get; set; } = new();
        public Dictionary<int, decimal> TotaliMese { get; set; } = new();
        public decimal TotaleAnno { get; set; }
    }
}


