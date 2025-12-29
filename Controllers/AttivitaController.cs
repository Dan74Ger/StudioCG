using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioCG.Web.Data;
using StudioCG.Web.Models;

namespace StudioCG.Web.Controllers
{
    [Authorize]
    public class AttivitaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AttivitaController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Attivita - Lista attività per anno corrente
        public async Task<IActionResult> Index(int? annoId)
        {
            var annoCorrente = annoId.HasValue
                ? await _context.AnnualitaFiscali.FindAsync(annoId)
                : await _context.AnnualitaFiscali.FirstOrDefaultAsync(a => a.IsCurrent);

            if (annoCorrente == null)
            {
                TempData["Error"] = "Nessun anno fiscale configurato.";
                return View(new List<AttivitaAnnuale>());
            }

            ViewBag.AnnoCorrente = annoCorrente;
            ViewBag.TuttiAnni = await _context.AnnualitaFiscali
                .Where(a => a.IsActive)
                .OrderByDescending(a => a.Anno)
                .ToListAsync();

            var attivita = await _context.AttivitaAnnuali
                .Include(aa => aa.AttivitaTipo)
                    .ThenInclude(at => at!.Stati)
                .Include(aa => aa.ClientiAttivita)
                    .ThenInclude(ca => ca.Cliente)
                .Include(aa => aa.ClientiAttivita)
                    .ThenInclude(ca => ca.StatoAttivitaTipoNav)
                .Where(aa => aa.AnnualitaFiscaleId == annoCorrente.Id && aa.IsActive)
                .OrderBy(aa => aa.AttivitaTipo!.DisplayOrder)
                .ThenBy(aa => aa.AttivitaTipo!.Nome)
                .ToListAsync();

            // Cerca anno precedente per funzione copia
            var annoPrecedente = await _context.AnnualitaFiscali
                .Where(a => a.IsActive && a.Anno < annoCorrente.Anno)
                .OrderByDescending(a => a.Anno)
                .FirstOrDefaultAsync();

            if (annoPrecedente != null)
            {
                ViewBag.AnnoPrecedente = annoPrecedente;
                
                // Conta i clienti per ogni attività dell'anno precedente
                var attivitaAnnoPrecedente = await _context.AttivitaAnnuali
                    .Include(aa => aa.AttivitaTipo)
                    .Include(aa => aa.ClientiAttivita)
                    .Where(aa => aa.AnnualitaFiscaleId == annoPrecedente.Id && aa.IsActive)
                    .ToListAsync();
                
                ViewBag.AttivitaAnnoPrecedente = attivitaAnnoPrecedente;
            }

            return View(attivita);
        }

