using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioCG.Web.Data;
using StudioCG.Web.Filters;
using StudioCG.Web.Models;
using System.Text.RegularExpressions;

namespace StudioCG.Web.Controllers
{
    [AdminOnly]
    public class AttivitaTipiController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AttivitaTipiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: AttivitaTipi
        public async Task<IActionResult> Index()
        {
            var tipi = await _context.AttivitaTipi
                .Include(t => t.Campi)
                .Include(t => t.AttivitaAnnuali)
                    .ThenInclude(aa => aa.AnnualitaFiscale)
                .OrderBy(t => t.DisplayOrder)
                .ThenBy(t => t.Nome)
                .ToListAsync();
            return View(tipi);
        }

        // GET: AttivitaTipi/Create
        public IActionResult Create()
        {
            return View(new AttivitaTipo());
        }

        // POST: AttivitaTipi/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AttivitaTipo model)
        {
            if (await _context.AttivitaTipi.AnyAsync(t => t.Nome == model.Nome))
            {
                ModelState.AddModelError("Nome", "Esiste già un tipo di attività con questo nome.");
            }

            if (ModelState.IsValid)
            {
                model.CreatedAt = DateTime.Now;
                _context.AttivitaTipi.Add(model);
                await _context.SaveChangesAsync();

                // Crea automaticamente il Permission per gestire i permessi utente
                var permission = new Permission
                {
                    PageName = model.Nome,
                    Description = $"Accesso all'attività: {model.Nome}",
                    PageUrl = $"/Attivita/Tipo/{model.Id}",
                    Category = "ATTIVITA",
                    Icon = model.Icon ?? "fas fa-tasks",
                    ShowInMenu = false,
                    DisplayOrder = model.DisplayOrder
                };
                _context.Permissions.Add(permission);
                await _context.SaveChangesAsync();

                // Abbina automaticamente all'anno corrente se esiste
                var annoCorrente = await _context.AnnualitaFiscali.FirstOrDefaultAsync(a => a.IsCurrent);
                if (annoCorrente != null)
                {
                    var attivitaAnnuale = new AttivitaAnnuale
                    {
                        AttivitaTipoId = model.Id,
                        AnnualitaFiscaleId = annoCorrente.Id,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };
                    _context.AttivitaAnnuali.Add(attivitaAnnuale);
                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = $"Tipo attività '{model.Nome}' creato. Ora aggiungi i campi.";
                return RedirectToAction(nameof(Campi), new { id = model.Id });
            }
            return View(model);
        }

        // GET: AttivitaTipi/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var tipo = await _context.AttivitaTipi.FindAsync(id);
            if (tipo == null) return NotFound();

            return View(tipo);
        }

        // POST: AttivitaTipi/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AttivitaTipo model)
        {
            if (id != model.Id) return NotFound();

            var existingTipo = await _context.AttivitaTipi.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Nome == model.Nome && t.Id != id);
            if (existingTipo != null)
            {
                ModelState.AddModelError("Nome", "Esiste già un tipo di attività con questo nome.");
            }

            if (ModelState.IsValid)
            {
                var tipo = await _context.AttivitaTipi.FindAsync(id);
                if (tipo == null) return NotFound();

                tipo.Nome = model.Nome;
                tipo.Descrizione = model.Descrizione;
                tipo.Icon = model.Icon;
                tipo.DisplayOrder = model.DisplayOrder;
                tipo.IsActive = model.IsActive;

                // Aggiorna anche il Permission corrispondente
                var permissionUrl = $"/Attivita/Tipo/{id}";
                var permission = await _context.Permissions.FirstOrDefaultAsync(p => p.PageUrl == permissionUrl);
                if (permission != null)
                {
                    permission.PageName = model.Nome;
                    permission.Description = $"Accesso all'attività: {model.Nome}";
                    permission.Icon = model.Icon ?? "fas fa-tasks";
                    permission.DisplayOrder = model.DisplayOrder;
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Tipo attività aggiornato con successo.";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // GET: AttivitaTipi/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var tipo = await _context.AttivitaTipi
                .Include(t => t.Campi)
                .Include(t => t.AttivitaAnnuali)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tipo == null) return NotFound();

            return View(tipo);
        }

        // POST: AttivitaTipi/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tipo = await _context.AttivitaTipi.FindAsync(id);
            if (tipo != null)
            {
                // Elimina il Permission corrispondente
                var permissionUrl = $"/Attivita/Tipo/{id}";
                var permission = await _context.Permissions.FirstOrDefaultAsync(p => p.PageUrl == permissionUrl);
                if (permission != null)
                {
                    var userPermissions = _context.UserPermissions.Where(up => up.PermissionId == permission.Id);
                    _context.UserPermissions.RemoveRange(userPermissions);
                    _context.Permissions.Remove(permission);
                }

                _context.AttivitaTipi.Remove(tipo);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Tipo attività eliminato con successo.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ==================== GESTIONE CAMPI ====================

        // GET: AttivitaTipi/Campi/5
        public async Task<IActionResult> Campi(int? id)
        {
            if (id == null) return NotFound();

            var tipo = await _context.AttivitaTipi
                .Include(t => t.Campi.OrderBy(c => c.DisplayOrder))
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tipo == null) return NotFound();

            return View(tipo);
        }

