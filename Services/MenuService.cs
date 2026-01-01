using Microsoft.EntityFrameworkCore;
using StudioCG.Web.Data;
using StudioCG.Web.Models;
using StudioCG.Web.Models.Entita;

namespace StudioCG.Web.Services
{
    public interface IMenuService
    {
        Task<List<MenuItemDto>> GetMenuForUserAsync(string username);
    }

    /// <summary>
    /// DTO per rappresentare una voce di menu con sotto-voci
    /// </summary>
    public class MenuItemDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Url { get; set; }
        public string Icon { get; set; } = "fas fa-circle";
        public string? IconColor { get; set; }
        public string? Categoria { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsGroup { get; set; }
        public bool IsVisible { get; set; } = true;
        public string TipoVoce { get; set; } = "System";
        public int? ParentId { get; set; }
        public List<MenuItemDto> Children { get; set; } = new List<MenuItemDto>();
    }

    public class MenuService : IMenuService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPermissionService _permissionService;

        public MenuService(ApplicationDbContext context, IPermissionService permissionService)
        {
            _context = context;
            _permissionService = permissionService;
        }

        public async Task<List<MenuItemDto>> GetMenuForUserAsync(string username)
        {
            var isAdmin = string.Equals(username, "admin", StringComparison.OrdinalIgnoreCase);
            
            // Carica tutte le voci del menu attive
            var allVoci = await _context.VociMenu
                .Where(vm => vm.IsActive && vm.IsVisible)
                .OrderBy(vm => vm.DisplayOrder)
                .ToListAsync();

            // Costruisci la gerarchia
            var rootItems = allVoci.Where(v => v.ParentId == null).ToList();
            var result = new List<MenuItemDto>();

            foreach (var voce in rootItems)
            {
                var menuItem = await BuildMenuItemAsync(voce, allVoci, username, isAdmin);
                if (menuItem != null)
                {
                    result.Add(menuItem);
                }
            }

            return result.OrderBy(m => m.DisplayOrder).ToList();
        }

        private async Task<MenuItemDto?> BuildMenuItemAsync(VoceMenu voce, List<VoceMenu> allVoci, string username, bool isAdmin)
        {
            // Verifica visibilità in base al tipo
            switch (voce.TipoVoce)
            {
                case "System":
                    // Verifica permessi per le voci di sistema con URL
                    if (!string.IsNullOrEmpty(voce.Url))
                    {
                        // UTENTI solo per admin
                        if (voce.Categoria == "ADMIN" && !isAdmin)
                            return null;

                        // Controlla permesso
                        if (!isAdmin && !await _permissionService.UserHasPermissionAsync(username, voce.Url))
                            return null;
                    }
                    break;

                case "DynamicAttivita":
                    // Gruppo ATTIVITÀ - sempre visibile se ci sono attività
                    break;

                case "DynamicEntita":
                    // Gruppo ENTITÀ - sempre visibile se ci sono entità o admin
                    break;

                case "DynamicDatiUtenza":
                    // Gruppo DATI UTENZA - sempre visibile se ci sono pagine o admin
                    break;
            }

            var menuItem = new MenuItemDto
            {
                Id = voce.Id,
                Nome = voce.Nome,
                Url = voce.Url,
                Icon = voce.Icon,
                Categoria = voce.Categoria,
                DisplayOrder = voce.DisplayOrder,
                IsGroup = voce.IsGroup,
                IsVisible = true,
                TipoVoce = voce.TipoVoce,
                ParentId = voce.ParentId
            };

            // Aggiungi figli statici
            var children = allVoci.Where(v => v.ParentId == voce.Id).ToList();
            foreach (var child in children)
            {
                var childItem = await BuildMenuItemAsync(child, allVoci, username, isAdmin);
                if (childItem != null)
                {
                    menuItem.Children.Add(childItem);
                }
            }

            // Aggiungi voci dinamiche in base al tipo
            switch (voce.TipoVoce)
            {
                case "DynamicAttivita":
                    await AddDynamicAttivitaAsync(menuItem, username, isAdmin);
                    break;

                case "DynamicEntita":
                    await AddDynamicEntitaAsync(menuItem, username, isAdmin);
                    break;

                case "DynamicDatiUtenza":
                    await AddDynamicDatiUtenzaAsync(menuItem, username, isAdmin);
                    break;
            }

            // Per i gruppi, nascondi se non ci sono figli visibili
            if (voce.IsGroup && !menuItem.Children.Any())
            {
                // Eccezione: admin vede sempre i gruppi di sistema
                if (!isAdmin)
                    return null;
            }

            menuItem.Children = menuItem.Children.OrderBy(c => c.DisplayOrder).ToList();

            return menuItem;
        }

