using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioCG.Web.Data;
using StudioCG.Web.Filters;
using StudioCG.Web.Models;

namespace StudioCG.Web.Controllers
{
    [Authorize]
    public class ClientiController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClientiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Clienti
        public async Task<IActionResult> Index()
        {
            var clienti = await _context.Clienti
                .Where(c => c.IsActive)
                .OrderBy(c => c.RagioneSociale)
                .ToListAsync();
            return View(clienti);
        }

        // GET: Clienti/Details/5
        public async Task<IActionResult> Details(int? id, int? annoId)
        {
            if (id == null) return NotFound();

            var cliente = await _context.Clienti
                .Include(c => c.Soggetti.OrderBy(s => s.TipoSoggetto).ThenBy(s => s.DisplayOrder))
                .Include(c => c.Attivita)
                    .ThenInclude(a => a.AttivitaAnnuale)
                        .ThenInclude(aa => aa!.AttivitaTipo)
                .Include(c => c.Attivita)
                    .ThenInclude(a => a.AttivitaAnnuale)
                        .ThenInclude(aa => aa!.AnnualitaFiscale)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cliente == null) return NotFound();

            // Carica tutti gli anni disponibili
            var tuttiAnni = await _context.AnnualitaFiscali
                .Where(a => a.IsActive)
                .OrderByDescending(a => a.Anno)
                .ToListAsync();
            ViewBag.TuttiAnni = tuttiAnni;

            // Determina l'anno selezionato
            var annoCorrente = annoId.HasValue
                ? await _context.AnnualitaFiscali.FindAsync(annoId)
                : await _context.AnnualitaFiscali.FirstOrDefaultAsync(a => a.IsCurrent);

            if (annoCorrente != null)
            {
                ViewBag.AttivitaDisponibili = await _context.AttivitaAnnuali
                    .Include(aa => aa.AttivitaTipo)
                    .Where(aa => aa.AnnualitaFiscaleId == annoCorrente.Id && aa.IsActive)
                    .OrderBy(aa => aa.AttivitaTipo!.DisplayOrder)
                    .ToListAsync();
                ViewBag.AnnoCorrente = annoCorrente;
                ViewBag.AnnoSelezionato = annoCorrente.Id;
            }

