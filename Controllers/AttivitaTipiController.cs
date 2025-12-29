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
                .Include(t => t.Stati)
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

                // Crea stati di default per la nuova attività
                await CreaStatiDefault(model.Id);

                TempData["Success"] = $"Tipo attività '{model.Nome}' creato. Ora aggiungi i campi e gli stati.";
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
                .Include(t => t.Stati)
                .Include(t => t.AttivitaAnnuali)
                    .ThenInclude(aa => aa.ClientiAttivita)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tipo == null) return NotFound();

            // Conta i clienti totali assegnati a questo tipo
            var totaleClientiAssegnati = tipo.AttivitaAnnuali.Sum(aa => aa.ClientiAttivita.Count);
            ViewBag.TotaleClientiAssegnati = totaleClientiAssegnati;

            return View(tipo);
        }

        // POST: AttivitaTipi/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tipo = await _context.AttivitaTipi
                .Include(t => t.AttivitaAnnuali)
                    .ThenInclude(aa => aa.ClientiAttivita)
                        .ThenInclude(ca => ca.Valori)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tipo != null)
            {
                // 1. Elimina tutti i valori dei campi cliente-attività
                foreach (var attivitaAnnuale in tipo.AttivitaAnnuali)
                {
                    foreach (var clienteAttivita in attivitaAnnuale.ClientiAttivita)
                    {
                        _context.ClientiAttivitaValori.RemoveRange(clienteAttivita.Valori);
                    }
                    // 2. Elimina tutti i clienti-attività
                    _context.ClientiAttivita.RemoveRange(attivitaAnnuale.ClientiAttivita);
                }

                // 3. Elimina tutte le attività annuali
                _context.AttivitaAnnuali.RemoveRange(tipo.AttivitaAnnuali);

                // 4. Elimina il Permission corrispondente
                var permissionUrl = $"/Attivita/Tipo/{id}";
                var permission = await _context.Permissions.FirstOrDefaultAsync(p => p.PageUrl == permissionUrl);
                if (permission != null)
                {
                    var userPermissions = _context.UserPermissions.Where(up => up.PermissionId == permission.Id);
                    _context.UserPermissions.RemoveRange(userPermissions);
                    _context.Permissions.Remove(permission);
                }

                // 5. Elimina il tipo (gli Stati e Campi vengono eliminati in cascade)
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
                
                // Prima elimina tutti i valori associati a questo campo
                var valoriAssociati = await _context.ClientiAttivitaValori
                    .Where(v => v.AttivitaCampoId == id)
                    .ToListAsync();
                
                if (valoriAssociati.Any())
                {
                    _context.ClientiAttivitaValori.RemoveRange(valoriAssociati);
                }
                
                _context.AttivitaCampi.Remove(campo);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Campo eliminato insieme a {valoriAssociati.Count} valori associati.";
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

        // ==================== GESTIONE STATI ====================

        /// <summary>
        /// Crea gli stati di default per una nuova attività
        /// </summary>
        private async Task CreaStatiDefault(int attivitaTipoId)
        {
            var statiDefault = new[]
            {
                new StatoAttivitaTipo
                {
                    AttivitaTipoId = attivitaTipoId,
                    Nome = "Da Fare",
                    Icon = "fas fa-clock",
                    ColoreTesto = "#000000",
                    ColoreSfondo = "#ffc107",
                    DisplayOrder = 0,
                    IsDefault = true,
                    IsFinale = false,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new StatoAttivitaTipo
                {
                    AttivitaTipoId = attivitaTipoId,
                    Nome = "Completata",
                    Icon = "fas fa-check-circle",
                    ColoreTesto = "#FFFFFF",
                    ColoreSfondo = "#28a745",
                    DisplayOrder = 1,
                    IsDefault = false,
                    IsFinale = true,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                }
            };

            _context.StatiAttivitaTipo.AddRange(statiDefault);
            await _context.SaveChangesAsync();
        }

        // GET: AttivitaTipi/Stati/5
        public async Task<IActionResult> Stati(int? id)
        {
            if (id == null) return NotFound();

            var tipo = await _context.AttivitaTipi
                .Include(t => t.Stati.OrderBy(s => s.DisplayOrder))
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tipo == null) return NotFound();

            return View(tipo);
        }

        // POST: AttivitaTipi/AddStato
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStato(StatoAttivitaTipo model)
        {
            if (await _context.StatiAttivitaTipo.AnyAsync(s => s.AttivitaTipoId == model.AttivitaTipoId && s.Nome == model.Nome))
            {
                TempData["Error"] = "Esiste già uno stato con questo nome.";
                return RedirectToAction(nameof(Stati), new { id = model.AttivitaTipoId });
            }

            // Imposta ordine
            var maxOrder = await _context.StatiAttivitaTipo
                .Where(s => s.AttivitaTipoId == model.AttivitaTipoId)
                .MaxAsync(s => (int?)s.DisplayOrder) ?? -1;
            model.DisplayOrder = maxOrder + 1;
            model.CreatedAt = DateTime.Now;
            model.IsActive = true;

            _context.StatiAttivitaTipo.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Stato '{model.Nome}' aggiunto.";
            return RedirectToAction(nameof(Stati), new { id = model.AttivitaTipoId });
        }

        // POST: AttivitaTipi/UpdateStato
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStato(int id, string nome, string icon, string coloreTesto, string coloreSfondo)
        {
            var stato = await _context.StatiAttivitaTipo.FindAsync(id);
            if (stato == null) return NotFound();

            // Verifica duplicati
            if (await _context.StatiAttivitaTipo.AnyAsync(s => s.AttivitaTipoId == stato.AttivitaTipoId && s.Nome == nome && s.Id != id))
            {
                TempData["Error"] = "Esiste già uno stato con questo nome.";
                return RedirectToAction(nameof(Stati), new { id = stato.AttivitaTipoId });
            }

            stato.Nome = nome;
            stato.Icon = icon;
            stato.ColoreTesto = coloreTesto;
            stato.ColoreSfondo = coloreSfondo;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Stato aggiornato.";
            return RedirectToAction(nameof(Stati), new { id = stato.AttivitaTipoId });
        }

        // POST: AttivitaTipi/DeleteStato/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStato(int id)
        {
            var stato = await _context.StatiAttivitaTipo.FindAsync(id);
            if (stato == null) return NotFound();

            var tipoId = stato.AttivitaTipoId;

            // Verifica se è l'unico stato
            var countStati = await _context.StatiAttivitaTipo.CountAsync(s => s.AttivitaTipoId == tipoId);
            if (countStati <= 1)
            {
                TempData["Error"] = "Non puoi eliminare l'unico stato rimasto. Ogni attività deve avere almeno uno stato.";
                return RedirectToAction(nameof(Stati), new { id = tipoId });
            }

            // Verifica se ci sono ClienteAttivita che usano questo stato
            var usato = await _context.ClientiAttivita.AnyAsync(ca => ca.StatoAttivitaTipoId == id);
            if (usato)
            {
                TempData["Error"] = "Questo stato è utilizzato da alcune attività cliente. Prima cambia lo stato a queste attività.";
                return RedirectToAction(nameof(Stati), new { id = tipoId });
            }

            // Se era default o finale, imposta un altro stato
            if (stato.IsDefault)
            {
                var altroStato = await _context.StatiAttivitaTipo
                    .FirstOrDefaultAsync(s => s.AttivitaTipoId == tipoId && s.Id != id);
                if (altroStato != null) altroStato.IsDefault = true;
            }

            _context.StatiAttivitaTipo.Remove(stato);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Stato eliminato.";
            return RedirectToAction(nameof(Stati), new { id = tipoId });
        }

        // POST: AttivitaTipi/MoveStatoUp/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveStatoUp(int id)
        {
            var stato = await _context.StatiAttivitaTipo.FindAsync(id);
            if (stato != null)
            {
                var prevStato = await _context.StatiAttivitaTipo
                    .Where(s => s.AttivitaTipoId == stato.AttivitaTipoId && s.DisplayOrder < stato.DisplayOrder)
                    .OrderByDescending(s => s.DisplayOrder)
                    .FirstOrDefaultAsync();

                if (prevStato != null)
                {
                    var tempOrder = stato.DisplayOrder;
                    stato.DisplayOrder = prevStato.DisplayOrder;
                    prevStato.DisplayOrder = tempOrder;
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction(nameof(Stati), new { id = stato.AttivitaTipoId });
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: AttivitaTipi/MoveStatoDown/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveStatoDown(int id)
        {
            var stato = await _context.StatiAttivitaTipo.FindAsync(id);
            if (stato != null)
            {
                var nextStato = await _context.StatiAttivitaTipo
                    .Where(s => s.AttivitaTipoId == stato.AttivitaTipoId && s.DisplayOrder > stato.DisplayOrder)
                    .OrderBy(s => s.DisplayOrder)
                    .FirstOrDefaultAsync();

                if (nextStato != null)
                {
                    var tempOrder = stato.DisplayOrder;
                    stato.DisplayOrder = nextStato.DisplayOrder;
                    nextStato.DisplayOrder = tempOrder;
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction(nameof(Stati), new { id = stato.AttivitaTipoId });
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: AttivitaTipi/SetDefaultStato/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetDefaultStato(int id)
        {
            var stato = await _context.StatiAttivitaTipo.FindAsync(id);
            if (stato == null) return NotFound();

            // Rimuovi flag default da tutti gli stati di questo tipo
            var statiTipo = await _context.StatiAttivitaTipo
                .Where(s => s.AttivitaTipoId == stato.AttivitaTipoId)
                .ToListAsync();

            foreach (var s in statiTipo)
            {
                s.IsDefault = (s.Id == id);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"'{stato.Nome}' impostato come stato predefinito.";
            return RedirectToAction(nameof(Stati), new { id = stato.AttivitaTipoId });
        }

        // POST: AttivitaTipi/SetFinaleStato/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetFinaleStato(int id)
        {
            var stato = await _context.StatiAttivitaTipo.FindAsync(id);
            if (stato == null) return NotFound();

            // Toggle il flag finale
            stato.IsFinale = !stato.IsFinale;
            await _context.SaveChangesAsync();

            var messaggio = stato.IsFinale 
                ? $"'{stato.Nome}' marcato come stato finale (completamento)." 
                : $"'{stato.Nome}' non è più uno stato finale.";
            TempData["Success"] = messaggio;
            return RedirectToAction(nameof(Stati), new { id = stato.AttivitaTipoId });
        }
    }
}

