using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioCG.Web.Data;
using StudioCG.Web.Filters;
using StudioCG.Web.Models.Documenti;
using StudioCG.Web.Services;

namespace StudioCG.Web.Controllers
{
    [Authorize]
    [AdminOnly]
    public class DocumentiController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DocumentiController> _logger;
        private readonly DocumentoGeneratorService _generatorService;

        public DocumentiController(
            ApplicationDbContext context, 
            ILogger<DocumentiController> logger,
            DocumentoGeneratorService generatorService)
        {
            _context = context;
            _logger = logger;
            _generatorService = generatorService;
        }

        // GET: /Documenti
        public async Task<IActionResult> Index()
        {
            var stats = new DocumentiDashboardViewModel
            {
                NumeroTemplate = await _context.TemplateDocumenti.CountAsync(t => t.IsActive),
                NumeroClausole = await _context.ClausoleDocumenti.CountAsync(c => c.IsActive),
                NumeroDocumentiGenerati = await _context.DocumentiGenerati.CountAsync(),
                UltimiDocumenti = await _context.DocumentiGenerati
                    .Include(d => d.Cliente)
                    .Include(d => d.TemplateDocumento)
                    .OrderByDescending(d => d.GeneratoIl)
                    .Take(10)
                    .ToListAsync(),
                ConfigurazionePresente = await _context.ConfigurazioniStudio.AnyAsync()
            };

            return View(stats);
        }

        #region Impostazioni Studio

        // GET: /Documenti/ImpostazioniStudio
        public async Task<IActionResult> ImpostazioniStudio()
        {
            var config = await _context.ConfigurazioniStudio.FirstOrDefaultAsync();
            if (config == null)
            {
                config = new ConfigurazioneStudio { NomeStudio = "" };
            }
            return View(config);
        }