            return View(cliente);
        }

        // GET: Clienti/Create
        public IActionResult Create()
        {
            return View(new Cliente());
        }

        // POST: Clienti/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Cliente model)
        {
            if (ModelState.IsValid)
            {
                model.CreatedAt = DateTime.Now;
                _context.Clienti.Add(model);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Cliente '{model.RagioneSociale}' creato con successo.";
                return RedirectToAction(nameof(Details), new { id = model.Id });
            }
            return View(model);
        }

        // GET: Clienti/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var cliente = await _context.Clienti.FindAsync(id);
            if (cliente == null) return NotFound();

            return View(cliente);
        }

        // POST: Clienti/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Cliente model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var cliente = await _context.Clienti.FindAsync(id);
                if (cliente == null) return NotFound();

                cliente.RagioneSociale = model.RagioneSociale;
                cliente.Indirizzo = model.Indirizzo;
                cliente.Citta = model.Citta;
                cliente.Provincia = model.Provincia;
                cliente.CAP = model.CAP;
                cliente.Email = model.Email;
                cliente.PEC = model.PEC;
                cliente.Telefono = model.Telefono;
                cliente.CodiceFiscale = model.CodiceFiscale;
                cliente.PartitaIVA = model.PartitaIVA;
                cliente.CodiceAteco = model.CodiceAteco;
                cliente.Note = model.Note;
                cliente.IsActive = model.IsActive;

                await _context.SaveChangesAsync();
                TempData["Success"] = "Cliente aggiornato con successo.";
                return RedirectToAction(nameof(Details), new { id = cliente.Id });
            }
            return View(model);
        }

        // GET: Clienti/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var cliente = await _context.Clienti
                .Include(c => c.Soggetti)
                .Include(c => c.Attivita)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cliente == null) return NotFound();

            return View(cliente);
        }

        // POST: Clienti/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var cliente = await _context.Clienti.FindAsync(id);
            if (cliente != null)
            {
                _context.Clienti.Remove(cliente);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Cliente eliminato con successo.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ==================== GESTIONE SOGGETTI ====================

        // POST: Clienti/AddSoggetto
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSoggetto(ClienteSoggetto model)
        {
            if (ModelState.IsValid)
            {
                var maxOrder = await _context.ClientiSoggetti
                    .Where(s => s.ClienteId == model.ClienteId && s.TipoSoggetto == model.TipoSoggetto)
                    .MaxAsync(s => (int?)s.DisplayOrder) ?? 0;
                model.DisplayOrder = maxOrder + 1;

                _context.ClientiSoggetti.Add(model);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Soggetto aggiunto con successo.";
            }
            else
            {
                TempData["Error"] = "Errore nella validazione dei dati.";
            }
            return RedirectToAction(nameof(Details), new { id = model.ClienteId });
        }

        // GET: Clienti/EditSoggetto/5
        public async Task<IActionResult> EditSoggetto(int? id)
        {
            if (id == null) return NotFound();

            var soggetto = await _context.ClientiSoggetti
                .Include(s => s.Cliente)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (soggetto == null) return NotFound();

            return View(soggetto);
        }

        // POST: Clienti/EditSoggetto/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSoggetto(int id, ClienteSoggetto model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var soggetto = await _context.ClientiSoggetti.FindAsync(id);
                if (soggetto == null) return NotFound();

                soggetto.TipoSoggetto = model.TipoSoggetto;
                soggetto.Nome = model.Nome;
                soggetto.Cognome = model.Cognome;
                soggetto.CodiceFiscale = model.CodiceFiscale;
                soggetto.Indirizzo = model.Indirizzo;
                soggetto.Citta = model.Citta;
                soggetto.Provincia = model.Provincia;
                soggetto.CAP = model.CAP;
                soggetto.Email = model.Email;
                soggetto.Telefono = model.Telefono;
                soggetto.QuotaPercentuale = model.QuotaPercentuale;

                await _context.SaveChangesAsync();
                TempData["Success"] = "Soggetto aggiornato con successo.";
                return RedirectToAction(nameof(Details), new { id = soggetto.ClienteId });
            }
            return View(model);
        }

        // POST: Clienti/DeleteSoggetto/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSoggetto(int id)
        {
            var soggetto = await _context.ClientiSoggetti.FindAsync(id);
            if (soggetto != null)
            {
                var clienteId = soggetto.ClienteId;
                _context.ClientiSoggetti.Remove(soggetto);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Soggetto eliminato.";
                return RedirectToAction(nameof(Details), new { id = clienteId });
            }
            return RedirectToAction(nameof(Index));
        }

        // ==================== GESTIONE ATTIVITÀ CLIENTE ====================

        // POST: Clienti/ToggleAttivita
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAttivita(int clienteId, int attivitaAnnualeId, bool assegna, int? annoId)
        {
            if (assegna)
            {
                // Verifica se esiste già
                var exists = await _context.ClientiAttivita
                    .AnyAsync(ca => ca.ClienteId == clienteId && ca.AttivitaAnnualeId == attivitaAnnualeId);

                if (!exists)
                {
                    var clienteAttivita = new ClienteAttivita
                    {
                        ClienteId = clienteId,
                        AttivitaAnnualeId = attivitaAnnualeId,
                        Stato = StatoAttivita.DaFare,
                        CreatedAt = DateTime.Now
                    };
                    _context.ClientiAttivita.Add(clienteAttivita);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Attività assegnata al cliente.";
                }
            }
            else
            {
                var clienteAttivita = await _context.ClientiAttivita
                    .FirstOrDefaultAsync(ca => ca.ClienteId == clienteId && ca.AttivitaAnnualeId == attivitaAnnualeId);

                if (clienteAttivita != null)
                {
                    _context.ClientiAttivita.Remove(clienteAttivita);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Attività rimossa dal cliente.";
                }
            }

            return RedirectToAction(nameof(Details), new { id = clienteId, annoId = annoId });
        }
    }
}

