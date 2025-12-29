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

            // Trova l'anno precedente
            var annoPrecedente = await _context.AnnualitaFiscali
                .Include(a => a.AttivitaAnnuali)
                    .ThenInclude(aa => aa.AttivitaTipo)
                .Where(a => a.Anno < annualitaDestinazione.Anno)
                .OrderByDescending(a => a.Anno)
                .FirstOrDefaultAsync();

            ViewBag.AnnoPrecedente = annoPrecedente;
            return View(annualitaDestinazione);
        }

        // POST: Annualita/CopiaAttivita/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CopiaAttivitaConfirm(int id, bool copiaClienti)
        {
            var annualitaDestinazione = await _context.AnnualitaFiscali.FindAsync(id);
            if (annualitaDestinazione == null) return NotFound();

            // Trova l'anno precedente
            var annoPrecedente = await _context.AnnualitaFiscali
                .Include(a => a.AttivitaAnnuali)
                    .ThenInclude(aa => aa.ClientiAttivita)
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

            foreach (var attivitaPrecedente in annoPrecedente.AttivitaAnnuali)
            {
                // Verifica se esiste già per l'anno destinazione
                var esisteGia = await _context.AttivitaAnnuali
                    .AnyAsync(aa => aa.AttivitaTipoId == attivitaPrecedente.AttivitaTipoId 
                                 && aa.AnnualitaFiscaleId == id);

                if (!esisteGia)
                {
                    var nuovaAttivitaAnnuale = new AttivitaAnnuale
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

                    // Copia anche i clienti se richiesto
                    if (copiaClienti)
                    {
                        // Trova lo stato default per questa attività (con fallback al primo disponibile)
                        var statoDefault = await _context.StatiAttivitaTipo
                            .Where(s => s.AttivitaTipoId == attivitaPrecedente.AttivitaTipoId && s.IsActive)
                            .OrderByDescending(s => s.IsDefault)
                            .ThenBy(s => s.DisplayOrder)
                            .FirstOrDefaultAsync();

                        foreach (var clienteAttivita in attivitaPrecedente.ClientiAttivita)
                        {
                            var nuovoClienteAttivita = new ClienteAttivita
                            {
                                ClienteId = clienteAttivita.ClienteId,
                                AttivitaAnnualeId = nuovaAttivitaAnnuale.Id,
                                Stato = StatoAttivita.DaFare,
                                StatoAttivitaTipoId = statoDefault?.Id,
                                CreatedAt = DateTime.Now
                            };
                            _context.ClientiAttivita.Add(nuovoClienteAttivita);
                            clientiCopiati++;
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
            TempData["Success"] = messaggio;

            return RedirectToAction(nameof(Index));
        }
    }
}