        // POST: /Documenti/SalvaImpostazioniStudio
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SalvaImpostazioniStudio(ConfigurazioneStudio model, IFormFile? logoFile, IFormFile? firmaFile)
        {
            if (!ModelState.IsValid)
            {
                return View("ImpostazioniStudio", model);
            }

            var existingConfig = await _context.ConfigurazioniStudio.FirstOrDefaultAsync();
            bool isNew = existingConfig == null;
            var config = existingConfig ?? new ConfigurazioneStudio();

            if (isNew)
            {
                _context.ConfigurazioniStudio.Add(config);
            }

            // Aggiorna campi
            config.NomeStudio = model.NomeStudio;
            config.Indirizzo = model.Indirizzo;
            config.Citta = model.Citta;
            config.CAP = model.CAP;
            config.Provincia = model.Provincia;
            config.PIVA = model.PIVA;
            config.CF = model.CF;
            config.Email = model.Email;
            config.PEC = model.PEC;
            config.Telefono = model.Telefono;
            config.UpdatedAt = DateTime.Now;

            // Upload Logo
            if (logoFile != null && logoFile.Length > 0)
            {
                using var ms = new MemoryStream();
                await logoFile.CopyToAsync(ms);
                config.Logo = ms.ToArray();
                config.LogoContentType = logoFile.ContentType;
                config.LogoFileName = logoFile.FileName;
            }

            // Upload Firma
            if (firmaFile != null && firmaFile.Length > 0)
            {
                using var ms = new MemoryStream();
                await firmaFile.CopyToAsync(ms);
                config.Firma = ms.ToArray();
                config.FirmaContentType = firmaFile.ContentType;
                config.FirmaFileName = firmaFile.FileName;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Impostazioni studio salvate con successo!";
            return RedirectToAction(nameof(ImpostazioniStudio));
        }

        // GET: /Documenti/Logo
        public async Task<IActionResult> Logo()
        {
            var config = await _context.ConfigurazioniStudio.FirstOrDefaultAsync();
            if (config?.Logo == null)
                return NotFound();

            return File(config.Logo, config.LogoContentType ?? "image/png");
        }

        // GET: /Documenti/Firma
        public async Task<IActionResult> Firma()
        {
            var config = await _context.ConfigurazioniStudio.FirstOrDefaultAsync();
            if (config?.Firma == null)
                return NotFound();

            return File(config.Firma, config.FirmaContentType ?? "image/png");
        }

        // POST: /Documenti/RimuoviLogo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RimuoviLogo()
        {
            var config = await _context.ConfigurazioniStudio.FirstOrDefaultAsync();
            if (config != null)
            {
                config.Logo = null;
                config.LogoContentType = null;
                config.LogoFileName = null;
                config.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Logo rimosso!";
            }
            return RedirectToAction(nameof(ImpostazioniStudio));
        }

        // POST: /Documenti/RimuoviFirma
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RimuoviFirma()
        {
            var config = await _context.ConfigurazioniStudio.FirstOrDefaultAsync();
            if (config != null)
            {
                config.Firma = null;
                config.FirmaContentType = null;
                config.FirmaFileName = null;
                config.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Firma rimossa!";
            }
            return RedirectToAction(nameof(ImpostazioniStudio));
        }

        #endregion

        #region Clausole

        // GET: /Documenti/Clausole
        public async Task<IActionResult> Clausole()
        {
            var clausole = await _context.ClausoleDocumenti
                .Where(c => c.IsActive)
                .OrderBy(c => c.Categoria)
                .ThenBy(c => c.Ordine)
                .ThenBy(c => c.Nome)
                .ToListAsync();

            ViewBag.Categorie = CategorieClausole.Tutte;
            return View(clausole);
        }

        // POST: /Documenti/CreateClausola
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateClausola(ClausolaDocumento model)
        {
            if (string.IsNullOrWhiteSpace(model.Nome))
            {
                TempData["Error"] = "Il nome della clausola è obbligatorio.";
                return RedirectToAction(nameof(Clausole));
            }

            var clausola = new ClausolaDocumento
            {
                Nome = model.Nome,
                Categoria = model.Categoria ?? CategorieClausole.Generale,
                Descrizione = model.Descrizione,
                Contenuto = model.Contenuto ?? "",
                Ordine = model.Ordine,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.ClausoleDocumenti.Add(clausola);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Clausola '{clausola.Nome}' creata con successo!";
            return RedirectToAction(nameof(Clausole));
        }

        // POST: /Documenti/UpdateClausola
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateClausola(int id, string nome, string categoria, string? descrizione, string contenuto, int ordine)
        {
            var clausola = await _context.ClausoleDocumenti.FindAsync(id);
            if (clausola == null)
            {
                TempData["Error"] = "Clausola non trovata.";
                return RedirectToAction(nameof(Clausole));
            }

            clausola.Nome = nome;
            clausola.Categoria = categoria;
            clausola.Descrizione = descrizione;
            clausola.Contenuto = contenuto;
            clausola.Ordine = ordine;
            clausola.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Clausola '{clausola.Nome}' aggiornata!";
            return RedirectToAction(nameof(Clausole));
        }

        // POST: /Documenti/DeleteClausola
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteClausola(int id)
        {
            var clausola = await _context.ClausoleDocumenti.FindAsync(id);
            if (clausola != null)
            {
                clausola.IsActive = false;
                clausola.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Clausola '{clausola.Nome}' eliminata!";
            }
            return RedirectToAction(nameof(Clausole));
        }

        #endregion

        #region Template

        // GET: /Documenti/Template
        public async Task<IActionResult> Template()
        {
            var templates = await _context.TemplateDocumenti
                .Where(t => t.IsActive)
                .OrderBy(t => t.Categoria)
                .ThenBy(t => t.Ordine)
                .ThenBy(t => t.Nome)
                .ToListAsync();

            ViewBag.Categorie = CategorieTemplate.Tutte;
            return View(templates);
        }

        // GET: /Documenti/TemplateEditor/{id?}
        public async Task<IActionResult> TemplateEditor(int? id)
        {
            var clausole = await _context.ClausoleDocumenti
                .Where(c => c.IsActive)
                .OrderBy(c => c.Categoria)
                .ThenBy(c => c.Nome)
                .ToListAsync();

            ViewBag.Clausole = clausole;
            ViewBag.Categorie = CategorieTemplate.Tutte;

            if (id.HasValue)
            {
                var template = await _context.TemplateDocumenti.FindAsync(id.Value);
                if (template == null)
                    return NotFound();

                return View(template);
            }

            return View(new TemplateDocumento { Nome = "", Contenuto = "" });
        }

        // POST: /Documenti/SaveTemplate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveTemplate(TemplateDocumento model)
        {
            if (string.IsNullOrWhiteSpace(model.Nome))
            {
                TempData["Error"] = "Il nome del template è obbligatorio.";
                return RedirectToAction(nameof(Template));
            }

            // Assicurati che Contenuto non sia null
            model.Contenuto ??= "";
            model.Categoria ??= CategorieTemplate.Altro;

            if (model.Id == 0)
            {
                // Nuovo template
                model.IsActive = true;
                model.CreatedAt = DateTime.Now;
                _context.TemplateDocumenti.Add(model);
            }
            else
            {
                // Aggiorna esistente
                var existing = await _context.TemplateDocumenti.FindAsync(model.Id);
                if (existing == null)
                {
                    TempData["Error"] = "Template non trovato.";
                    return RedirectToAction(nameof(Template));
                }

                existing.Nome = model.Nome;
                existing.Categoria = model.Categoria ?? CategorieTemplate.Altro;
                existing.Descrizione = model.Descrizione;
                existing.Intestazione = model.Intestazione;
                existing.Contenuto = model.Contenuto ?? "";
                existing.PiePagina = model.PiePagina;
                existing.RichiestaMandato = model.RichiestaMandato;
                existing.TipoOutputDefault = model.TipoOutputDefault;
                existing.Ordine = model.Ordine;
                existing.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Template '{model.Nome}' salvato con successo!";
            return RedirectToAction(nameof(Template));
        }

        // POST: /Documenti/DeleteTemplate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTemplate(int id)
        {
            var template = await _context.TemplateDocumenti.FindAsync(id);
            if (template != null)
            {
                template.IsActive = false;
                template.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Template '{template.Nome}' eliminato!";
            }
            return RedirectToAction(nameof(Template));
        }

        // POST: /Documenti/DuplicaTemplate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DuplicaTemplate(int id)
        {
            var templateOriginale = await _context.TemplateDocumenti.FindAsync(id);
            if (templateOriginale == null)
            {
                TempData["Error"] = "Template non trovato!";
                return RedirectToAction(nameof(Template));
            }

            // Crea una copia del template
            var nuovoTemplate = new TemplateDocumento
            {
                Nome = $"{templateOriginale.Nome} (Copia)",
                Categoria = templateOriginale.Categoria,
                Descrizione = templateOriginale.Descrizione,
                Intestazione = templateOriginale.Intestazione,
                Contenuto = templateOriginale.Contenuto,
                PiePagina = templateOriginale.PiePagina,
                RichiestaMandato = templateOriginale.RichiestaMandato,
                TipoOutputDefault = templateOriginale.TipoOutputDefault,
                Ordine = templateOriginale.Ordine + 1,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.TemplateDocumenti.Add(nuovoTemplate);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Template duplicato! Modifica '{nuovoTemplate.Nome}' per personalizzarlo.";
            
            // Apri direttamente l'editor per modificare il nuovo template
            return RedirectToAction("TemplateEditor", new { id = nuovoTemplate.Id });
        }

        #endregion

        #region Genera Documento

        // GET: /Documenti/Genera
        public async Task<IActionResult> Genera()
        {
            var templates = await _context.TemplateDocumenti
                .Where(t => t.IsActive)
                .OrderBy(t => t.Categoria)
                .ThenBy(t => t.Nome)
                .ToListAsync();

            var clienti = await _context.Clienti
                .Where(c => c.IsActive)
                .OrderBy(c => c.RagioneSociale)
                .Select(c => new { c.Id, c.RagioneSociale })
                .ToListAsync();

            ViewBag.Templates = templates;
            ViewBag.Clienti = clienti;

            return View();
        }

        // POST: /Documenti/GeneraDocumento
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GeneraDocumento(int templateId, int clienteId, int? mandatoId, TipoOutputDocumento tipoOutput)
        {
            try
            {
                // Ottieni userId corrente
                var username = User.Identity?.Name;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                var userId = user?.Id ?? 0;

                // Genera documento
                var documento = await _generatorService.GeneraDocumentoAsync(
                    templateId, clienteId, mandatoId, tipoOutput, userId);

                TempData["Success"] = $"Documento '{documento.NomeFile}' generato con successo!";

                // Scarica direttamente
                return File(documento.Contenuto, documento.ContentType, documento.NomeFile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore generazione documento");
                TempData["Error"] = $"Errore: {ex.Message}";
                return RedirectToAction(nameof(Genera));
            }
        }

        // GET: /Documenti/Anteprima
        [HttpGet]
        public async Task<IActionResult> Anteprima(int templateId, int clienteId, int? mandatoId)
        {
            try
            {
                var html = await _generatorService.GeneraAnteprimaAsync(templateId, clienteId, mandatoId);
                return Content(html, "text/html");
            }
            catch (Exception ex)
            {
                return Content($"<p class='text-danger'>Errore: {ex.Message}</p>", "text/html");
            }
        }

        // GET: /Documenti/GetMandatiCliente/{clienteId}
        [HttpGet]
        public async Task<IActionResult> GetMandatiCliente(int clienteId)
        {
            var mandati = await _context.MandatiClienti
                .Where(m => m.ClienteId == clienteId && m.IsActive)
                .OrderByDescending(m => m.Anno)
                .Select(m => new { 
                    m.Id, 
                    Oggetto = m.Note ?? $"Mandato {m.Anno}", 
                    ImportoAnnuo = m.ImportoAnnuo.ToString("N2") 
                })
                .ToListAsync();

            return Json(mandati);
        }

        #endregion

        #region Archivio

        // GET: /Documenti/Archivio
        public async Task<IActionResult> Archivio(int? clienteId, int? templateId, DateTime? dataFrom, DateTime? dataTo)
        {
            var query = _context.DocumentiGenerati
                .Include(d => d.Cliente)
                .Include(d => d.TemplateDocumento)
                .Include(d => d.GeneratoDa)
                .AsQueryable();

            if (clienteId.HasValue)
                query = query.Where(d => d.ClienteId == clienteId.Value);

            if (templateId.HasValue)
                query = query.Where(d => d.TemplateDocumentoId == templateId.Value);

            if (dataFrom.HasValue)
                query = query.Where(d => d.GeneratoIl >= dataFrom.Value);

            if (dataTo.HasValue)
                query = query.Where(d => d.GeneratoIl <= dataTo.Value.AddDays(1));

            var documenti = await query
                .OrderByDescending(d => d.GeneratoIl)
                .Take(100)
                .ToListAsync();

            // Per i filtri
            ViewBag.Clienti = await _context.Clienti
                .Where(c => c.IsActive)
                .OrderBy(c => c.RagioneSociale)
                .Select(c => new { c.Id, c.RagioneSociale })
                .ToListAsync();

            ViewBag.Templates = await _context.TemplateDocumenti
                .Where(t => t.IsActive)
                .OrderBy(t => t.Nome)
                .Select(t => new { t.Id, t.Nome })
                .ToListAsync();

            ViewBag.ClienteId = clienteId;
            ViewBag.TemplateId = templateId;
            ViewBag.DataFrom = dataFrom;
            ViewBag.DataTo = dataTo;

            return View(documenti);
        }

        // GET: /Documenti/DownloadDocumento/{id}
        public async Task<IActionResult> DownloadDocumento(int id)
        {
            var doc = await _context.DocumentiGenerati.FindAsync(id);
            if (doc == null)
                return NotFound();

            return File(doc.Contenuto, doc.ContentType, doc.NomeFile);
        }

        // POST: /Documenti/DeleteDocumento
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDocumento(int id)
        {
            var doc = await _context.DocumentiGenerati.FindAsync(id);
            if (doc != null)
            {
                _context.DocumentiGenerati.Remove(doc);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Documento eliminato!";
            }
            return RedirectToAction(nameof(Archivio));
        }

        // POST: /Documenti/DeleteDocumentiMultipli
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDocumentiMultipli(int[] ids)
        {
            if (ids == null || ids.Length == 0)
            {
                TempData["Error"] = "Nessun documento selezionato.";
                return RedirectToAction(nameof(Archivio));
            }

            var documenti = await _context.DocumentiGenerati
                .Where(d => ids.Contains(d.Id))
                .ToListAsync();

            if (documenti.Any())
            {
                _context.DocumentiGenerati.RemoveRange(documenti);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"{documenti.Count} documento/i eliminato/i con successo!";
            }

            return RedirectToAction(nameof(Archivio));
        }

        #endregion
    }

    /// <summary>
    /// ViewModel per la dashboard Documenti
    /// </summary>
    public class DocumentiDashboardViewModel
    {
        public int NumeroTemplate { get; set; }
        public int NumeroClausole { get; set; }
        public int NumeroDocumentiGenerati { get; set; }
        public List<DocumentoGenerato> UltimiDocumenti { get; set; } = new();
        public bool ConfigurazionePresente { get; set; }
    }
}