        private async Task AddDynamicAttivitaAsync(MenuItemDto parent, string username, bool isAdmin)
        {
            var annoCorrente = await _permissionService.GetAnnoCorrenteAsync();
            if (annoCorrente == null) return;

            var attivita = await _context.AttivitaAnnuali
                .Include(aa => aa.AttivitaTipo)
                .Where(aa => aa.AnnualitaFiscaleId == annoCorrente.Id && aa.IsActive)
                .OrderBy(aa => aa.AttivitaTipo!.DisplayOrder)
                .ThenBy(aa => aa.AttivitaTipo!.Nome)
                .ToListAsync();

            // Filtra per permessi utente
            foreach (var att in attivita)
            {
                var url = $"/Attivita/Tipo/{att.AttivitaTipoId}";
                
                if (!isAdmin && !await _permissionService.UserHasPermissionAsync(username, url))
                    continue;

                parent.Children.Add(new MenuItemDto
                {
                    Id = 1000 + att.AttivitaTipoId,
                    Nome = att.AttivitaTipo?.Nome ?? "Attività",
                    Url = url,
                    Icon = att.AttivitaTipo?.Icon ?? "fas fa-tasks",
                    DisplayOrder = 10 + att.AttivitaTipo?.DisplayOrder ?? 0,
                    TipoVoce = "DynamicItem"
                });
            }
        }

        private async Task AddDynamicEntitaAsync(MenuItemDto parent, string username, bool isAdmin)
        {
            var entita = await _context.EntitaDinamiche
                .Where(e => e.IsActive)
                .OrderBy(e => e.DisplayOrder)
                .ThenBy(e => e.Nome)
                .ToListAsync();

            foreach (var ent in entita)
            {
                var url = $"/Entita/Dati/{ent.Id}";

                if (!isAdmin && !await _permissionService.UserHasPermissionAsync(username, url))
                    continue;

                parent.Children.Add(new MenuItemDto
                {
                    Id = 2000 + ent.Id,
                    Nome = ent.Nome,
                    Url = url,
                    Icon = ent.Icon,
                    IconColor = ent.Colore,
                    DisplayOrder = 10 + ent.DisplayOrder,
                    TipoVoce = "DynamicItem"
                });
            }
        }

        private async Task AddDynamicDatiUtenzaAsync(MenuItemDto parent, string username, bool isAdmin)
        {
            var pages = await _context.DynamicPages
                .Where(p => p.IsActive)
                .OrderBy(p => p.DisplayOrder)
                .ThenBy(p => p.Name)
                .ToListAsync();

            // Raggruppa per categoria
            var riservati = pages.Where(p => p.Category == "DatiRiservati").ToList();
            var generali = pages.Where(p => p.Category == "DatiGenerali").ToList();

            // Aggiungi gruppo Dati Riservati
            if (riservati.Any() || isAdmin)
            {
                var riservatiGroup = new MenuItemDto
                {
                    Id = 5010,
                    Nome = "DATI Riservati",
                    Icon = "fas fa-lock",
                    DisplayOrder = 10,
                    IsGroup = true,
                    TipoVoce = "DynamicGroup"
                };

                foreach (var page in riservati)
                {
                    var url = $"/DynamicData/Page/{page.Id}";
                    if (!isAdmin && !await _permissionService.UserHasPermissionAsync(username, url))
                        continue;

                    riservatiGroup.Children.Add(new MenuItemDto
                    {
                        Id = 5100 + page.Id,
                        Nome = page.Name,
                        Url = url,
                        Icon = page.Icon,
                        DisplayOrder = page.DisplayOrder,
                        TipoVoce = "DynamicItem"
                    });
                }

                if (riservatiGroup.Children.Any() || isAdmin)
                    parent.Children.Add(riservatiGroup);
            }

            // Aggiungi gruppo Dati Generali
            if (generali.Any() || isAdmin)
            {
                var generaliGroup = new MenuItemDto
                {
                    Id = 5020,
                    Nome = "DATI Generali",
                    Icon = "fas fa-folder-open",
                    DisplayOrder = 20,
                    IsGroup = true,
                    TipoVoce = "DynamicGroup"
                };

                foreach (var page in generali)
                {
                    var url = $"/DynamicData/Page/{page.Id}";
                    if (!isAdmin && !await _permissionService.UserHasPermissionAsync(username, url))
                        continue;

                    generaliGroup.Children.Add(new MenuItemDto
                    {
                        Id = 5200 + page.Id,
                        Nome = page.Name,
                        Url = url,
                        Icon = page.Icon,
                        DisplayOrder = page.DisplayOrder,
                        TipoVoce = "DynamicItem"
                    });
                }

                if (generaliGroup.Children.Any() || isAdmin)
                    parent.Children.Add(generaliGroup);
            }
        }
    }
}