        // POST: AttivitaTipi/AddCampo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCampo(AttivitaCampo model)
        {
            // Genera Name da Label
            model.Name = GenerateFieldName(model.Label);

            if (await _context.AttivitaCampi.AnyAsync(c => c.AttivitaTipoId == model.AttivitaTipoId && c.Name == model.Name))
            {
                TempData["Error"] = "Esiste già un campo con un nome simile.";
                return RedirectToAction(nameof(Campi), new { id = model.AttivitaTipoId });
            }

            // Imposta ordine
            var maxOrder = await _context.AttivitaCampi
                .Where(c => c.AttivitaTipoId == model.AttivitaTipoId)
                .MaxAsync(c => (int?)c.DisplayOrder) ?? 0;
            model.DisplayOrder = maxOrder + 1;

            _context.AttivitaCampi.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Campo '{model.Label}' aggiunto.";
            return RedirectToAction(nameof(Campi), new { id = model.AttivitaTipoId });
        }

        // POST: AttivitaTipi/DeleteCampo/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCampo(int id)
        {
            var campo = await _context.AttivitaCampi.FindAsync(id);
            if (campo != null)
            {
                var tipoId = campo.AttivitaTipoId;
                _context.AttivitaCampi.Remove(campo);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Campo eliminato.";
                return RedirectToAction(nameof(Campi), new { id = tipoId });
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: AttivitaTipi/MoveCampoUp/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveCampoUp(int id)
        {
            var campo = await _context.AttivitaCampi.FindAsync(id);
            if (campo != null)
            {
                var prevCampo = await _context.AttivitaCampi
                    .Where(c => c.AttivitaTipoId == campo.AttivitaTipoId && c.DisplayOrder < campo.DisplayOrder)
                    .OrderByDescending(c => c.DisplayOrder)
                    .FirstOrDefaultAsync();

                if (prevCampo != null)
                {
                    var tempOrder = campo.DisplayOrder;
                    campo.DisplayOrder = prevCampo.DisplayOrder;
                    prevCampo.DisplayOrder = tempOrder;
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction(nameof(Campi), new { id = campo.AttivitaTipoId });
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: AttivitaTipi/MoveCampoDown/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveCampoDown(int id)
        {
            var campo = await _context.AttivitaCampi.FindAsync(id);
            if (campo != null)
            {
                var nextCampo = await _context.AttivitaCampi
                    .Where(c => c.AttivitaTipoId == campo.AttivitaTipoId && c.DisplayOrder > campo.DisplayOrder)
                    .OrderBy(c => c.DisplayOrder)
                    .FirstOrDefaultAsync();

                if (nextCampo != null)
                {
                    var tempOrder = campo.DisplayOrder;
                    campo.DisplayOrder = nextCampo.DisplayOrder;
                    nextCampo.DisplayOrder = tempOrder;
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction(nameof(Campi), new { id = campo.AttivitaTipoId });
            }
            return RedirectToAction(nameof(Index));
        }

        // ==================== GESTIONE ABBINAMENTO ANNI ====================

        // GET: AttivitaTipi/Anni/5
        public async Task<IActionResult> Anni(int? id)
        {
            if (id == null) return NotFound();

            var tipo = await _context.AttivitaTipi
                .Include(t => t.AttivitaAnnuali)
                    .ThenInclude(aa => aa.AnnualitaFiscale)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tipo == null) return NotFound();

            ViewBag.TuttiAnni = await _context.AnnualitaFiscali
                .Where(a => a.IsActive)
                .OrderByDescending(a => a.Anno)
                .ToListAsync();

            return View(tipo);
        }

        // POST: AttivitaTipi/ToggleAnno
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAnno(int tipoId, int annualitaId, bool assegna)
        {
            if (assegna)
            {
                var exists = await _context.AttivitaAnnuali
                    .AnyAsync(aa => aa.AttivitaTipoId == tipoId && aa.AnnualitaFiscaleId == annualitaId);

                if (!exists)
                {
                    var attivitaAnnuale = new AttivitaAnnuale
                    {
                        AttivitaTipoId = tipoId,
                        AnnualitaFiscaleId = annualitaId,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };
                    _context.AttivitaAnnuali.Add(attivitaAnnuale);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Anno abbinato all'attività.";
                }
            }
            else
            {
                var attivitaAnnuale = await _context.AttivitaAnnuali
                    .FirstOrDefaultAsync(aa => aa.AttivitaTipoId == tipoId && aa.AnnualitaFiscaleId == annualitaId);

                if (attivitaAnnuale != null)
                {
                    _context.AttivitaAnnuali.Remove(attivitaAnnuale);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Anno rimosso dall'attività.";
                }
            }

            return RedirectToAction(nameof(Anni), new { id = tipoId });
        }

        private string GenerateFieldName(string label)
        {
            return Regex.Replace(label, @"[^a-zA-Z0-9]", "");
        }
    }
}