        // GET: Attivita/Tipo/5 - Dettaglio attività con lista clienti
        public async Task<IActionResult> Tipo(int? id, int? annoId, string? searchNome, int? filtroStatoId, string? ordinamento)
        {
            if (id == null) return NotFound();

            var annoCorrente = annoId.HasValue
                ? await _context.AnnualitaFiscali.FindAsync(annoId)
                : await _context.AnnualitaFiscali.FirstOrDefaultAsync(a => a.IsCurrent);

            if (annoCorrente == null)
            {
                TempData["Error"] = "Nessun anno fiscale configurato.";
                return RedirectToAction(nameof(Index));
            }

            var attivitaAnnuale = await _context.AttivitaAnnuali
                .Include(aa => aa.AttivitaTipo)
                    .ThenInclude(at => at!.Campi.OrderBy(c => c.DisplayOrder))
                .Include(aa => aa.AttivitaTipo)
                    .ThenInclude(at => at!.Stati.OrderBy(s => s.DisplayOrder))
                .Include(aa => aa.AnnualitaFiscale)
                .Include(aa => aa.ClientiAttivita)
                    .ThenInclude(ca => ca.Cliente)
                .Include(aa => aa.ClientiAttivita)
                    .ThenInclude(ca => ca.Valori)
                .Include(aa => aa.ClientiAttivita)
                    .ThenInclude(ca => ca.StatoAttivitaTipoNav)
                .FirstOrDefaultAsync(aa => aa.AttivitaTipoId == id && aa.AnnualitaFiscaleId == annoCorrente.Id);

            if (attivitaAnnuale == null)
            {
                TempData["Error"] = "Attività non trovata per questo anno.";
                return RedirectToAction(nameof(Index));
            }

            // Carica gli stati disponibili per questa attività
            var statiDisponibili = attivitaAnnuale.AttivitaTipo?.Stati
                .Where(s => s.IsActive)
                .OrderBy(s => s.DisplayOrder)
                .ToList() ?? new List<StatoAttivitaTipo>();
            ViewBag.StatiDisponibili = statiDisponibili;

            // Applica filtri ai ClientiAttivita
            var clientiFiltrati = attivitaAnnuale.ClientiAttivita.AsEnumerable();

            // Filtro per nome cliente
            if (!string.IsNullOrWhiteSpace(searchNome))
            {
                clientiFiltrati = clientiFiltrati.Where(ca => 
                    ca.Cliente != null && ca.Cliente.RagioneSociale.Contains(searchNome, StringComparison.OrdinalIgnoreCase));
            }

            // Filtro per stato dinamico
            if (filtroStatoId.HasValue)
            {
                clientiFiltrati = clientiFiltrati.Where(ca => ca.StatoAttivitaTipoId == filtroStatoId.Value);
            }

            // Ordinamento
            if (ordinamento == "desc")
            {
                clientiFiltrati = clientiFiltrati.OrderByDescending(ca => ca.Cliente?.RagioneSociale);
            }
            else
            {
                clientiFiltrati = clientiFiltrati.OrderBy(ca => ca.Cliente?.RagioneSociale);
            }

            // Sostituisci la collection con quella filtrata
            attivitaAnnuale.ClientiAttivita = clientiFiltrati.ToList();

            // Passa i parametri di filtro alla view
            ViewBag.SearchNome = searchNome;
            ViewBag.FiltroStatoId = filtroStatoId;
            ViewBag.Ordinamento = ordinamento ?? "asc";

            ViewBag.TuttiAnni = await _context.AnnualitaFiscali
                .Where(a => a.IsActive)
                .OrderByDescending(a => a.Anno)
                .ToListAsync();

            // Clienti non ancora assegnati a questa attività (usa i dati originali non filtrati)
            var clientiGiaAssegnati = await _context.ClientiAttivita
                .Where(ca => ca.AttivitaAnnualeId == attivitaAnnuale.Id)
                .Select(ca => ca.ClienteId)
                .ToListAsync();
            ViewBag.ClientiDisponibili = await _context.Clienti
                .Where(c => c.IsActive && !clientiGiaAssegnati.Contains(c.Id))
                .OrderBy(c => c.RagioneSociale)
                .ToListAsync();

            // Cerca anno precedente con clienti assegnati per questa attività
            var annoPrecedente = await _context.AnnualitaFiscali
                .Where(a => a.IsActive && a.Anno < annoCorrente.Anno)
                .OrderByDescending(a => a.Anno)
                .FirstOrDefaultAsync();

            if (annoPrecedente != null)
            {
                var attivitaAnnoPrecedente = await _context.AttivitaAnnuali
                    .Include(aa => aa.ClientiAttivita)
                    .FirstOrDefaultAsync(aa => aa.AttivitaTipoId == id && aa.AnnualitaFiscaleId == annoPrecedente.Id);

                if (attivitaAnnoPrecedente != null && attivitaAnnoPrecedente.ClientiAttivita.Any())
                {
                    ViewBag.AnnoPrecedente = annoPrecedente;
                    ViewBag.ClientiAnnoPrecedente = attivitaAnnoPrecedente.ClientiAttivita.Count;
                }
            }

            return View(attivitaAnnuale);
        }

