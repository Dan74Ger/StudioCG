using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioCG.Web.Data;
using StudioCG.Web.Filters;
using StudioCG.Web.Models;
using System.Text.RegularExpressions;

namespace StudioCG.Web.Controllers
{
    [AdminOnly]
    public class DynamicPagesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DynamicPagesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: DynamicPages
        public async Task<IActionResult> Index()
        {
            var pages = await _context.DynamicPages
                .Include(p => p.Fields)
                .OrderBy(p => p.Category)
                .ThenBy(p => p.DisplayOrder)
                .ThenBy(p => p.Name)
                .ToListAsync();
            return View(pages);
        }

        // GET: DynamicPages/Create
        public IActionResult Create()
        {
            return View(new DynamicPage());
        }

        // POST: DynamicPages/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DynamicPage model)
        {
            // Genera TableName da Name
            model.TableName = GenerateTableName(model.Name);

            if (await _context.DynamicPages.AnyAsync(p => p.TableName == model.TableName))
            {
                ModelState.AddModelError("Name", "Esiste già una pagina con un nome simile.");
            }

            if (ModelState.IsValid)
            {
                model.CreatedAt = DateTime.Now;
                _context.DynamicPages.Add(model);
                await _context.SaveChangesAsync();

                // Crea anche il Permission corrispondente per gestire i permessi utente
                var permission = new Permission
                {
                    PageName = model.Name,
                    Description = $"Accesso alla pagina dinamica: {model.Name}",
                    PageUrl = $"/DynamicData/Page/{model.Id}",
                    Category = model.Category == "DatiRiservati" ? "DATI Riservati" : "DATI Generali",
                    Icon = model.Icon ?? "fas fa-file",
                    ShowInMenu = false, // Le pagine dinamiche sono mostrate dal menu dinamico
                    DisplayOrder = model.DisplayOrder
                };
                _context.Permissions.Add(permission);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Pagina '{model.Name}' creata. Ora aggiungi i campi.";
                return RedirectToAction(nameof(Fields), new { id = model.Id });
            }

            return View(model);
        }

        // GET: DynamicPages/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var page = await _context.DynamicPages.FindAsync(id);
            if (page == null) return NotFound();

