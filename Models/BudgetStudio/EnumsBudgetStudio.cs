using System.ComponentModel.DataAnnotations;

namespace StudioCG.Web.Models.BudgetStudio
{
    public enum MetodoPagamentoBudget
    {
        [Display(Name = "Bonifico")]
        Bonifico = 0,

        [Display(Name = "Ri.Ba.")]
        Riba = 1,

        [Display(Name = "Contanti")]
        Contanti = 2,

        [Display(Name = "Carta")]
        Carta = 3,

        [Display(Name = "SDD")]
        Sdd = 4,

        [Display(Name = "Altro")]
        Altro = 5
    }
}