        // GET: Attivita/ClienteAttivita/5 - Modifica attività per singolo cliente
        public async Task<IActionResult> ClienteAttivita(int? id)
        {
            if (id == null) return NotFound();

            var clienteAttivita = await _context.ClientiAttivita
                .Include(ca => ca.Cliente)
                .Include(ca => ca.AttivitaAnnuale)
                    .ThenInclude(aa => aa!.AttivitaTipo)
                        .ThenInclude(at => at!.Campi.OrderBy(c => c.DisplayOrder))
                .Include(ca => ca.AttivitaAnnuale)
                    .ThenInclude(aa => aa!.AttivitaTipo)
                        .ThenInclude(at => at!.Stati.OrderBy(s => s.DisplayOrder))
                .Include(ca => ca.AttivitaAnnuale)
                    .ThenInclude(aa => aa!.AnnualitaFiscale)
                .Include(ca => ca.Valori)
                .Include(ca => ca.StatoAttivitaTipoNav)
                .FirstOrDefaultAsync(ca => ca.Id == id);

            if (clienteAttivita == null) return NotFound();

            // Carica gli stati disponibili
            ViewBag.StatiDisponibili = clienteAttivita.AttivitaAnnuale?.AttivitaTipo?.Stati
                .Where(s => s.IsActive)
                .OrderBy(s => s.DisplayOrder)
                .ToList() ?? new List<StatoAttivitaTipo>();

            return View(clienteAttivita);
        }