            return View(page);
        }

        // POST: DynamicPages/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DynamicPage model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var page = await _context.DynamicPages.FindAsync(id);
                if (page == null) return NotFound();

                page.Name = model.Name;
                page.Description = model.Description;
                page.Category = model.Category;
                page.Icon = model.Icon;
                page.DisplayOrder = model.DisplayOrder;
                page.IsActive = model.IsActive;

                await _context.SaveChangesAsync();
                TempData["Success"] = "Pagina aggiornata con successo.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // GET: DynamicPages/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var page = await _context.DynamicPages
                .Include(p => p.Fields)
                .Include(p => p.Records)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (page == null) return NotFound();

            return View(page);
        }

        // POST: DynamicPages/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var page = await _context.DynamicPages.FindAsync(id);
            if (page != null)
            {
                // Elimina anche il Permission corrispondente
                var pageUrl = $"/DynamicData/Page/{id}";
                var permission = await _context.Permissions.FirstOrDefaultAsync(p => p.PageUrl == pageUrl);
                if (permission != null)
                {
                    // Elimina prima i UserPermission collegati
                    var userPermissions = _context.UserPermissions.Where(up => up.PermissionId == permission.Id);
                    _context.UserPermissions.RemoveRange(userPermissions);
                    _context.Permissions.Remove(permission);
                }

                _context.DynamicPages.Remove(page);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Pagina eliminata con successo.";
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: DynamicPages/Fields/5
        public async Task<IActionResult> Fields(int? id)
        {
            if (id == null) return NotFound();

            var page = await _context.DynamicPages
                .Include(p => p.Fields.OrderBy(f => f.DisplayOrder))
                .FirstOrDefaultAsync(p => p.Id == id);

            if (page == null) return NotFound();

            return View(page);
        }

        // POST: DynamicPages/AddField
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddField(DynamicField model)
        {
            // Genera Name da Label
            model.Name = GenerateFieldName(model.Label);

            if (await _context.DynamicFields.AnyAsync(f => f.DynamicPageId == model.DynamicPageId && f.Name == model.Name))
            {
                TempData["Error"] = "Esiste già un campo con un nome simile.";
                return RedirectToAction(nameof(Fields), new { id = model.DynamicPageId });
            }

            // Imposta ordine
            var maxOrder = await _context.DynamicFields
                .Where(f => f.DynamicPageId == model.DynamicPageId)
                .MaxAsync(f => (int?)f.DisplayOrder) ?? 0;
            model.DisplayOrder = maxOrder + 1;

            _context.DynamicFields.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Campo '{model.Label}' aggiunto.";
            return RedirectToAction(nameof(Fields), new { id = model.DynamicPageId });
        }

        // POST: DynamicPages/DeleteField/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteField(int id)
        {
            var field = await _context.DynamicFields.FindAsync(id);
            if (field != null)
            {
                var pageId = field.DynamicPageId;
                _context.DynamicFields.Remove(field);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Campo eliminato.";
                return RedirectToAction(nameof(Fields), new { id = pageId });
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: DynamicPages/MoveFieldUp/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveFieldUp(int id)
        {
            var field = await _context.DynamicFields.FindAsync(id);
            if (field != null)
            {
                var prevField = await _context.DynamicFields
                    .Where(f => f.DynamicPageId == field.DynamicPageId && f.DisplayOrder < field.DisplayOrder)
                    .OrderByDescending(f => f.DisplayOrder)
                    .FirstOrDefaultAsync();

                if (prevField != null)
                {
                    var tempOrder = field.DisplayOrder;
                    field.DisplayOrder = prevField.DisplayOrder;
                    prevField.DisplayOrder = tempOrder;
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction(nameof(Fields), new { id = field.DynamicPageId });
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: DynamicPages/MoveFieldDown/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveFieldDown(int id)
        {
            var field = await _context.DynamicFields.FindAsync(id);
            if (field != null)
            {
                var nextField = await _context.DynamicFields
                    .Where(f => f.DynamicPageId == field.DynamicPageId && f.DisplayOrder > field.DisplayOrder)
                    .OrderBy(f => f.DisplayOrder)
                    .FirstOrDefaultAsync();

                if (nextField != null)
                {
                    var tempOrder = field.DisplayOrder;
                    field.DisplayOrder = nextField.DisplayOrder;
                    nextField.DisplayOrder = tempOrder;
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction(nameof(Fields), new { id = field.DynamicPageId });
            }
            return RedirectToAction(nameof(Index));
        }

        private string GenerateTableName(string name)
        {
            // Rimuove caratteri speciali e spazi
            var tableName = Regex.Replace(name, @"[^a-zA-Z0-9]", "");
            return $"Dyn_{tableName}";
        }

        private string GenerateFieldName(string label)
        {
            // Rimuove caratteri speciali e spazi
            return Regex.Replace(label, @"[^a-zA-Z0-9]", "");
        }

        // GET: DynamicPages/ExportExcel - Esporta dati in Excel
        public async Task<IActionResult> ExportExcel(string categoria)
        {
            // Filtra per categoria
            var pagesQuery = _context.DynamicPages
                .Include(p => p.Fields.OrderBy(f => f.DisplayOrder))
                .Include(p => p.Records)
                    .ThenInclude(r => r.FieldValues)
                .Where(p => p.IsActive);

            if (categoria == "DatiRiservati")
            {
                pagesQuery = pagesQuery.Where(p => p.Category == "DatiRiservati");
            }
            else if (categoria == "DatiGenerali")
            {
                pagesQuery = pagesQuery.Where(p => p.Category == "DatiGenerali");
            }
            // Se "Tutti" non filtra per categoria

            var pages = await pagesQuery
                .OrderBy(p => p.Category)
                .ThenBy(p => p.DisplayOrder)
                .ThenBy(p => p.Name)
                .ToListAsync();

            if (!pages.Any())
            {
                TempData["Error"] = "Nessun dato da esportare.";
                return RedirectToAction(nameof(Index));
            }

            using var workbook = new XLWorkbook();

            foreach (var page in pages)
            {
                // Crea un foglio per ogni pagina
                var sheetName = page.Name.Length > 31 ? page.Name.Substring(0, 31) : page.Name;
                // Rimuovi caratteri non validi per Excel
                sheetName = Regex.Replace(sheetName, @"[\[\]\*\?\/\\:]", "");
                
                var worksheet = workbook.Worksheets.Add(sheetName);

                // Intestazioni
                int col = 1;
                foreach (var field in page.Fields.OrderBy(f => f.DisplayOrder))
                {
                    worksheet.Cell(1, col++).Value = field.Label;
                }

                // Stile intestazione
                var headerRange = worksheet.Range(1, 1, 1, Math.Max(col - 1, 1));
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = page.Category == "DatiRiservati" 
                    ? XLColor.FromHtml("#DC3545") 
                    : XLColor.FromHtml("#17A2B8");
                headerRange.Style.Font.FontColor = XLColor.White;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Dati
                int row = 2;
                foreach (var record in page.Records.OrderByDescending(r => r.CreatedAt))
                {
                    col = 1;
                    foreach (var field in page.Fields.OrderBy(f => f.DisplayOrder))
                    {
                        var valore = record.FieldValues
                            .FirstOrDefault(v => v.DynamicFieldId == field.Id)?.Value ?? "";

                        if (field.FieldType == FieldTypes.Boolean)
                        {
                            worksheet.Cell(row, col++).Value = valore == "true" ? "Sì" : "No";
                        }
                        else if (field.FieldType == FieldTypes.Date && DateTime.TryParse(valore, out var dataVal))
                        {
                            worksheet.Cell(row, col).Value = dataVal;
                            worksheet.Cell(row, col++).Style.DateFormat.Format = "dd/MM/yyyy";
                        }
                        else if ((field.FieldType == FieldTypes.Number || field.FieldType == FieldTypes.Decimal) && decimal.TryParse(valore, out var numVal))
                        {
                            worksheet.Cell(row, col++).Value = numVal;
                        }
                        else
                        {
                            worksheet.Cell(row, col++).Value = valore;
                        }
                    }
                    row++;
                }

                // Centra tutte le celle dati
                if (row > 2 && page.Fields.Any())
                {
                    var dataRange = worksheet.Range(2, 1, row - 1, page.Fields.Count);
                    dataRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    dataRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }

                // Auto-fit colonne
                worksheet.Columns().AdjustToContents();
            }

            // Genera file
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var categoriaLabel = categoria switch
            {
                "DatiRiservati" => "DatiRiservati",
                "DatiGenerali" => "DatiGenerali",
                _ => "TuttiIDati"
            };
            var fileName = $"Export_{categoriaLabel}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}

