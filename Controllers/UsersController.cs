using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioCG.Web.Data;
using StudioCG.Web.Filters;
using StudioCG.Web.Models;
using StudioCG.Web.Models.ViewModels;
using StudioCG.Web.Services;

namespace StudioCG.Web.Controllers
{
    [Authorize]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordService _passwordService;
        private readonly IPermissionService _permissionService;

        public UsersController(ApplicationDbContext context, IPasswordService passwordService, IPermissionService permissionService)
        {
            _context = context;
            _passwordService = passwordService;
            _permissionService = permissionService;
        }

        private async Task<bool> CanAccessAsync(string pageUrl)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return false;
            if (username.Equals("admin", StringComparison.OrdinalIgnoreCase)) return true;
            return await _permissionService.UserHasPermissionAsync(username, pageUrl);
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            if (!await CanAccessAsync("/Users"))
                return RedirectToAction("AccessDenied", "Account");
            var users = await _context.Users
                .OrderBy(u => u.Cognome)
                .ThenBy(u => u.Nome)
                .ToListAsync();
            return View(users);
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.UserPermissions)
                    .ThenInclude(up => up.Permission)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // GET: Users/Create
        public IActionResult Create()
        {
            return View(new UserViewModel());
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserViewModel model)
        {
            if (string.IsNullOrEmpty(model.Password))
            {
                ModelState.AddModelError("Password", "La password è obbligatoria per un nuovo utente.");
            }

            if (await _context.Users.AnyAsync(u => u.Username == model.Username))
            {
                ModelState.AddModelError("Username", "Questo nome utente è già in uso.");
            }

            if (ModelState.IsValid)
            {
                var user = new User
                {
                    Username = model.Username,
                    PasswordHash = _passwordService.HashPassword(model.Password!),
                    Nome = model.Nome,
                    Cognome = model.Cognome,
                    Email = model.Email,
                    IsActive = model.IsActive,
                    IsAdmin = model.IsAdmin,
                    DataCreazione = DateTime.Now
                };

                _context.Add(user);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Utente creato con successo.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(int? id)
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

            var model = new UserViewModel
            {
                Id = user.Id,
                Username = user.Username,
                Nome = user.Nome,
                Cognome = user.Cognome,
                Email = user.Email,
                IsActive = user.IsActive,
                IsAdmin = user.IsAdmin
            };

            return View(model);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UserViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            // Remove password validation if not changing
            if (string.IsNullOrEmpty(model.Password))
            {
                ModelState.Remove("Password");
                ModelState.Remove("ConfirmPassword");
            }

            var existingUser = await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == model.Username && u.Id != id);
            if (existingUser != null)
            {
                ModelState.AddModelError("Username", "Questo nome utente è già in uso.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _context.Users.FindAsync(id);
                    if (user == null)
                    {
                        return NotFound();
                    }

                    user.Username = model.Username;
                    user.Nome = model.Nome;
                    user.Cognome = model.Cognome;
                    user.Email = model.Email;
                    user.IsActive = model.IsActive;
                    user.IsAdmin = model.IsAdmin;

                    if (!string.IsNullOrEmpty(model.Password))
                    {
                        user.PasswordHash = _passwordService.HashPassword(model.Password);
                    }

                    _context.Update(user);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Utente aggiornato con successo.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await UserExists(model.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            // Prevent deletion of admin user
            if (user.Username == "admin")
            {
                TempData["Error"] = "Non è possibile eliminare l'utente amministratore principale.";
                return RedirectToAction(nameof(Index));
            }

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                // Prevent deletion of admin user
                if (user.Username == "admin")
                {
                    TempData["Error"] = "Non è possibile eliminare l'utente amministratore principale.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Utente eliminato con successo.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> UserExists(int id)
        {
            return await _context.Users.AnyAsync(e => e.Id == id);
        }
    }
}