        // POST: Attivita/ClienteAttivita/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClienteAttivita(int id, int? statoId, string? note, IFormCollection form)
        {
            var clienteAttivita = await _context.ClientiAttivita
                .Include(ca => ca.Valori)
                .Include(ca => ca.AttivitaAnnuale)
                    .ThenInclude(aa => aa!.AttivitaTipo)
                        .ThenInclude(at => at!.Campi)
                .FirstOrDefaultAsync(ca => ca.Id == id);

            if (clienteAttivita == null) return NotFound();

            // Carica il nuovo stato
            StatoAttivitaTipo? nuovoStato = null;
            if (statoId.HasValue)
            {
                nuovoStato = await _context.StatiAttivitaTipo.FindAsync(statoId.Value);
            }

            // Aggiorna stato dinamico e note
            clienteAttivita.StatoAttivitaTipoId = statoId;
            clienteAttivita.Note = note;
            clienteAttivita.UpdatedAt = DateTime.Now;

            // Gestione data completamento basata su stato dinamico
            if (nuovoStato != null && nuovoStato.IsFinale && !clienteAttivita.DataCompletamento.HasValue)
            {
                clienteAttivita.DataCompletamento = DateTime.Now;
            }
            else if (nuovoStato != null && nuovoStato.IsDefault)
            {
                clienteAttivita.DataCompletamento = null;
            }

            // Aggiorna valori campi - gestisce correttamente i checkbox
            var campiAttivita = clienteAttivita.AttivitaAnnuale?.AttivitaTipo?.Campi ?? new List<AttivitaCampo>();
            
            foreach (var campo in campiAttivita)
            {
                string valoreDaSalvare;
                var formKey = $"campi[{campo.Id}]";
                
                if (campo.FieldType == AttivitaFieldType.Boolean)
                {
                    // Per i checkbox, verifica se c'è "true" tra i valori inviati
                    var valori = form[formKey];
                    valoreDaSalvare = valori.Contains("true") ? "true" : "false";
                }
                else
                {
                    valoreDaSalvare = form[formKey].ToString();
                }
                
                var valoreEsistente = clienteAttivita.Valori.FirstOrDefault(v => v.AttivitaCampoId == campo.Id);
                if (valoreEsistente != null)
                {
                    valoreEsistente.Valore = valoreDaSalvare;
                }
                else
                {
                    clienteAttivita.Valori.Add(new ClienteAttivitaValore
                    {
                        ClienteAttivitaId = id,
                        AttivitaCampoId = campo.Id,
                        Valore = valoreDaSalvare
                    });
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Attività aggiornata con successo.";

            return RedirectToAction(nameof(Tipo), new { 
                id = clienteAttivita.AttivitaAnnuale!.AttivitaTipoId,
                annoId = clienteAttivita.AttivitaAnnuale.AnnualitaFiscaleId
            });
        }

        // POST: Attivita/CambiaStato (legacy - manteniamo per retrocompatibilità)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiaStato(int id, StatoAttivita nuovoStato, int? returnToTipoId, int? annoId)
        {
            var clienteAttivita = await _context.ClientiAttivita
                .Include(ca => ca.AttivitaAnnuale)
                .FirstOrDefaultAsync(ca => ca.Id == id);

            if (clienteAttivita == null) return NotFound();

            clienteAttivita.Stato = nuovoStato;
            clienteAttivita.UpdatedAt = DateTime.Now;

            if (nuovoStato == StatoAttivita.DRInviate)
            {
                clienteAttivita.DataCompletamento = DateTime.Now;
            }
            else if (nuovoStato == StatoAttivita.DaFare || nuovoStato == StatoAttivita.Sospesa)
            {
                clienteAttivita.DataCompletamento = null;
            }

            await _context.SaveChangesAsync();

            if (returnToTipoId.HasValue)
            {
                return RedirectToAction(nameof(Tipo), new { id = returnToTipoId, annoId = annoId ?? clienteAttivita.AttivitaAnnuale?.AnnualitaFiscaleId });
            }
            return RedirectToAction(nameof(Index), new { annoId = annoId });
        }

        // POST: Attivita/CambiaStatoDinamico - Nuovo metodo per stati dinamici
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiaStatoDinamico(int id, int nuovoStatoId, int? returnToTipoId, int? annoId)
        {
            var clienteAttivita = await _context.ClientiAttivita
                .Include(ca => ca.AttivitaAnnuale)
                .FirstOrDefaultAsync(ca => ca.Id == id);

            if (clienteAttivita == null) return NotFound();

            // Verifica che lo stato esista
            var nuovoStato = await _context.StatiAttivitaTipo.FindAsync(nuovoStatoId);
            if (nuovoStato == null) return NotFound();

            clienteAttivita.StatoAttivitaTipoId = nuovoStatoId;
            clienteAttivita.UpdatedAt = DateTime.Now;

            // Se lo stato è finale, imposta la data di completamento
            if (nuovoStato.IsFinale && !clienteAttivita.DataCompletamento.HasValue)
            {
                clienteAttivita.DataCompletamento = DateTime.Now;
            }
            // Se lo stato è default (iniziale), rimuove la data di completamento
            else if (nuovoStato.IsDefault)
            {
                clienteAttivita.DataCompletamento = null;
            }

            await _context.SaveChangesAsync();

            if (returnToTipoId.HasValue)
            {
                return RedirectToAction(nameof(Tipo), new { id = returnToTipoId, annoId = annoId ?? clienteAttivita.AttivitaAnnuale?.AnnualitaFiscaleId });
            }
            return RedirectToAction(nameof(Index), new { annoId = annoId });
        }

        // POST: Attivita/NuovoClienteAttivita - Aggiunge un cliente all'attività
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NuovoClienteAttivita(int attivitaAnnualeId, int clienteId)
        {
            var attivitaAnnuale = await _context.AttivitaAnnuali
                .Include(aa => aa.AttivitaTipo)
                .FirstOrDefaultAsync(aa => aa.Id == attivitaAnnualeId);
            if (attivitaAnnuale == null) return NotFound();

            // Verifica che il cliente non sia già assegnato
            var giàAssegnato = await _context.ClientiAttivita
                .AnyAsync(ca => ca.AttivitaAnnualeId == attivitaAnnualeId && ca.ClienteId == clienteId);

            if (giàAssegnato)
            {
                TempData["Error"] = "Il cliente è già assegnato a questa attività.";
                return RedirectToAction(nameof(Tipo), new { id = attivitaAnnuale.AttivitaTipoId, annoId = attivitaAnnuale.AnnualitaFiscaleId });
            }

            // Trova lo stato default per questo tipo di attività (con fallback al primo disponibile)
            var statoDefault = await _context.StatiAttivitaTipo
                .Where(s => s.AttivitaTipoId == attivitaAnnuale.AttivitaTipoId && s.IsActive)
                .OrderByDescending(s => s.IsDefault) // Prima quelli default
                .ThenBy(s => s.DisplayOrder)         // Poi per ordine
                .FirstOrDefaultAsync();

            var clienteAttivita = new ClienteAttivita
            {
                ClienteId = clienteId,
                AttivitaAnnualeId = attivitaAnnualeId,
                Stato = StatoAttivita.DaFare, // Manteniamo per retrocompatibilità
                StatoAttivitaTipoId = statoDefault?.Id,
                CreatedAt = DateTime.Now
            };

            _context.ClientiAttivita.Add(clienteAttivita);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cliente aggiunto all'attività.";
            return RedirectToAction(nameof(ClienteAttivita), new { id = clienteAttivita.Id });
        }

        // POST: Attivita/AssegnaTutti - Assegna attività a tutti i clienti attivi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssegnaTutti(int attivitaAnnualeId)
        {
            var attivitaAnnuale = await _context.AttivitaAnnuali.FindAsync(attivitaAnnualeId);
            if (attivitaAnnuale == null) return NotFound();

            // Trova lo stato default per questo tipo di attività (con fallback al primo disponibile)
            var statoDefault = await _context.StatiAttivitaTipo
                .Where(s => s.AttivitaTipoId == attivitaAnnuale.AttivitaTipoId && s.IsActive)
                .OrderByDescending(s => s.IsDefault)
                .ThenBy(s => s.DisplayOrder)
                .FirstOrDefaultAsync();

            var clientiAttivi = await _context.Clienti
                .Where(c => c.IsActive)
                .ToListAsync();

            var clientiGiaAssegnati = await _context.ClientiAttivita
                .Where(ca => ca.AttivitaAnnualeId == attivitaAnnualeId)
                .Select(ca => ca.ClienteId)
                .ToListAsync();

            int nuoviAssegnati = 0;
            foreach (var cliente in clientiAttivi)
            {
                if (!clientiGiaAssegnati.Contains(cliente.Id))
                {
                    _context.ClientiAttivita.Add(new ClienteAttivita
                    {
                        ClienteId = cliente.Id,
                        AttivitaAnnualeId = attivitaAnnualeId,
                        Stato = StatoAttivita.DaFare,
                        StatoAttivitaTipoId = statoDefault?.Id,
                        CreatedAt = DateTime.Now
                    });
                    nuoviAssegnati++;
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Attività assegnata a {nuoviAssegnati} nuovi clienti.";

            return RedirectToAction(nameof(Tipo), new { 
                id = attivitaAnnuale.AttivitaTipoId,
                annoId = attivitaAnnuale.AnnualitaFiscaleId
            });
        }

        // POST: Attivita/CopiaMultiplaClienti - Copia clienti per attività selezionate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CopiaMultiplaClienti(List<int> attivitaTipoIds, int annoPrecedenteId, int annoCorrenteId)
        {
            var annoCorrente = await _context.AnnualitaFiscali.FindAsync(annoCorrenteId);
            var annoPrecedente = await _context.AnnualitaFiscali.FindAsync(annoPrecedenteId);
            
            if (annoCorrente == null || annoPrecedente == null)
            {
                TempData["Error"] = "Errore nella configurazione degli anni.";
                return RedirectToAction(nameof(Index), new { annoId = annoCorrenteId });
            }

            int totaleCopiati = 0;
            int attivitaProcessate = 0;

            foreach (var tipoId in attivitaTipoIds)
            {
                // Trova l'attività annuale per l'anno corrente
                var attivitaDestinazioneRecord = await _context.AttivitaAnnuali
                    .Include(aa => aa.ClientiAttivita)
                    .FirstOrDefaultAsync(aa => aa.AttivitaTipoId == tipoId && aa.AnnualitaFiscaleId == annoCorrenteId);

                // Trova l'attività dell'anno precedente
                var attivitaSorgente = await _context.AttivitaAnnuali
                    .Include(aa => aa.ClientiAttivita)
                    .FirstOrDefaultAsync(aa => aa.AttivitaTipoId == tipoId && aa.AnnualitaFiscaleId == annoPrecedenteId);

                if (attivitaDestinazioneRecord == null || attivitaSorgente == null)
                    continue;

                // Clienti già assegnati nella destinazione
                var clientiGiaAssegnati = attivitaDestinazioneRecord.ClientiAttivita
                    .Select(ca => ca.ClienteId)
                    .ToHashSet();

                // Trova lo stato default per questa attività (con fallback al primo disponibile)
                var statoDefault = await _context.StatiAttivitaTipo
                    .Where(s => s.AttivitaTipoId == tipoId && s.IsActive)
                    .OrderByDescending(s => s.IsDefault)
                    .ThenBy(s => s.DisplayOrder)
                    .FirstOrDefaultAsync();

                foreach (var clienteAttivita in attivitaSorgente.ClientiAttivita)
                {
                    if (!clientiGiaAssegnati.Contains(clienteAttivita.ClienteId))
                    {
                        _context.ClientiAttivita.Add(new ClienteAttivita
                        {
                            ClienteId = clienteAttivita.ClienteId,
                            AttivitaAnnualeId = attivitaDestinazioneRecord.Id,
                            Stato = StatoAttivita.DaFare,
                            StatoAttivitaTipoId = statoDefault?.Id,
                            CreatedAt = DateTime.Now
                        });
                        totaleCopiati++;
                    }
                }
                attivitaProcessate++;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Copiati {totaleCopiati} clienti in {attivitaProcessate} attività dal {annoPrecedente.Anno} al {annoCorrente.Anno}.";
            return RedirectToAction(nameof(Index), new { annoId = annoCorrenteId });
        }

        // POST: Attivita/CopiaClientiDaAnno - Copia clienti da anno precedente
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CopiaClientiDaAnno(int attivitaAnnualeId, int annoPrecedenteId)
        {
            var attivitaAnnualeDestinazione = await _context.AttivitaAnnuali
                .Include(aa => aa.ClientiAttivita)
                .FirstOrDefaultAsync(aa => aa.Id == attivitaAnnualeId);

            if (attivitaAnnualeDestinazione == null) return NotFound();

            // Trova l'attività dello stesso tipo nell'anno precedente
            var attivitaAnnoPrecedente = await _context.AttivitaAnnuali
                .Include(aa => aa.ClientiAttivita)
                .FirstOrDefaultAsync(aa => aa.AttivitaTipoId == attivitaAnnualeDestinazione.AttivitaTipoId 
                    && aa.AnnualitaFiscaleId == annoPrecedenteId);

            if (attivitaAnnoPrecedente == null)
            {
                TempData["Error"] = "Attività non trovata nell'anno precedente.";
                return RedirectToAction(nameof(Tipo), new { 
                    id = attivitaAnnualeDestinazione.AttivitaTipoId,
                    annoId = attivitaAnnualeDestinazione.AnnualitaFiscaleId 
                });
            }

            // Clienti già assegnati nella destinazione
            var clientiGiaAssegnati = attivitaAnnualeDestinazione.ClientiAttivita
                .Select(ca => ca.ClienteId)
                .ToHashSet();

            // Trova lo stato default per questa attività (con fallback al primo disponibile)
            var statoDefault = await _context.StatiAttivitaTipo
                .Where(s => s.AttivitaTipoId == attivitaAnnualeDestinazione.AttivitaTipoId && s.IsActive)
                .OrderByDescending(s => s.IsDefault)
                .ThenBy(s => s.DisplayOrder)
                .FirstOrDefaultAsync();

            int copiati = 0;
            foreach (var clienteAttivita in attivitaAnnoPrecedente.ClientiAttivita)
            {
                if (!clientiGiaAssegnati.Contains(clienteAttivita.ClienteId))
                {
                    _context.ClientiAttivita.Add(new ClienteAttivita
                    {
                        ClienteId = clienteAttivita.ClienteId,
                        AttivitaAnnualeId = attivitaAnnualeId,
                        Stato = StatoAttivita.DaFare,
                        StatoAttivitaTipoId = statoDefault?.Id,
                        CreatedAt = DateTime.Now
                    });
                    copiati++;
                }
            }

            await _context.SaveChangesAsync();

            var annoPrecedente = await _context.AnnualitaFiscali.FindAsync(annoPrecedenteId);
            var annoDestinazione = await _context.AnnualitaFiscali.FindAsync(attivitaAnnualeDestinazione.AnnualitaFiscaleId);
            TempData["Success"] = $"Copiati {copiati} clienti dal {annoPrecedente?.Anno} al {annoDestinazione?.Anno}.";

            return RedirectToAction(nameof(Tipo), new { 
                id = attivitaAnnualeDestinazione.AttivitaTipoId,
                annoId = attivitaAnnualeDestinazione.AnnualitaFiscaleId 
            });
        }

        // GET: Attivita/ExportExcel/5 - Esporta l'attività in Excel
        public async Task<IActionResult> ExportExcel(int? id)
        {
            if (id == null) return NotFound();

            var attivitaAnnuale = await _context.AttivitaAnnuali
                .Include(aa => aa.AttivitaTipo)
                    .ThenInclude(at => at!.Campi.OrderBy(c => c.DisplayOrder))
                .Include(aa => aa.AnnualitaFiscale)
                .Include(aa => aa.ClientiAttivita)
                    .ThenInclude(ca => ca.Cliente)
                .Include(aa => aa.ClientiAttivita)
                    .ThenInclude(ca => ca.Valori)
                .Include(aa => aa.ClientiAttivita)
                    .ThenInclude(ca => ca.StatoAttivitaTipoNav)
                .FirstOrDefaultAsync(aa => aa.Id == id);

            if (attivitaAnnuale == null) return NotFound();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add($"{attivitaAnnuale.AttivitaTipo?.Nome ?? "Attività"}");

            // Stile intestazione
            var headerStyle = workbook.Style;
            
            // Intestazioni
            int col = 1;
            worksheet.Cell(1, col++).Value = "Cliente";
            worksheet.Cell(1, col++).Value = "Cod. Ateco";
            worksheet.Cell(1, col++).Value = "Stato";
            
            var campi = attivitaAnnuale.AttivitaTipo?.Campi.OrderBy(c => c.DisplayOrder).ToList() ?? new List<AttivitaCampo>();
            foreach (var campo in campi)
            {
                worksheet.Cell(1, col++).Value = campo.Label;
            }
            worksheet.Cell(1, col++).Value = "Note";

            // Stile intestazione
            var headerRange = worksheet.Range(1, 1, 1, col - 1);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Dati
            int row = 2;
            foreach (var ca in attivitaAnnuale.ClientiAttivita.OrderBy(c => c.Cliente?.RagioneSociale))
            {
                col = 1;
                worksheet.Cell(row, col++).Value = ca.Cliente?.RagioneSociale ?? "";
                worksheet.Cell(row, col++).Value = ca.Cliente?.CodiceAteco ?? "";
                
                // Stato - usa stato dinamico se disponibile, altrimenti legacy
                var statoText = ca.StatoAttivitaTipoNav?.Nome ?? ca.Stato switch
                {
                    StatoAttivita.DaFare => "Da Fare",
                    StatoAttivita.Completata => "Completata",
                    StatoAttivita.DaInviareEntratel => "Da inviare Entratel",
                    StatoAttivita.DRInviate => "DR Inviate",
                    StatoAttivita.Sospesa => "Sospesa",
                    _ => ""
                };
                worksheet.Cell(row, col++).Value = statoText;

                // Campi dinamici
                foreach (var campo in campi)
                {
                    var valore = ca.Valori.FirstOrDefault(v => v.AttivitaCampoId == campo.Id)?.Valore ?? "";
                    
                    if (campo.FieldType == AttivitaFieldType.Boolean)
                    {
                        worksheet.Cell(row, col++).Value = valore == "true" ? "Sì" : "No";
                    }
                    else if (campo.FieldType == AttivitaFieldType.Date && DateTime.TryParse(valore, out var dataVal))
                    {
                        worksheet.Cell(row, col).Value = dataVal;
                        worksheet.Cell(row, col++).Style.DateFormat.Format = "dd/MM/yyyy";
                    }
                    else
                    {
                        worksheet.Cell(row, col++).Value = valore;
                    }
                }

                // Note
                worksheet.Cell(row, col++).Value = ca.Note ?? "";
                
                row++;
            }

            // Centra tutte le celle dati
            var totalCols = 3 + campi.Count + 1; // Cliente, Ateco, Stato + campi + Note
            if (row > 2)
            {
                var dataRange = worksheet.Range(2, 1, row - 1, totalCols);
                dataRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                dataRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }

            // Auto-fit colonne
            worksheet.Columns().AdjustToContents();

            // Genera file
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"{attivitaAnnuale.AttivitaTipo?.Nome ?? "Attivita"}_{attivitaAnnuale.AnnualitaFiscale?.Anno}_{DateTime.Now:yyyyMMdd}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // ==================== FIX STATI NULLI ====================
        // POST: Attivita/FixStatiNulli - Corregge tutti i ClienteAttivita senza stato assegnato
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FixStatiNulli()
        {
            // Trova tutti i ClienteAttivita con StatoAttivitaTipoId nullo
            var clientiAttivitaSenzaStato = await _context.ClientiAttivita
                .Include(ca => ca.AttivitaAnnuale)
                .Where(ca => ca.StatoAttivitaTipoId == null)
                .ToListAsync();

            if (!clientiAttivitaSenzaStato.Any())
            {
                TempData["Success"] = "Nessuna attività cliente da correggere.";
                return RedirectToAction(nameof(Index));
            }

            int corretti = 0;
            var statiCache = new Dictionary<int, int?>(); // Cache degli stati default per tipo

            foreach (var ca in clientiAttivitaSenzaStato)
            {
                if (ca.AttivitaAnnuale == null) continue;
                
                var tipoId = ca.AttivitaAnnuale.AttivitaTipoId;
                
                // Usa cache per evitare query ripetute
                if (!statiCache.TryGetValue(tipoId, out var statoId))
                {
                    var statoDefault = await _context.StatiAttivitaTipo
                        .Where(s => s.AttivitaTipoId == tipoId && s.IsActive)
                        .OrderByDescending(s => s.IsDefault)
                        .ThenBy(s => s.DisplayOrder)
                        .FirstOrDefaultAsync();
                    
                    statoId = statoDefault?.Id;
                    statiCache[tipoId] = statoId;
                }

                if (statoId.HasValue)
                {
                    ca.StatoAttivitaTipoId = statoId.Value;
                    corretti++;
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Corretti {corretti} record con stato nullo. Assegnato stato di default.";
            return RedirectToAction(nameof(Index));
        }
    }
}

