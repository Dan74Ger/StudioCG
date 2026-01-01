using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioCG.Web.Data;
using StudioCG.Web.Filters;
using StudioCG.Web.Models;
using StudioCG.Web.Models.Entita;

namespace StudioCG.Web.Controllers
{
    [Authorize]
    public class EntitaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EntitaController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ============ GESTIONE ENTITÀ (ADMIN) ============

        // GET: Entita/Gestione
        [AdminOnly]
        public async Task<IActionResult> Gestione()
        {
            var entita = await _context.EntitaDinamiche
                .Include(e => e.Campi.OrderBy(c => c.DisplayOrder))
                .Include(e => e.Stati.OrderBy(s => s.DisplayOrder))
                .OrderBy(e => e.DisplayOrder)
                .ThenBy(e => e.Nome)
                .ToListAsync();

            return View(entita);
        }

        // POST: Entita/CreaEntita
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminOnly]
        public async Task<IActionResult> CreaEntita(EntitaDinamica entita)
        {
            if (string.IsNullOrWhiteSpace(entita.Nome))
            {
                TempData["Error"] = "Il nome dell'entità è obbligatorio.";
                return RedirectToAction(nameof(Gestione));
            }

            // Verifica duplicati
            if (await _context.EntitaDinamiche.AnyAsync(e => e.Nome == entita.Nome))
            {
                TempData["Error"] = $"Esiste già un'entità con nome '{entita.Nome}'.";
                return RedirectToAction(nameof(Gestione));
            }

            // Imposta valori default
            if (string.IsNullOrEmpty(entita.NomePluruale))
                entita.NomePluruale = entita.Nome;

            var maxOrder = await _context.EntitaDinamiche.MaxAsync(e => (int?)e.DisplayOrder) ?? 0;
            entita.DisplayOrder = maxOrder + 1;
            entita.CreatedAt = DateTime.Now;
            entita.IsActive = true;

            _context.EntitaDinamiche.Add(entita);
            await _context.SaveChangesAsync();

            // Crea stati default
            var statiDefault = new[]
            {
                new StatoEntita { EntitaDinamicaId = entita.Id, Nome = "Nuovo", ColoreSfondo = "#17a2b8", ColoreTesto = "#FFFFFF", Icon = "fas fa-star", DisplayOrder = 1, IsDefault = true },
                new StatoEntita { EntitaDinamicaId = entita.Id, Nome = "In Corso", ColoreSfondo = "#ffc107", ColoreTesto = "#000000", Icon = "fas fa-spinner", DisplayOrder = 2 },
                new StatoEntita { EntitaDinamicaId = entita.Id, Nome = "Completato", ColoreSfondo = "#28a745", ColoreTesto = "#FFFFFF", Icon = "fas fa-check", DisplayOrder = 3, IsFinale = true }
            };
            _context.StatiEntita.AddRange(statiDefault);
            await _context.SaveChangesAsync();

            // Aggiungi permesso automatico
            await AggiungiPermessoEntita(entita);

            TempData["Success"] = $"Entità '{entita.Nome}' creata con successo.";
            return RedirectToAction(nameof(Configura), new { id = entita.Id });
        }

        // GET: Entita/Configura/5
        [AdminOnly]
        public async Task<IActionResult> Configura(int id)
        {
            var entita = await _context.EntitaDinamiche
                .Include(e => e.Campi.OrderBy(c => c.DisplayOrder))
                .Include(e => e.Stati.OrderBy(s => s.DisplayOrder))
                .FirstOrDefaultAsync(e => e.Id == id);

            if (entita == null)
            {
                TempData["Error"] = "Entità non trovata.";
                return RedirectToAction(nameof(Gestione));
            }

            return View(entita);
        }

        // POST: Entita/AggiornaEntita
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminOnly]
        public async Task<IActionResult> AggiornaEntita(int id, string nome, string nomePluruale, string? descrizione, string icon, string colore, bool collegataACliente)
        {
            var entita = await _context.EntitaDinamiche.FindAsync(id);
            if (entita == null)
            {
                TempData["Error"] = "Entità non trovata.";
                return RedirectToAction(nameof(Gestione));
            }

            entita.Nome = nome;
            entita.NomePluruale = nomePluruale;
            entita.Descrizione = descrizione;
            entita.Icon = icon;
            entita.Colore = colore;
            entita.CollegataACliente = collegataACliente;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Entità aggiornata con successo.";
            return RedirectToAction(nameof(Configura), new { id });
        }

        // POST: Entita/EliminaEntita
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminOnly]
        public async Task<IActionResult> EliminaEntita(int id)
        {
            var entita = await _context.EntitaDinamiche
                .Include(e => e.Records)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (entita == null)
            {
                TempData["Error"] = "Entità non trovata.";
                return RedirectToAction(nameof(Gestione));
            }

            // Rimuovi permesso
            var permesso = await _context.Permissions.FirstOrDefaultAsync(p => p.PageUrl == $"/Entita/Dati/{id}");
            if (permesso != null)
            {
                _context.Permissions.Remove(permesso);
            }

            _context.EntitaDinamiche.Remove(entita);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Entità '{entita.Nome}' eliminata con tutti i suoi dati.";
            return RedirectToAction(nameof(Gestione));
        }

        // ============ GESTIONE CAMPI ============

        // POST: Entita/AddCampo
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminOnly]
        public async Task<IActionResult> AddCampo(CampoEntita campo)
        {
            if (string.IsNullOrWhiteSpace(campo.Nome) || string.IsNullOrWhiteSpace(campo.Label))
            {
                TempData["Error"] = "Nome e etichetta sono obbligatori.";
                return RedirectToAction(nameof(Configura), new { id = campo.EntitaDinamicaId });
            }

            // Verifica duplicati
            if (await _context.CampiEntita.AnyAsync(c => c.EntitaDinamicaId == campo.EntitaDinamicaId && c.Nome == campo.Nome))
            {
                TempData["Error"] = $"Esiste già un campo con nome '{campo.Nome}'.";
                return RedirectToAction(nameof(Configura), new { id = campo.EntitaDinamicaId });
            }

            var maxOrder = await _context.CampiEntita
                .Where(c => c.EntitaDinamicaId == campo.EntitaDinamicaId)
                .MaxAsync(c => (int?)c.DisplayOrder) ?? 0;
            campo.DisplayOrder = maxOrder + 1;
            campo.IsActive = true;

            _context.CampiEntita.Add(campo);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Campo '{campo.Label}' aggiunto.";
            return RedirectToAction(nameof(Configura), new { id = campo.EntitaDinamicaId });
        }

        // POST: Entita/EditCampo
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminOnly]
        public async Task<IActionResult> EditCampo(int id, string label, string tipoCampo, bool isRequired, 
            bool showInList, bool useAsFilter, string? options, string? defaultValue, string? placeholder, int colWidth)
        {
            var campo = await _context.CampiEntita.FindAsync(id);
            if (campo == null)
            {
                TempData["Error"] = "Campo non trovato.";
                return RedirectToAction(nameof(Gestione));
            }

            campo.Label = label;
            campo.TipoCampo = tipoCampo;
            campo.IsRequired = isRequired;
            campo.ShowInList = showInList;
            campo.UseAsFilter = useAsFilter;
            campo.Options = options;
            campo.DefaultValue = defaultValue;
            campo.Placeholder = placeholder;
            campo.ColWidth = colWidth;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Campo '{campo.Label}' aggiornato.";
            return RedirectToAction(nameof(Configura), new { id = campo.EntitaDinamicaId });
        }

        // POST: Entita/DeleteCampo
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminOnly]
        public async Task<IActionResult> DeleteCampo(int id)
        {
            var campo = await _context.CampiEntita.FindAsync(id);
            if (campo == null)
            {
                TempData["Error"] = "Campo non trovato.";
                return RedirectToAction(nameof(Gestione));
            }

            var entitaId = campo.EntitaDinamicaId;
            _context.CampiEntita.Remove(campo);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Campo eliminato.";
            return RedirectToAction(nameof(Configura), new { id = entitaId });
        }

        // POST: Entita/MoveCampoUp e MoveCampoDown
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminOnly]
        public async Task<IActionResult> MoveCampoUp(int id)
        {
            var campo = await _context.CampiEntita.FindAsync(id);
            if (campo == null) return RedirectToAction(nameof(Gestione));

            var campoPrecedente = await _context.CampiEntita
                .Where(c => c.EntitaDinamicaId == campo.EntitaDinamicaId && c.DisplayOrder < campo.DisplayOrder)
                .OrderByDescending(c => c.DisplayOrder)
                .FirstOrDefaultAsync();

            if (campoPrecedente != null)
            {
                var temp = campo.DisplayOrder;
                campo.DisplayOrder = campoPrecedente.DisplayOrder;
                campoPrecedente.DisplayOrder = temp;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Configura), new { id = campo.EntitaDinamicaId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminOnly]
        public async Task<IActionResult> MoveCampoDown(int id)
        {
            var campo = await _context.CampiEntita.FindAsync(id);
            if (campo == null) return RedirectToAction(nameof(Gestione));

            var campoSuccessivo = await _context.CampiEntita
                .Where(c => c.EntitaDinamicaId == campo.EntitaDinamicaId && c.DisplayOrder > campo.DisplayOrder)
                .OrderBy(c => c.DisplayOrder)
                .FirstOrDefaultAsync();

            if (campoSuccessivo != null)
            {
                var temp = campo.DisplayOrder;
                campo.DisplayOrder = campoSuccessivo.DisplayOrder;
                campoSuccessivo.DisplayOrder = temp;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Configura), new { id = campo.EntitaDinamicaId });
        }

        // ============ GESTIONE STATI ============

        // POST: Entita/AddStato
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminOnly]
        public async Task<IActionResult> AddStato(StatoEntita stato)
        {
            if (string.IsNullOrWhiteSpace(stato.Nome))
            {
                TempData["Error"] = "Il nome dello stato è obbligatorio.";
                return RedirectToAction(nameof(Configura), new { id = stato.EntitaDinamicaId });
            }

            var maxOrder = await _context.StatiEntita
                .Where(s => s.EntitaDinamicaId == stato.EntitaDinamicaId)
                .MaxAsync(s => (int?)s.DisplayOrder) ?? 0;
            stato.DisplayOrder = maxOrder + 1;
            stato.IsActive = true;

            _context.StatiEntita.Add(stato);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Stato '{stato.Nome}' aggiunto.";
            return RedirectToAction(nameof(Configura), new { id = stato.EntitaDinamicaId });
        }

        // POST: Entita/DeleteStato
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminOnly]
        public async Task<IActionResult> DeleteStato(int id)
        {
            var stato = await _context.StatiEntita.FindAsync(id);
            if (stato == null)
            {
                TempData["Error"] = "Stato non trovato.";
                return RedirectToAction(nameof(Gestione));
            }

            var entitaId = stato.EntitaDinamicaId;
            _context.StatiEntita.Remove(stato);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Configura), new { id = entitaId });
        }

        // ============ VISUALIZZAZIONE DATI ============

        // GET: Entita/Dati/5
        public async Task<IActionResult> Dati(int id, int? clienteId, int? statoId, string? search)
        {
            var entita = await _context.EntitaDinamiche
                .Include(e => e.Campi.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder))
                .Include(e => e.Stati.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder))
                .FirstOrDefaultAsync(e => e.Id == id && e.IsActive);

            if (entita == null)
            {
                TempData["Error"] = "Entità non trovata.";
                return RedirectToAction(nameof(Gestione));
            }

            ViewBag.Entita = entita;
            ViewBag.ClienteId = clienteId;
            ViewBag.StatoId = statoId;
            ViewBag.Search = search;

            // Leggi filtri dinamici dai campi
            var filtriCampi = new Dictionary<int, string>();
            var campiFiltrabili = entita.Campi.Where(c => c.UseAsFilter).ToList();
            foreach (var campo in campiFiltrabili)
            {
                var valoreFiltro = Request.Query[$"filtro_{campo.Id}"].ToString();
                if (!string.IsNullOrWhiteSpace(valoreFiltro))
                {
                    filtriCampi[campo.Id] = valoreFiltro;
                }
            }
            ViewBag.FiltriCampi = filtriCampi;

            // Query records
            var query = _context.RecordsEntita
                .Include(r => r.Cliente)
                .Include(r => r.StatoEntita)
                .Include(r => r.Valori)
                .Where(r => r.EntitaDinamicaId == id);

            if (clienteId.HasValue)
                query = query.Where(r => r.ClienteId == clienteId);

            if (statoId.HasValue)
                query = query.Where(r => r.StatoEntitaId == statoId);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(r => r.Titolo != null && r.Titolo.Contains(search));

            var records = await query
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // Applica filtri campi custom (in memoria per semplicità)
            if (filtriCampi.Any())
            {
                records = records.Where(r =>
                {
                    foreach (var filtro in filtriCampi)
                    {
                        var valore = r.Valori.FirstOrDefault(v => v.CampoEntitaId == filtro.Key)?.Valore ?? "";
                        var campo = campiFiltrabili.First(c => c.Id == filtro.Key);
                        
                        if (campo.TipoCampo == "checkbox")
                        {
                            // Per checkbox: confronto esatto
                            if (valore != filtro.Value) return false;
                        }
                        else if (campo.TipoCampo == "dropdown")
                        {
                            // Per dropdown: confronto esatto
                            if (valore != filtro.Value) return false;
                        }
                        else
                        {
                            // Per altri tipi: contiene
                            if (!valore.Contains(filtro.Value, StringComparison.OrdinalIgnoreCase)) return false;
                        }
                    }
                    return true;
                }).ToList();
            }

            // Carica clienti per dropdown
            if (entita.CollegataACliente)
            {
                ViewBag.Clienti = await _context.Clienti
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.RagioneSociale)
                    .ToListAsync();
            }

            return View(records);
        }

        // GET: Entita/NuovoRecord/5
        public async Task<IActionResult> NuovoRecord(int id, int? clienteId)
        {
            var entita = await _context.EntitaDinamiche
                .Include(e => e.Campi.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder))
                .Include(e => e.Stati.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder))
                .FirstOrDefaultAsync(e => e.Id == id && e.IsActive);

            if (entita == null)
            {
                TempData["Error"] = "Entità non trovata.";
                return RedirectToAction(nameof(Gestione));
            }

            ViewBag.Entita = entita;
            ViewBag.ClienteId = clienteId;

            if (entita.CollegataACliente)
            {
                ViewBag.Clienti = await _context.Clienti
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.RagioneSociale)
                    .ToListAsync();
            }

            return View(new RecordEntita { EntitaDinamicaId = id, ClienteId = clienteId });
        }

        // POST: Entita/CreaRecord
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreaRecord(RecordEntita record, IFormCollection form)
        {
            var entita = await _context.EntitaDinamiche
                .Include(e => e.Campi.Where(c => c.IsActive))
                .Include(e => e.Stati.Where(s => s.IsActive))
                .FirstOrDefaultAsync(e => e.Id == record.EntitaDinamicaId);

            if (entita == null)
            {
                TempData["Error"] = "Entità non trovata.";
                return RedirectToAction(nameof(Gestione));
            }

            // Imposta stato default se non specificato
            if (!record.StatoEntitaId.HasValue)
            {
                var statoDefault = entita.Stati.FirstOrDefault(s => s.IsDefault) ?? entita.Stati.FirstOrDefault();
                record.StatoEntitaId = statoDefault?.Id;
            }

            // Imposta user
            var username = User.Identity?.Name;
            if (!string.IsNullOrEmpty(username))
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                record.CreatedByUserId = user?.Id;
            }

            record.CreatedAt = DateTime.Now;
            record.UpdatedAt = DateTime.Now;

            _context.RecordsEntita.Add(record);
            await _context.SaveChangesAsync();

            // Salva valori campi
            foreach (var campo in entita.Campi)
            {
                var formKey = $"campo_{campo.Id}";
                var valore = form[formKey].ToString();

                if (campo.TipoCampo == "checkbox")
                {
                    valore = form.Keys.Contains(formKey) ? "true" : "false";
                }

                if (!string.IsNullOrEmpty(valore))
                {
                    _context.ValoriCampiEntita.Add(new ValoreCampoEntita
                    {
                        RecordEntitaId = record.Id,
                        CampoEntitaId = campo.Id,
                        Valore = valore,
                        UpdatedAt = DateTime.Now
                    });
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Record creato con successo.";
            return RedirectToAction(nameof(Dati), new { id = record.EntitaDinamicaId });
        }

        // GET: Entita/EditRecord/5
        public async Task<IActionResult> EditRecord(int id)
        {
            var record = await _context.RecordsEntita
                .Include(r => r.EntitaDinamica)
                    .ThenInclude(e => e!.Campi.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder))
                .Include(r => r.EntitaDinamica)
                    .ThenInclude(e => e!.Stati.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder))
                .Include(r => r.Valori)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (record == null)
            {
                TempData["Error"] = "Record non trovato.";
                return RedirectToAction(nameof(Gestione));
            }

            ViewBag.Entita = record.EntitaDinamica;
            ViewBag.ValoriCampi = record.Valori.ToDictionary(v => v.CampoEntitaId, v => v.Valore);

            if (record.EntitaDinamica?.CollegataACliente == true)
            {
                ViewBag.Clienti = await _context.Clienti
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.RagioneSociale)
                    .ToListAsync();
            }

            return View(record);
        }

        // POST: Entita/AggiornaRecord
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AggiornaRecord(int id, int? clienteId, int? statoEntitaId, string? titolo, string? note, IFormCollection form)
        {
            var record = await _context.RecordsEntita
                .Include(r => r.EntitaDinamica)
                    .ThenInclude(e => e!.Campi.Where(c => c.IsActive))
                .Include(r => r.Valori)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (record == null)
            {
                TempData["Error"] = "Record non trovato.";
                return RedirectToAction(nameof(Gestione));
            }

            record.ClienteId = clienteId;
            record.StatoEntitaId = statoEntitaId;
            record.Titolo = titolo;
            record.Note = note;
            record.UpdatedAt = DateTime.Now;

            // Aggiorna valori campi
            foreach (var campo in record.EntitaDinamica!.Campi)
            {
                var formKey = $"campo_{campo.Id}";
                var valore = form[formKey].ToString();

                if (campo.TipoCampo == "checkbox")
                {
                    valore = form.Keys.Contains(formKey) ? "true" : "false";
                }

                var valoreEsistente = record.Valori.FirstOrDefault(v => v.CampoEntitaId == campo.Id);
                if (valoreEsistente != null)
                {
                    valoreEsistente.Valore = valore;
                    valoreEsistente.UpdatedAt = DateTime.Now;
                }
                else if (!string.IsNullOrEmpty(valore))
                {
                    _context.ValoriCampiEntita.Add(new ValoreCampoEntita
                    {
                        RecordEntitaId = record.Id,
                        CampoEntitaId = campo.Id,
                        Valore = valore,
                        UpdatedAt = DateTime.Now
                    });
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Record aggiornato.";
            return RedirectToAction(nameof(Dati), new { id = record.EntitaDinamicaId });
        }

        // POST: Entita/EliminaRecord
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminaRecord(int id)
        {
            var record = await _context.RecordsEntita.FindAsync(id);
            if (record == null)
            {
                TempData["Error"] = "Record non trovato.";
                return RedirectToAction(nameof(Gestione));
            }

            var entitaId = record.EntitaDinamicaId;
            _context.RecordsEntita.Remove(record);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Record eliminato.";
            return RedirectToAction(nameof(Dati), new { id = entitaId });
        }

        // POST: Entita/CambiaStatoRapido
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiaStatoRapido(int recordId, int nuovoStatoId, int? clienteId, int? statoIdFiltro)
        {
            var record = await _context.RecordsEntita.FindAsync(recordId);
            if (record == null)
            {
                TempData["Error"] = "Record non trovato.";
                return RedirectToAction(nameof(Gestione));
            }

            var statoNuovo = await _context.StatiEntita.FindAsync(nuovoStatoId);
            if (statoNuovo == null || statoNuovo.EntitaDinamicaId != record.EntitaDinamicaId)
            {
                TempData["Error"] = "Stato non valido.";
                return RedirectToAction(nameof(Dati), new { id = record.EntitaDinamicaId });
            }

            record.StatoEntitaId = nuovoStatoId;
            record.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Stato aggiornato a '{statoNuovo.Nome}'.";
            return RedirectToAction(nameof(Dati), new { id = record.EntitaDinamicaId, clienteId, statoId = statoIdFiltro });
        }

        // ============ HELPERS ============

        private async Task AggiungiPermessoEntita(EntitaDinamica entita)
        {
            var pageUrl = $"/Entita/Dati/{entita.Id}";
            
            if (!await _context.Permissions.AnyAsync(p => p.PageUrl == pageUrl))
            {
                var maxOrder = await _context.Permissions.MaxAsync(p => (int?)p.DisplayOrder) ?? 0;
                
                _context.Permissions.Add(new Permission
                {
                    PageName = entita.Nome,
                    PageUrl = pageUrl,
                    Description = $"Gestione dati {entita.NomePluruale}",
                    Icon = entita.Icon,
                    Category = "ENTITA",
                    DisplayOrder = maxOrder + 1,
                    ShowInMenu = true
                });
                
                await _context.SaveChangesAsync();
            }
        }
    }
}
