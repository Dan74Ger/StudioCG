using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioCG.Web.Data;
using StudioCG.Web.Filters;
using StudioCG.Web.Models;
using StudioCG.Web.Models.ViewModels;

namespace StudioCG.Web.Controllers
{
    [AdminOnly]
    public class PermissionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PermissionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Permissions/Utente/5
        [HttpGet]
        public async Task<IActionResult> Utente(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Non permettere di modificare i permessi dell'admin
            if (user.Username.ToLower() == "admin")
            {
                TempData["Error"] = "L'utente admin ha sempre accesso completo a tutte le pagine.";
                return RedirectToAction("Index", "Users");
            }

            var allPermissions = await _context.Permissions
                .OrderBy(p => p.DisplayOrder)
                .ToListAsync();

            var userPermissions = await _context.UserPermissions
                .Where(up => up.UserId == id)
                .ToListAsync();

            var viewModel = new UserPermissionsViewModel
            {
                UserId = user.Id,
                Username = user.Username,
                NomeCompleto = user.NomeCompleto,
                Permissions = allPermissions.Select(p =>
                {
                    var userPerm = userPermissions.FirstOrDefault(up => up.PermissionId == p.Id);
                    return new PermissionAssignmentViewModel
                    {
                        PermissionId = p.Id,
                        PageName = p.PageName,
                        PageUrl = p.PageUrl,
                        Description = p.Description,
                        Icon = p.Icon,
                        CanView = userPerm?.CanView ?? false,
                        CanEdit = userPerm?.CanEdit ?? false,
                        CanCreate = userPerm?.CanCreate ?? false,
                        CanDelete = userPerm?.CanDelete ?? false
                    };
                }).ToList()
            };

            return View(viewModel);
        }

        // POST: Permissions/Utente/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Utente(int id, UserPermissionsViewModel model)
        {
            if (id != model.UserId)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Non permettere di modificare i permessi dell'admin
            if (user.Username.ToLower() == "admin")
            {
                TempData["Error"] = "L'utente admin ha sempre accesso completo a tutte le pagine.";
                return RedirectToAction("Index", "Users");
            }

            // Rimuovi tutti i permessi esistenti
            var existingPermissions = await _context.UserPermissions
                .Where(up => up.UserId == id)
                .ToListAsync();
            _context.UserPermissions.RemoveRange(existingPermissions);

            // Aggiungi i nuovi permessi
            foreach (var perm in model.Permissions)
            {
                if (perm.CanView || perm.CanEdit || perm.CanCreate || perm.CanDelete)
                {
                    _context.UserPermissions.Add(new UserPermission
                    {
                        UserId = id,
                        PermissionId = perm.PermissionId,
                        CanView = perm.CanView,
                        CanEdit = perm.CanEdit,
                        CanCreate = perm.CanCreate,
                        CanDelete = perm.CanDelete
                    });
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Permessi aggiornati per l'utente {user.NomeCompleto}.";
            return RedirectToAction("Index", "Users");
        }

        // GET: Permissions/Pages - Gestione pagine/sezioni
        public async Task<IActionResult> Pages()
        {
            var permissions = await _context.Permissions
                .OrderBy(p => p.DisplayOrder)
                .ToListAsync();
            return View(permissions);
        }

        // GET: Permissions/CreatePage
        public IActionResult CreatePage()
        {
            return View(new Permission());
        }

        // POST: Permissions/CreatePage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePage(Permission model)
        {
            if (await _context.Permissions.AnyAsync(p => p.PageUrl == model.PageUrl))
            {
                ModelState.AddModelError("PageUrl", "Questo URL è già registrato.");
            }

            if (ModelState.IsValid)
            {
                _context.Permissions.Add(model);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Pagina creata con successo.";
                return RedirectToAction(nameof(Pages));
            }

            return View(model);
        }

        // GET: Permissions/EditPage/5
        public async Task<IActionResult> EditPage(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var permission = await _context.Permissions.FindAsync(id);
            if (permission == null)
            {
                return NotFound();
            }

            return View(permission);
        }

        // POST: Permissions/EditPage/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPage(int id, Permission model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            var existingPerm = await _context.Permissions.AsNoTracking()
                .FirstOrDefaultAsync(p => p.PageUrl == model.PageUrl && p.Id != id);
            if (existingPerm != null)
            {
                ModelState.AddModelError("PageUrl", "Questo URL è già registrato.");
            }

            if (ModelState.IsValid)
            {
                _context.Update(model);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Pagina aggiornata con successo.";
                return RedirectToAction(nameof(Pages));
            }

            return View(model);
        }

        // POST: Permissions/DeletePage/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePage(int id)
        {
            var permission = await _context.Permissions.FindAsync(id);
            if (permission != null)
            {
                _context.Permissions.Remove(permission);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Pagina eliminata con successo.";
            }
            return RedirectToAction(nameof(Pages));
        }
    }
}
