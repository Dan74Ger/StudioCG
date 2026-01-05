using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioCG.Web.Data;
using StudioCG.Web.Filters;
using StudioCG.Web.Models;
using StudioCG.Web.Services;

namespace StudioCG.Web.Controllers
{
    [Authorize]
    public class MenuController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPermissionService _permissionService;

        public MenuController(ApplicationDbContext context, IPermissionService permissionService)
        {
            _context = context;
            _permissionService = permissionService;
        }

        private async Task<bool> CanAccessAsync(string pageUrl)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return false;
            if (username.Equals("admin", StringComparison.OrdinalIgnoreCase)) return true;
            return await _permissionService.UserHasPermissionAsync(username, pageUrl);
        }

        // GET: Menu
        public async Task<IActionResult> Index()
        {
            if (!await CanAccessAsync("/Menu"))
                return RedirectToAction("AccessDenied", "Account");

            // Inizializza il menu se non esiste
            if (!await _context.VociMenu.AnyAsync())
            {
                await InizializzaMenuDefault();
            }

            var voci = await _context.VociMenu
                .Include(v => v.Children.OrderBy(c => c.DisplayOrder))
                .Where(v => v.ParentId == null) // Solo voci radice
                .OrderBy(v => v.DisplayOrder)
                .ToListAsync();

            // Carica anche le voci dinamiche per visualizzarle
            var annoCorrente = await _permissionService.GetAnnoCorrenteAsync();
            
            // Attività dell'anno corrente
            ViewBag.Attivita = annoCorrente != null 
                ? await _context.AttivitaAnnuali
                    .Include(a => a.AttivitaTipo)
                    .Where(a => a.AnnualitaFiscaleId == annoCorrente.Id && a.IsActive)
                    .OrderBy(a => a.AttivitaTipo!.DisplayOrder)
                    .ToListAsync()
                : new List<AttivitaAnnuale>();
            
            // Entità dinamiche
            ViewBag.EntitaDinamiche = await _context.EntitaDinamiche
                .Where(e => e.IsActive)
                .OrderBy(e => e.DisplayOrder)
                .ToListAsync();
            
            // Pagine dinamiche
            ViewBag.PagineDinamiche = await _context.DynamicPages
                .Where(p => p.IsActive)
                .OrderBy(p => p.Category)
                .ThenBy(p => p.DisplayOrder)
                .ToListAsync();

            return View(voci);
        }

        // POST: Menu/AddVoce
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVoce(VoceMenu voce)
        {
            if (string.IsNullOrWhiteSpace(voce.Nome))
            {
                TempData["Error"] = "Il nome della voce è obbligatorio.";
                return RedirectToAction(nameof(Index));
            }

            var maxOrder = await _context.VociMenu
                .Where(v => v.ParentId == voce.ParentId)
                .MaxAsync(v => (int?)v.DisplayOrder) ?? 0;

            voce.DisplayOrder = maxOrder + 1;
            voce.CreatedAt = DateTime.Now;
            voce.TipoVoce = "Custom";

            _context.VociMenu.Add(voce);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Voce '{voce.Nome}' aggiunta con successo.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Menu/EditVoce
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVoce(int id, string nome, string? url, string icon, bool isVisible)
        {
            var voce = await _context.VociMenu.FindAsync(id);
            if (voce == null)
            {
                TempData["Error"] = "Voce non trovata.";
                return RedirectToAction(nameof(Index));
            }

            voce.Nome = nome;
            voce.Url = url;
            voce.Icon = icon;
            voce.IsVisible = isVisible;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Voce '{voce.Nome}' aggiornata.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Menu/DeleteVoce
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVoce(int id)
        {
            var voce = await _context.VociMenu
                .Include(v => v.Children)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (voce == null)
            {
                TempData["Error"] = "Voce non trovata.";
                return RedirectToAction(nameof(Index));
            }

            // Impedisci eliminazione voci di sistema
            if (voce.TipoVoce == "System")
            {
                TempData["Error"] = "Non è possibile eliminare voci di sistema.";
                return RedirectToAction(nameof(Index));
            }

            // Elimina anche le sotto-voci
            if (voce.Children.Any())
            {
                _context.VociMenu.RemoveRange(voce.Children);
            }

            _context.VociMenu.Remove(voce);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Voce '{voce.Nome}' eliminata.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Menu/MoveUp
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveUp(int id)
        {
            var voce = await _context.VociMenu.FindAsync(id);
            if (voce == null) return RedirectToAction(nameof(Index));

            var vocePrecedente = await _context.VociMenu
                .Where(v => v.ParentId == voce.ParentId && v.DisplayOrder < voce.DisplayOrder)
                .OrderByDescending(v => v.DisplayOrder)
                .FirstOrDefaultAsync();

            if (vocePrecedente != null)
            {
                var temp = voce.DisplayOrder;
                voce.DisplayOrder = vocePrecedente.DisplayOrder;
                vocePrecedente.DisplayOrder = temp;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Menu/MoveDown
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveDown(int id)
        {
            var voce = await _context.VociMenu.FindAsync(id);
            if (voce == null) return RedirectToAction(nameof(Index));

            var voceSuccessiva = await _context.VociMenu
                .Where(v => v.ParentId == voce.ParentId && v.DisplayOrder > voce.DisplayOrder)
                .OrderBy(v => v.DisplayOrder)
                .FirstOrDefaultAsync();

            if (voceSuccessiva != null)
            {
                var temp = voce.DisplayOrder;
                voce.DisplayOrder = voceSuccessiva.DisplayOrder;
                voceSuccessiva.DisplayOrder = temp;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Menu/ToggleVisibility
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVisibility(int id)
        {
            var voce = await _context.VociMenu.FindAsync(id);
            if (voce == null) return RedirectToAction(nameof(Index));

            voce.IsVisible = !voce.IsVisible;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // Inizializza voci menu di default
        private Task InizializzaMenuDefault()
        {
            // Lo script SQL ha già inizializzato il menu
            // Questo metodo è solo per backup
            return Task.CompletedTask;
        }
    }
}
