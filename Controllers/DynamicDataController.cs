using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioCG.Web.Data;
using StudioCG.Web.Models;
using StudioCG.Web.Services;

namespace StudioCG.Web.Controllers
{
    [Authorize]
    public class DynamicDataController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPermissionService _permissionService;

        public DynamicDataController(ApplicationDbContext context, IPermissionService permissionService)
        {
            _context = context;
            _permissionService = permissionService;
        }

        private async Task<int> GetCurrentUserIdAsync()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return 0;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            return user?.Id ?? 0;
        }

        private string GetPageUrl(int pageId) => $"/DynamicData/Page/{pageId}";

        // GET: DynamicData/Page/5
        public async Task<IActionResult> Page(int? id)
        {
            if (id == null) return NotFound();

            var page = await _context.DynamicPages
                .Include(p => p.Fields.OrderBy(f => f.DisplayOrder))
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (page == null) return NotFound();

            // Verifica permessi
            var userId = await GetCurrentUserIdAsync();
            var pageUrl = GetPageUrl(id.Value);
            
            var canView = await _permissionService.CanViewAsync(userId, pageUrl);
            var canCreate = await _permissionService.CanCreateAsync(userId, pageUrl);
            var canEdit = await _permissionService.CanEditAsync(userId, pageUrl);
            var canDelete = await _permissionService.CanDeleteAsync(userId, pageUrl);

            // Se non può visualizzare, accesso negato
            if (!canView)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            // Carica i record con i valori
            var records = await _context.DynamicRecords
                .Where(r => r.DynamicPageId == id)
                .Include(r => r.FieldValues)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.Records = records;
            ViewBag.CanCreate = canCreate;
            ViewBag.CanEdit = canEdit;
            ViewBag.CanDelete = canDelete;
            ViewData["Title"] = page.Name;

            return View();
        }

        // GET: DynamicData/Create/5
        public async Task<IActionResult> Create(int? id)
        {
            if (id == null) return NotFound();

            // Verifica permesso di creazione
            var userId = await GetCurrentUserIdAsync();
            var pageUrl = GetPageUrl(id.Value);
            if (!await _permissionService.CanCreateAsync(userId, pageUrl))
            {
                TempData["Error"] = "Non hai i permessi per creare nuovi record in questa pagina.";
                return RedirectToAction(nameof(Page), new { id });
            }

            var page = await _context.DynamicPages
                .Include(p => p.Fields.OrderBy(f => f.DisplayOrder))
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (page == null) return NotFound();

            ViewBag.Page = page;
            ViewData["Title"] = $"Nuovo - {page.Name}";

            return View();
        }

        // POST: DynamicData/Create/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int id, IFormCollection form)
        {
            // Verifica permesso di creazione
            var userId = await GetCurrentUserIdAsync();
            var pageUrl = GetPageUrl(id);
            if (!await _permissionService.CanCreateAsync(userId, pageUrl))
            {
                TempData["Error"] = "Non hai i permessi per creare nuovi record in questa pagina.";
                return RedirectToAction(nameof(Page), new { id });
            }

            var page = await _context.DynamicPages
                .Include(p => p.Fields)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (page == null) return NotFound();

            // Valida campi obbligatori
            foreach (var field in page.Fields.Where(f => f.IsRequired))
            {
                var value = form[$"field_{field.Id}"].ToString();
                if (string.IsNullOrWhiteSpace(value))
                {
                    ModelState.AddModelError($"field_{field.Id}", $"Il campo '{field.Label}' è obbligatorio.");
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Page = page;
                ViewBag.FormValues = form;
                ViewData["Title"] = $"Nuovo - {page.Name}";
                return View();
            }

            // Crea il record
            var record = new DynamicRecord
            {
                DynamicPageId = id,
                CreatedAt = DateTime.Now,
                CreatedBy = User.Identity?.Name
            };

            _context.DynamicRecords.Add(record);
            await _context.SaveChangesAsync();

            // Salva i valori dei campi
            foreach (var field in page.Fields)
            {
                var value = form[$"field_{field.Id}"].ToString();
                
                // Per checkbox, gestisci il valore
                if (field.FieldType == FieldTypes.Boolean)
                {
                    value = form[$"field_{field.Id}"].Contains("true") ? "true" : "false";
                }

                var fieldValue = new DynamicFieldValue
                {
                    DynamicRecordId = record.Id,
                    DynamicFieldId = field.Id,
                    Value = value
                };
                _context.DynamicFieldValues.Add(fieldValue);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Record creato con successo.";
            return RedirectToAction(nameof(Page), new { id });
        }

        // GET: DynamicData/Edit/5?recordId=10
        public async Task<IActionResult> Edit(int? id, int? recordId)
        {
            if (id == null || recordId == null) return NotFound();

            // Verifica permesso di modifica
            var userId = await GetCurrentUserIdAsync();
            var pageUrl = GetPageUrl(id.Value);
            if (!await _permissionService.CanEditAsync(userId, pageUrl))
            {
                TempData["Error"] = "Non hai i permessi per modificare i record in questa pagina.";
                return RedirectToAction(nameof(Page), new { id });
            }

            var page = await _context.DynamicPages
                .Include(p => p.Fields.OrderBy(f => f.DisplayOrder))
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (page == null) return NotFound();

            var record = await _context.DynamicRecords
                .Include(r => r.FieldValues)
                .FirstOrDefaultAsync(r => r.Id == recordId && r.DynamicPageId == id);

            if (record == null) return NotFound();

            ViewBag.Page = page;
            ViewBag.Record = record;
            ViewData["Title"] = $"Modifica - {page.Name}";

            return View();
        }

        // POST: DynamicData/Edit/5?recordId=10
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, int recordId, IFormCollection form)
        {
            // Verifica permesso di modifica
            var userId = await GetCurrentUserIdAsync();
            var pageUrl = GetPageUrl(id);
            if (!await _permissionService.CanEditAsync(userId, pageUrl))
            {
                TempData["Error"] = "Non hai i permessi per modificare i record in questa pagina.";
                return RedirectToAction(nameof(Page), new { id });
            }

            var page = await _context.DynamicPages
                .Include(p => p.Fields)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (page == null) return NotFound();

            var record = await _context.DynamicRecords
                .Include(r => r.FieldValues)
                .FirstOrDefaultAsync(r => r.Id == recordId && r.DynamicPageId == id);

            if (record == null) return NotFound();

            // Valida campi obbligatori
            foreach (var field in page.Fields.Where(f => f.IsRequired))
            {
                var value = form[$"field_{field.Id}"].ToString();
                if (string.IsNullOrWhiteSpace(value))
                {
                    ModelState.AddModelError($"field_{field.Id}", $"Il campo '{field.Label}' è obbligatorio.");
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Page = page;
                ViewBag.Record = record;
                ViewBag.FormValues = form;
                ViewData["Title"] = $"Modifica - {page.Name}";
                return View();
            }

            // Aggiorna i valori
            record.UpdatedAt = DateTime.Now;

            foreach (var field in page.Fields)
            {
                var value = form[$"field_{field.Id}"].ToString();
                
                if (field.FieldType == FieldTypes.Boolean)
                {
                    value = form[$"field_{field.Id}"].Contains("true") ? "true" : "false";
                }

                var fieldValue = record.FieldValues.FirstOrDefault(fv => fv.DynamicFieldId == field.Id);
                if (fieldValue != null)
                {
                    fieldValue.Value = value;
                }
                else
                {
                    _context.DynamicFieldValues.Add(new DynamicFieldValue
                    {
                        DynamicRecordId = record.Id,
                        DynamicFieldId = field.Id,
                        Value = value
                    });
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Record aggiornato con successo.";
            return RedirectToAction(nameof(Page), new { id });
        }

        // GET: DynamicData/Delete/5?recordId=10
        public async Task<IActionResult> Delete(int? id, int? recordId)
        {
            if (id == null || recordId == null) return NotFound();

            // Verifica permesso di eliminazione
            var userId = await GetCurrentUserIdAsync();
            var pageUrl = GetPageUrl(id.Value);
            if (!await _permissionService.CanDeleteAsync(userId, pageUrl))
            {
                TempData["Error"] = "Non hai i permessi per eliminare i record in questa pagina.";
                return RedirectToAction(nameof(Page), new { id });
            }

            var page = await _context.DynamicPages
                .Include(p => p.Fields.OrderBy(f => f.DisplayOrder))
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (page == null) return NotFound();

            var record = await _context.DynamicRecords
                .Include(r => r.FieldValues)
                .FirstOrDefaultAsync(r => r.Id == recordId && r.DynamicPageId == id);

            if (record == null) return NotFound();

            ViewBag.Page = page;
            ViewBag.Record = record;
            ViewData["Title"] = $"Elimina - {page.Name}";

            return View();
        }

        // POST: DynamicData/Delete/5?recordId=10
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, int recordId)
        {
            // Verifica permesso di eliminazione
            var userId = await GetCurrentUserIdAsync();
            var pageUrl = GetPageUrl(id);
            if (!await _permissionService.CanDeleteAsync(userId, pageUrl))
            {
                TempData["Error"] = "Non hai i permessi per eliminare i record in questa pagina.";
                return RedirectToAction(nameof(Page), new { id });
            }

            var record = await _context.DynamicRecords.FindAsync(recordId);
            if (record != null && record.DynamicPageId == id)
            {
                _context.DynamicRecords.Remove(record);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Record eliminato con successo.";
            }
            return RedirectToAction(nameof(Page), new { id });
        }
    }
}

