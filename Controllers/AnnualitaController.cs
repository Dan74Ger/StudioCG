using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioCG.Web.Data;
using StudioCG.Web.Filters;
using StudioCG.Web.Models;

namespace StudioCG.Web.Controllers
{
    [AdminOnly]
    public class AnnualitaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AnnualitaController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Annualita
        public async Task<IActionResult> Index()
        {
            var annualita = await _context.AnnualitaFiscali
                .Include(a => a.AttivitaAnnuali)
                .OrderByDescending(a => a.Anno)
                .ToListAsync();
            return View(annualita);
        }

        // GET: Annualita/Create
        public IActionResult Create()
        {
            var prossimoAnno = DateTime.Now.Year + 1;
            return View(new AnnualitaFiscale { Anno = prossimoAnno });
        }

        // POST: Annualita/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AnnualitaFiscale model)
        {
            if (await _context.AnnualitaFiscali.AnyAsync(a => a.Anno == model.Anno))
            {
                ModelState.AddModelError("Anno", "Questo anno esiste già.");
            }

            if (ModelState.IsValid)
            {
                model.CreatedAt = DateTime.Now;
                
                // Se è impostato come corrente, rimuovi il flag dagli altri
                if (model.IsCurrent)
                {
                    var altriAnni = await _context.AnnualitaFiscali.Where(a => a.IsCurrent).ToListAsync();
                    foreach (var anno in altriAnni)
                    {
                        anno.IsCurrent = false;
                    }
                }

                _context.AnnualitaFiscali.Add(model);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Anno fiscale {model.Anno} creato con successo.";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // GET: Annualita/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var annualita = await _context.AnnualitaFiscali.FindAsync(id);
            if (annualita == null) return NotFound();

            return View(annualita);
        }

        // POST: Annualita/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AnnualitaFiscale model)
        {
            if (id != model.Id) return NotFound();

            var existingAnno = await _context.AnnualitaFiscali.AsNoTracking()
                .FirstOrDefaultAsync(a => a.Anno == model.Anno && a.Id != id);
            if (existingAnno != null)
            {
                ModelState.AddModelError("Anno", "Questo anno esiste già.");
            }

            if (ModelState.IsValid)
            {
                var annualita = await _context.AnnualitaFiscali.FindAsync(id);
                if (annualita == null) return NotFound();

                // Se è impostato come corrente, rimuovi il flag dagli altri
                if (model.IsCurrent && !annualita.IsCurrent)
                {
                    var altriAnni = await _context.AnnualitaFiscali.Where(a => a.IsCurrent && a.Id != id).ToListAsync();
                    foreach (var anno in altriAnni)
                    {
                        anno.IsCurrent = false;
                    }
                }

                annualita.Anno = model.Anno;
                annualita.Descrizione = model.Descrizione;
                annualita.IsActive = model.IsActive;
                annualita.IsCurrent = model.IsCurrent;

                await _context.SaveChangesAsync();
                TempData["Success"] = "Anno fiscale aggiornato con successo.";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // POST: Annualita/SetCurrent/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetCurrent(int id)
        {
            // Rimuovi flag corrente da tutti
            var tuttiAnni = await _context.AnnualitaFiscali.ToListAsync();
            foreach (var anno in tuttiAnni)
            {
                anno.IsCurrent = (anno.Id == id);
            }
            await _context.SaveChangesAsync();
            TempData["Success"] = "Anno corrente impostato.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Annualita/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var annualita = await _context.AnnualitaFiscali
                .Include(a => a.AttivitaAnnuali)
                    .ThenInclude(aa => aa.ClientiAttivita)
                        .ThenInclude(ca => ca.Valori)
                .Include(a => a.AttivitaAnnuali)
                    .ThenInclude(aa => aa.AttivitaTipo)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (annualita == null) return NotFound();

            // Conta statistiche per avviso
            var totaleAttivita = annualita.AttivitaAnnuali.Count;
            var totaleClientiAttivita = annualita.AttivitaAnnuali.Sum(aa => aa.ClientiAttivita.Count);
            var totaleValori = annualita.AttivitaAnnuali.Sum(aa => aa.ClientiAttivita.Sum(ca => ca.Valori.Count));

            ViewBag.TotaleAttivita = totaleAttivita;
            ViewBag.TotaleClientiAttivita = totaleClientiAttivita;
            ViewBag.TotaleValori = totaleValori;

            return View(annualita);
        }

        // POST: Annualita/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var annualita = await _context.AnnualitaFiscali
                .Include(a => a.AttivitaAnnuali)
                    .ThenInclude(aa => aa.ClientiAttivita)
                        .ThenInclude(ca => ca.Valori)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (annualita != null)
            {
                // Elimina prima tutti i valori delle attività cliente
                foreach (var attivitaAnnuale in annualita.AttivitaAnnuali)
                {
                    foreach (var clienteAttivita in attivitaAnnuale.ClientiAttivita)
                    {
                        if (clienteAttivita.Valori.Any())
                        {
                            _context.ClientiAttivitaValori.RemoveRange(clienteAttivita.Valori);
                        }
                    }
                    
                    // Elimina i clienti attività
                    if (attivitaAnnuale.ClientiAttivita.Any())
                    {
                        _context.ClientiAttivita.RemoveRange(attivitaAnnuale.ClientiAttivita);
                    }
                }

                // Elimina le attività annuali
                if (annualita.AttivitaAnnuali.Any())
                {
                    _context.AttivitaAnnuali.RemoveRange(annualita.AttivitaAnnuali);
                }

                // Infine elimina l'anno fiscale
                _context.AnnualitaFiscali.Remove(annualita);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Anno fiscale {annualita.Anno} eliminato con successo insieme a tutti i dati collegati.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ==================== COPIA ATTIVITÀ DA ANNO PRECEDENTE ====================

        // GET: Annualita/CopiaAttivita/5
        public async Task<IActionResult> CopiaAttivita(int? id)
        {
            if (id == null) return NotFound();

            var annualitaDestinazione = await _context.AnnualitaFiscali
                .Include(a => a.AttivitaAnnuali)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (annualitaDestinazione == null) return NotFound();

            // Trova l'anno precedente con i campi delle attività
            var annoPrecedente = await _context.AnnualitaFiscali
                .Include(a => a.AttivitaAnnuali)
                    .ThenInclude(aa => aa.AttivitaTipo)
                        .ThenInclude(at => at!.Campi.OrderBy(c => c.DisplayOrder))
                .Where(a => a.Anno < annualitaDestinazione.Anno)
                .OrderByDescending(a => a.Anno)
                .FirstOrDefaultAsync();

            ViewBag.AnnoPrecedente = annoPrecedente;
            return View(annualitaDestinazione);
        }

        // POST: Annualita/CopiaAttivita/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CopiaAttivitaConfirm(int id, bool copiaClienti, bool copiaDati = false, IFormCollection? form = null)
        {
            var annualitaDestinazione = await _context.AnnualitaFiscali.FindAsync(id);
            if (annualitaDestinazione == null) return NotFound();

            // Trova l'anno precedente
            var annoPrecedente = await _context.AnnualitaFiscali
                .Include(a => a.AttivitaAnnuali)
                    .ThenInclude(aa => aa.ClientiAttivita)
                        .ThenInclude(ca => ca.Valori)
                .Where(a => a.Anno < annualitaDestinazione.Anno)
                .OrderByDescending(a => a.Anno)
                .FirstOrDefaultAsync();

            if (annoPrecedente == null)
            {
                TempData["Error"] = "Nessun anno precedente trovato.";
                return RedirectToAction(nameof(Index));
            }

            int attivitaCopiate = 0;
            int clientiCopiati = 0;
            int datiCopiati = 0;

            foreach (var attivitaPrecedente in annoPrecedente.AttivitaAnnuali)
            {
                // Ottieni i campi selezionati per questa attività
                HashSet<int>? campiFiltro = null;
                if (copiaDati && form != null)
                {
                    var campiKey = $"campiDaCopiare_{attivitaPrecedente.AttivitaTipoId}";
                    if (form.ContainsKey(campiKey))
                    {
                        var campiSelezionati = form[campiKey].Select(s => int.TryParse(s, out var i) ? i : 0).Where(i => i > 0);
                        campiFiltro = new HashSet<int>(campiSelezionati);
                    }
                }

                // Verifica se esiste già per l'anno destinazione
                var attivitaDestinazioneEsistente = await _context.AttivitaAnnuali
                    .Include(aa => aa.ClientiAttivita)
                        .ThenInclude(ca => ca.Valori)
                    .FirstOrDefaultAsync(aa => aa.AttivitaTipoId == attivitaPrecedente.AttivitaTipoId 
                                 && aa.AnnualitaFiscaleId == id);

                AttivitaAnnuale nuovaAttivitaAnnuale;
                
                if (attivitaDestinazioneEsistente == null)
                {
                    nuovaAttivitaAnnuale = new AttivitaAnnuale
                    {
                        AttivitaTipoId = attivitaPrecedente.AttivitaTipoId,
                        AnnualitaFiscaleId = id,
                        IsActive = attivitaPrecedente.IsActive,
                        DataScadenza = attivitaPrecedente.DataScadenza?.AddYears(1),
                        CreatedAt = DateTime.Now
                    };
                    _context.AttivitaAnnuali.Add(nuovaAttivitaAnnuale);
                    await _context.SaveChangesAsync();
                    attivitaCopiate++;
                }
                else
                {
                    nuovaAttivitaAnnuale = attivitaDestinazioneEsistente;
                }

                // Copia anche i clienti se richiesto
                if (copiaClienti || copiaDati)
                {
                    // Trova lo stato default per questa attività
                    var statoDefault = await _context.StatiAttivitaTipo
                        .Where(s => s.AttivitaTipoId == attivitaPrecedente.AttivitaTipoId && s.IsActive)
                        .OrderByDescending(s => s.IsDefault)
                        .ThenBy(s => s.DisplayOrder)
                        .FirstOrDefaultAsync();

                    // Clienti già assegnati nella destinazione
                    var clientiGiaAssegnati = nuovaAttivitaAnnuale.ClientiAttivita?
                        .ToDictionary(ca => ca.ClienteId) ?? new Dictionary<int, ClienteAttivita>();

                    foreach (var clienteAttivita in attivitaPrecedente.ClientiAttivita)
                    {
                        ClienteAttivita? clienteDestinazione = null;
                        
                        if (!clientiGiaAssegnati.ContainsKey(clienteAttivita.ClienteId))
                        {
                            if (copiaClienti)
                            {
                                clienteDestinazione = new ClienteAttivita
                                {
                                    ClienteId = clienteAttivita.ClienteId,
                                    AttivitaAnnualeId = nuovaAttivitaAnnuale.Id,
                                    Stato = StatoAttivita.DaFare,
                                    StatoAttivitaTipoId = statoDefault?.Id,
                                    Note = clienteAttivita.Note,
                                    CreatedAt = DateTime.Now
                                };
                                _context.ClientiAttivita.Add(clienteDestinazione);
                                await _context.SaveChangesAsync();
                                clientiCopiati++;
                            }
                        }
                        else
                        {
                            clienteDestinazione = clientiGiaAssegnati[clienteAttivita.ClienteId];
                        }

                        // Copia valori se richiesto
                        if (copiaDati && clienteDestinazione != null && clienteAttivita.Valori != null)
                        {
                            foreach (var valore in clienteAttivita.Valori)
                            {
                                // Controlla se il campo è nella lista dei campi da copiare
                                if (campiFiltro != null && !campiFiltro.Contains(valore.AttivitaCampoId))
                                    continue;

                                // Verifica se esiste già un valore per questo campo
                                var valoreEsistente = await _context.ClientiAttivitaValori
                                    .FirstOrDefaultAsync(v => v.ClienteAttivitaId == clienteDestinazione.Id 
                                                           && v.AttivitaCampoId == valore.AttivitaCampoId);

                                if (valoreEsistente == null)
                                {
                                    _context.ClientiAttivitaValori.Add(new ClienteAttivitaValore
                                    {
                                        ClienteAttivitaId = clienteDestinazione.Id,
                                        AttivitaCampoId = valore.AttivitaCampoId,
                                        Valore = valore.Valore
                                    });
                                    datiCopiati++;
                                }
                                else if (string.IsNullOrEmpty(valoreEsistente.Valore))
                                {
                                    valoreEsistente.Valore = valore.Valore;
                                    datiCopiati++;
                                }
                            }
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();

            var messaggio = $"Copiate {attivitaCopiate} attività dall'anno {annoPrecedente.Anno}.";
            if (copiaClienti)
            {
                messaggio += $" Copiati {clientiCopiati} abbinamenti cliente-attività.";
            }
            if (copiaDati && datiCopiati > 0)
            {
                messaggio += $" Copiati {datiCopiati} valori.";
            }
            TempData["Success"] = messaggio;

            return RedirectToAction(nameof(Index));
        }
    }
}

