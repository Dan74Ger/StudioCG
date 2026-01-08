using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using StudioCG.Web.Data;
using StudioCG.Web.Filters;
using StudioCG.Web.Models;
using StudioCG.Web.Models.AttivitaPeriodiche;

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
        public async Task<IActionResult> Index(string? searchRagioneSociale, string? searchTipoSoggetto, int? searchAttivitaId)
        {
            // Carica l'anno corrente
            var annoCorrente = await _context.AnnualitaFiscali.FirstOrDefaultAsync(a => a.IsCurrent);
            ViewBag.AnnoCorrente = annoCorrente;
            ViewBag.AnnoCorrenteId = annoCorrente?.Id;

            // Carica le attività disponibili per il filtro
            if (annoCorrente != null)
            {
                ViewBag.AttivitaDisponibili = await _context.AttivitaAnnuali
                    .Include(aa => aa.AttivitaTipo)
                    .Where(aa => aa.AnnualitaFiscaleId == annoCorrente.Id && aa.IsActive)
                    .OrderBy(aa => aa.AttivitaTipo!.DisplayOrder)
                    .ToListAsync();
            }

            // Valori di ricerca per mantenere i filtri
            ViewBag.SearchRagioneSociale = searchRagioneSociale;
            ViewBag.SearchTipoSoggetto = searchTipoSoggetto;
            ViewBag.SearchAttivitaId = searchAttivitaId;

            // Query base
            var query = _context.Clienti
                .Include(c => c.Attivita)
                    .ThenInclude(a => a.AttivitaAnnuale)
                        .ThenInclude(aa => aa!.AttivitaTipo)
                .Where(c => c.IsActive);

            // Filtro per Ragione Sociale
            if (!string.IsNullOrWhiteSpace(searchRagioneSociale))
            {
                query = query.Where(c => c.RagioneSociale.Contains(searchRagioneSociale));
            }

            // Filtro per Tipo Soggetto
            if (!string.IsNullOrWhiteSpace(searchTipoSoggetto))
            {
                query = query.Where(c => c.TipoSoggetto == searchTipoSoggetto);
            }

            // Filtro per Attività
            if (searchAttivitaId.HasValue)
            {
                query = query.Where(c => c.Attivita.Any(a => a.AttivitaAnnualeId == searchAttivitaId));
            }

            var clienti = await query
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
                .Include(c => c.Attivita)
                    .ThenInclude(a => a.StatoAttivitaTipoNav)
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

            // Carica campi custom per visualizzazione
            ViewBag.CampiCustom = await _context.CampiCustomClienti
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            // Carica valori campi custom del cliente
            ViewBag.ValoriCampiCustom = await _context.ValoriCampiCustomClienti
                .Where(v => v.ClienteId == id)
                .ToDictionaryAsync(v => v.CampoCustomClienteId, v => v.Valore);

            // Carica entità dinamiche collegate a cliente
            var entitaCollegate = await _context.EntitaDinamiche
                .Where(e => e.IsActive && e.CollegataACliente)
                .OrderBy(e => e.DisplayOrder)
                .ThenBy(e => e.Nome)
                .ToListAsync();
            ViewBag.EntitaCollegate = entitaCollegate;

            // Conta i record per entità del cliente
            var recordsPerEntita = new Dictionary<int, int>();
            foreach (var entita in entitaCollegate)
            {
                var count = await _context.RecordsEntita
                    .CountAsync(r => r.EntitaDinamicaId == entita.Id && r.ClienteId == id);
                recordsPerEntita[entita.Id] = count;
            }
            ViewBag.RecordsPerEntita = recordsPerEntita;

            // Carica Attività Periodiche collegate al cliente
            var attivitaPeriodiche = await _context.ClientiAttivitaPeriodiche
                .Include(cap => cap.TipoPeriodo)
                    .ThenInclude(tp => tp!.AttivitaPeriodica)
                .Where(cap => cap.ClienteId == id && cap.IsActive)
                .ToListAsync();
            
            // Raggruppa per AttivitaPeriodica
            var attivitaPeriodicheRaggruppate = attivitaPeriodiche
                .Where(cap => cap.TipoPeriodo?.AttivitaPeriodica != null)
                .GroupBy(cap => cap.TipoPeriodo!.AttivitaPeriodica!)
                .Select(g => new {
                    Attivita = g.Key,
                    TipiPeriodo = g.Select(cap => cap.TipoPeriodo).Distinct().ToList(),
                    ClienteAttivitaIds = g.ToDictionary(cap => cap.TipoPeriodoId, cap => cap.Id)
                })
                .ToList();
            ViewBag.AttivitaPeriodiche = attivitaPeriodicheRaggruppate;
            
            // Carica TUTTE le attività periodiche disponibili per assegnazione
            var tutteAttivitaPeriodiche = await _context.AttivitaPeriodiche
                .Include(ap => ap.TipiPeriodo)
                .Where(ap => ap.IsActive)
                .OrderBy(ap => ap.Nome)
                .ToListAsync();
            ViewBag.TutteAttivitaPeriodiche = tutteAttivitaPeriodiche;
            
            // IDs delle attività periodiche già assegnate (per tipo periodo)
            var tipiPeriodoAssegnati = attivitaPeriodiche.Select(cap => cap.TipoPeriodoId).ToHashSet();
            ViewBag.TipiPeriodoAssegnati = tipiPeriodoAssegnati;

            return View(cliente);
        }

        // GET: Clienti/Create
        public async Task<IActionResult> Create()
        {
            // Carica campi custom per il form
            ViewBag.CampiCustom = await _context.CampiCustomClienti
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            return View(new Cliente());
        }

        // POST: Clienti/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Cliente model, IFormCollection form)
        {
            if (ModelState.IsValid)
            {
                model.CreatedAt = DateTime.Now;
                _context.Clienti.Add(model);
                await _context.SaveChangesAsync();

                // Salva i valori dei campi custom
                await SalvaCampiCustom(model.Id, form);

                TempData["Success"] = $"Cliente '{model.RagioneSociale}' creato con successo.";
                return RedirectToAction(nameof(Details), new { id = model.Id });
            }

            // Ricarica campi custom in caso di errore
            ViewBag.CampiCustom = await _context.CampiCustomClienti
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            return View(model);
        }

        // GET: Clienti/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var cliente = await _context.Clienti.FindAsync(id);
            if (cliente == null) return NotFound();

            // Carica campi custom
            ViewBag.CampiCustom = await _context.CampiCustomClienti
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            // Carica valori esistenti
            ViewBag.ValoriCampiCustom = await _context.ValoriCampiCustomClienti
                .Where(v => v.ClienteId == id)
                .ToDictionaryAsync(v => v.CampoCustomClienteId, v => v.Valore);

            return View(cliente);
        }

        // POST: Clienti/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Cliente model, IFormCollection form)
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
                cliente.TipoSoggetto = model.TipoSoggetto;
                cliente.Note = model.Note;
                cliente.IsActive = model.IsActive;

                await _context.SaveChangesAsync();

                // Salva i valori dei campi custom
                await SalvaCampiCustom(id, form);

                TempData["Success"] = "Cliente aggiornato con successo.";
                return RedirectToAction(nameof(Details), new { id = cliente.Id });
            }

            // Ricarica campi custom in caso di errore
            ViewBag.CampiCustom = await _context.CampiCustomClienti
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            ViewBag.ValoriCampiCustom = await _context.ValoriCampiCustomClienti
                .Where(v => v.ClienteId == id)
                .ToDictionaryAsync(v => v.CampoCustomClienteId, v => v.Valore);

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
                
                // Documento di identità
                soggetto.DocumentoNumero = model.DocumentoNumero;
                soggetto.DocumentoRilasciatoDa = model.DocumentoRilasciatoDa;
                soggetto.DocumentoDataRilascio = model.DocumentoDataRilascio;
                soggetto.DocumentoScadenza = model.DocumentoScadenza;

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
                    // Trova l'AttivitaTipoId dall'AttivitaAnnuale
                    var attivitaAnnuale = await _context.AttivitaAnnuali
                        .FirstOrDefaultAsync(aa => aa.Id == attivitaAnnualeId);
                    
                    // Trova lo stato di default per questa attività tipo
                    int? statoDefaultId = null;
                    if (attivitaAnnuale != null)
                    {
                        var statoDefault = await _context.StatiAttivitaTipo
                            .Where(s => s.AttivitaTipoId == attivitaAnnuale.AttivitaTipoId && s.IsActive)
                            .OrderByDescending(s => s.IsDefault)
                            .ThenBy(s => s.DisplayOrder)
                            .FirstOrDefaultAsync();
                        statoDefaultId = statoDefault?.Id;
                    }

                    var clienteAttivita = new ClienteAttivita
                    {
                        ClienteId = clienteId,
                        AttivitaAnnualeId = attivitaAnnualeId,
                        StatoAttivitaTipoId = statoDefaultId,
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

        // ==================== EXPORT EXCEL ====================

        // GET: Clienti/ExportExcel
        public async Task<IActionResult> ExportExcel(string? searchRagioneSociale, string? searchTipoSoggetto, int? searchAttivitaId)
        {
            var annoCorrente = await _context.AnnualitaFiscali.FirstOrDefaultAsync(a => a.IsCurrent);

            // Query base con gli stessi filtri della pagina Index
            var query = _context.Clienti
                .Include(c => c.Soggetti)
                .Include(c => c.Attivita)
                    .ThenInclude(a => a.AttivitaAnnuale)
                        .ThenInclude(aa => aa!.AttivitaTipo)
                .Where(c => c.IsActive);

            // Applica filtri
            if (!string.IsNullOrWhiteSpace(searchRagioneSociale))
            {
                query = query.Where(c => c.RagioneSociale.Contains(searchRagioneSociale));
            }
            if (!string.IsNullOrWhiteSpace(searchTipoSoggetto))
            {
                query = query.Where(c => c.TipoSoggetto == searchTipoSoggetto);
            }
            if (searchAttivitaId.HasValue)
            {
                query = query.Where(c => c.Attivita.Any(a => a.AttivitaAnnualeId == searchAttivitaId));
            }

            var clienti = await query
                .OrderBy(c => c.RagioneSociale)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Clienti");

            // Intestazioni
            int col = 1;
            // Dati Cliente
            worksheet.Cell(1, col++).Value = "Ragione Sociale";
            worksheet.Cell(1, col++).Value = "Tipo Soggetto";
            worksheet.Cell(1, col++).Value = "Indirizzo";
            worksheet.Cell(1, col++).Value = "CAP";
            worksheet.Cell(1, col++).Value = "Città";
            worksheet.Cell(1, col++).Value = "Provincia";
            worksheet.Cell(1, col++).Value = "Codice Fiscale";
            worksheet.Cell(1, col++).Value = "Partita IVA";
            worksheet.Cell(1, col++).Value = "Codice Ateco";
            worksheet.Cell(1, col++).Value = "Email";
            worksheet.Cell(1, col++).Value = "PEC";
            worksheet.Cell(1, col++).Value = "Telefono";
            // Legale Rappresentante
            worksheet.Cell(1, col++).Value = "LR - Nome Cognome";
            worksheet.Cell(1, col++).Value = "LR - Codice Fiscale";
            worksheet.Cell(1, col++).Value = "LR - Indirizzo";
            worksheet.Cell(1, col++).Value = "LR - CAP";
            worksheet.Cell(1, col++).Value = "LR - Città";
            worksheet.Cell(1, col++).Value = "LR - Provincia";
            worksheet.Cell(1, col++).Value = "LR - Email";
            worksheet.Cell(1, col++).Value = "LR - Telefono";
            // Consiglieri
            worksheet.Cell(1, col++).Value = "Consiglieri";
            // Soci
            worksheet.Cell(1, col++).Value = "Soci";
            // Attività e altro
            worksheet.Cell(1, col++).Value = "Attività Anno Corrente";
            worksheet.Cell(1, col++).Value = "Note";
            worksheet.Cell(1, col++).Value = "Data Creazione";

            var totalCols = col - 1;

            // Stile intestazione
            var headerRange = worksheet.Range(1, 1, 1, totalCols);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Alignment.WrapText = true;

            // Dati
            int row = 2;
            foreach (var cliente in clienti)
            {
                col = 1;
                // Dati Cliente
                worksheet.Cell(row, col++).Value = cliente.RagioneSociale;
                worksheet.Cell(row, col++).Value = cliente.TipoSoggetto ?? "";
                worksheet.Cell(row, col++).Value = cliente.Indirizzo ?? "";
                worksheet.Cell(row, col++).Value = cliente.CAP ?? "";
                worksheet.Cell(row, col++).Value = cliente.Citta ?? "";
                worksheet.Cell(row, col++).Value = cliente.Provincia ?? "";
                worksheet.Cell(row, col++).Value = cliente.CodiceFiscale ?? "";
                worksheet.Cell(row, col++).Value = cliente.PartitaIVA ?? "";
                worksheet.Cell(row, col++).Value = cliente.CodiceAteco ?? "";
                worksheet.Cell(row, col++).Value = cliente.Email ?? "";
                worksheet.Cell(row, col++).Value = cliente.PEC ?? "";
                worksheet.Cell(row, col++).Value = cliente.Telefono ?? "";

                // Legale Rappresentante (primo trovato con tutti i dati)
                var legaleRapp = cliente.Soggetti
                    .Where(s => s.TipoSoggetto == TipoSoggetto.LegaleRappresentante)
                    .FirstOrDefault();
                worksheet.Cell(row, col++).Value = legaleRapp?.NomeCompleto ?? "";
                worksheet.Cell(row, col++).Value = legaleRapp?.CodiceFiscale ?? "";
                worksheet.Cell(row, col++).Value = legaleRapp?.Indirizzo ?? "";
                worksheet.Cell(row, col++).Value = legaleRapp?.CAP ?? "";
                worksheet.Cell(row, col++).Value = legaleRapp?.Citta ?? "";
                worksheet.Cell(row, col++).Value = legaleRapp?.Provincia ?? "";
                worksheet.Cell(row, col++).Value = legaleRapp?.Email ?? "";
                worksheet.Cell(row, col++).Value = legaleRapp?.Telefono ?? "";

                // Consiglieri (tutti, con dettagli)
                var consiglieri = cliente.Soggetti
                    .Where(s => s.TipoSoggetto == TipoSoggetto.Consigliere)
                    .Select(s => {
                        var dettagli = new List<string> { s.NomeCompleto };
                        if (!string.IsNullOrEmpty(s.CodiceFiscale)) dettagli.Add($"CF: {s.CodiceFiscale}");
                        if (!string.IsNullOrEmpty(s.Indirizzo)) dettagli.Add($"{s.Indirizzo}");
                        if (!string.IsNullOrEmpty(s.Citta)) dettagli.Add($"{s.CAP} {s.Citta} ({s.Provincia})");
                        if (!string.IsNullOrEmpty(s.Email)) dettagli.Add($"Email: {s.Email}");
                        if (!string.IsNullOrEmpty(s.Telefono)) dettagli.Add($"Tel: {s.Telefono}");
                        return string.Join(" - ", dettagli);
                    });
                worksheet.Cell(row, col++).Value = string.Join("\n", consiglieri);

                // Soci (tutti, con dettagli e quota)
                var soci = cliente.Soggetti
                    .Where(s => s.TipoSoggetto == TipoSoggetto.Socio)
                    .Select(s => {
                        var dettagli = new List<string> { s.NomeCompleto };
                        if (s.QuotaPercentuale.HasValue) dettagli.Add($"Quota: {s.QuotaPercentuale}%");
                        if (!string.IsNullOrEmpty(s.CodiceFiscale)) dettagli.Add($"CF: {s.CodiceFiscale}");
                        if (!string.IsNullOrEmpty(s.Indirizzo)) dettagli.Add($"{s.Indirizzo}");
                        if (!string.IsNullOrEmpty(s.Citta)) dettagli.Add($"{s.CAP} {s.Citta} ({s.Provincia})");
                        if (!string.IsNullOrEmpty(s.Email)) dettagli.Add($"Email: {s.Email}");
                        if (!string.IsNullOrEmpty(s.Telefono)) dettagli.Add($"Tel: {s.Telefono}");
                        return string.Join(" - ", dettagli);
                    });
                worksheet.Cell(row, col++).Value = string.Join("\n", soci);

                // Attività anno corrente
                var attivita = cliente.Attivita
                    .Where(a => a.AttivitaAnnuale?.AnnualitaFiscaleId == annoCorrente?.Id)
                    .Select(a => a.AttivitaAnnuale?.AttivitaTipo?.Nome)
                    .Where(n => n != null);
                worksheet.Cell(row, col++).Value = string.Join(", ", attivita);

                worksheet.Cell(row, col++).Value = cliente.Note ?? "";
                
                worksheet.Cell(row, col).Value = cliente.CreatedAt;
                worksheet.Cell(row, col++).Style.DateFormat.Format = "dd/MM/yyyy";

                row++;
            }

            // Centra tutte le celle dati e abilita wrap text
            if (row > 2)
            {
                var dataRange = worksheet.Range(2, 1, row - 1, totalCols);
                dataRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                dataRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                dataRange.Style.Alignment.WrapText = true;
            }

            // Auto-fit colonne
            worksheet.Columns().AdjustToContents();

            // Genera file
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"Clienti_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // GET: Clienti/ScadenzeDocumenti
        public async Task<IActionResult> ScadenzeDocumenti(
            string? searchNome, 
            string? tipoFiltro,  // "LegaleRappresentante", "Consigliere", "Socio", "PF", "DI", "PROF"
            string? statoScadenza,
            string? searchCliente,
            int? giorniAvviso)
        {
            // Valori di default
            giorniAvviso ??= 30;
            var oggi = DateTime.Today;
            var dataAvviso = oggi.AddDays(giorniAvviso.Value);
            
            var risultati = new List<ScadenzaDocumentoViewModel>();
            
            // === 1. Carica Soggetti (Legale Rapp., Consigliere, Socio) ===
            var querySoggetti = _context.ClientiSoggetti
                .Include(s => s.Cliente)
                .Where(s => s.Cliente != null && s.Cliente.IsActive)
                .AsQueryable();
            
            var soggetti = await querySoggetti.ToListAsync();
            foreach (var s in soggetti)
            {
                var tipoLabel = s.TipoSoggetto switch
                {
                    TipoSoggetto.LegaleRappresentante => "Legale Rapp.",
                    TipoSoggetto.Consigliere => "Consigliere",
                    TipoSoggetto.Socio => "Socio",
                    _ => "Altro"
                };
                var tipoBadge = s.TipoSoggetto switch
                {
                    TipoSoggetto.LegaleRappresentante => "bg-primary",
                    TipoSoggetto.Consigliere => "bg-info",
                    TipoSoggetto.Socio => "bg-success",
                    _ => "bg-secondary"
                };
                
                risultati.Add(new ScadenzaDocumentoViewModel
                {
                    Id = s.Id,
                    IsCliente = false,
                    ClienteId = s.ClienteId,
                    ClienteNome = s.Cliente?.RagioneSociale ?? "",
                    TipoLabel = tipoLabel,
                    TipoBadgeClass = tipoBadge,
                    Cognome = s.Cognome,
                    Nome = s.Nome,
                    CodiceFiscale = s.CodiceFiscale,
                    DocumentoNumero = s.DocumentoNumero,
                    DocumentoDataRilascio = s.DocumentoDataRilascio,
                    DocumentoRilasciatoDa = s.DocumentoRilasciatoDa,
                    DocumentoScadenza = s.DocumentoScadenza
                });
            }
            
            // === 2. Carica Clienti PF, DI, PROF ===
            var tipiClienteConDocumento = new[] { "PF", "DI", "PROF" };
            var queryClienti = _context.Clienti
                .Where(c => c.IsActive && c.TipoSoggetto != null && tipiClienteConDocumento.Contains(c.TipoSoggetto.ToUpper()))
                .AsQueryable();
            
            var clienti = await queryClienti.ToListAsync();
            foreach (var c in clienti)
            {
                var tipo = c.TipoSoggetto?.ToUpper() ?? "PF";
                var tipoBadge = tipo switch
                {
                    "PF" => "bg-dark",
                    "DI" => "bg-warning text-dark",
                    "PROF" => "bg-purple",
                    _ => "bg-secondary"
                };
                
                // Prova a separare nome/cognome dalla ragione sociale
                var parti = c.RagioneSociale.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                var cognome = parti.Length > 0 ? parti[0] : "";
                var nome = parti.Length > 1 ? parti[1] : "";
                
                risultati.Add(new ScadenzaDocumentoViewModel
                {
                    Id = c.Id,
                    IsCliente = true,
                    ClienteId = c.Id,
                    ClienteNome = c.RagioneSociale,
                    TipoLabel = tipo,
                    TipoBadgeClass = tipoBadge,
                    Cognome = cognome,
                    Nome = nome,
                    CodiceFiscale = c.CodiceFiscale,
                    DocumentoNumero = c.DocumentoNumero,
                    DocumentoDataRilascio = c.DocumentoDataRilascio,
                    DocumentoRilasciatoDa = c.DocumentoRilasciatoDa,
                    DocumentoScadenza = c.DocumentoScadenza
                });
            }
            
            // === Applica Filtri ===
            
            // Filtro per nome/cognome/CF
            if (!string.IsNullOrWhiteSpace(searchNome))
            {
                var searchLower = searchNome.ToLower();
                risultati = risultati.Where(r => 
                    (r.Nome != null && r.Nome.ToLower().Contains(searchLower)) ||
                    (r.Cognome != null && r.Cognome.ToLower().Contains(searchLower)) ||
                    (r.CodiceFiscale != null && r.CodiceFiscale.ToLower().Contains(searchLower)) ||
                    (r.ClienteNome.ToLower().Contains(searchLower))).ToList();
            }
            
            // Filtro per tipo
            if (!string.IsNullOrWhiteSpace(tipoFiltro))
            {
                risultati = risultati.Where(r => 
                    r.TipoLabel.Equals(tipoFiltro, StringComparison.OrdinalIgnoreCase) ||
                    (tipoFiltro == "LegaleRappresentante" && r.TipoLabel == "Legale Rapp.") ||
                    (tipoFiltro == "Consigliere" && r.TipoLabel == "Consigliere") ||
                    (tipoFiltro == "Socio" && r.TipoLabel == "Socio")
                ).ToList();
            }
            
            // Filtro per cliente
            if (!string.IsNullOrWhiteSpace(searchCliente))
            {
                var clienteLower = searchCliente.ToLower();
                risultati = risultati.Where(r => r.ClienteNome.ToLower().Contains(clienteLower)).ToList();
            }
            
            // Filtro per stato scadenza
            if (!string.IsNullOrWhiteSpace(statoScadenza))
            {
                switch (statoScadenza)
                {
                    case "scaduti":
                        risultati = risultati.Where(r => r.DocumentoScadenza.HasValue && r.DocumentoScadenza.Value < oggi).ToList();
                        break;
                    case "inScadenza":
                        risultati = risultati.Where(r => r.DocumentoScadenza.HasValue && 
                                                          r.DocumentoScadenza.Value >= oggi && 
                                                          r.DocumentoScadenza.Value <= dataAvviso).ToList();
                        break;
                    case "validi":
                        risultati = risultati.Where(r => r.DocumentoScadenza.HasValue && r.DocumentoScadenza.Value > dataAvviso).ToList();
                        break;
                    case "senzaScadenza":
                        risultati = risultati.Where(r => !r.DocumentoScadenza.HasValue).ToList();
                        break;
                    case "conDocumento":
                        risultati = risultati.Where(r => !string.IsNullOrEmpty(r.DocumentoNumero)).ToList();
                        break;
                }
            }
            
            // Ordinamento
            risultati = risultati
                .OrderBy(r => r.DocumentoScadenza.HasValue ? 0 : 1)
                .ThenBy(r => r.DocumentoScadenza)
                .ThenBy(r => r.ClienteNome)
                .ThenBy(r => r.Cognome)
                .ToList();
            
            // === Statistiche (su tutti i record, non filtrati) ===
            var tuttiSoggetti = await _context.ClientiSoggetti
                .Include(s => s.Cliente)
                .Where(s => s.Cliente != null && s.Cliente.IsActive)
                .ToListAsync();
            var tuttiClienti = await _context.Clienti
                .Where(c => c.IsActive && c.TipoSoggetto != null && tipiClienteConDocumento.Contains(c.TipoSoggetto.ToUpper()))
                .ToListAsync();
            
            var tuttiRecords = new List<(DateTime? Scadenza, string? NumDoc)>();
            tuttiRecords.AddRange(tuttiSoggetti.Select(s => (s.DocumentoScadenza, s.DocumentoNumero)));
            tuttiRecords.AddRange(tuttiClienti.Select(c => (c.DocumentoScadenza, c.DocumentoNumero)));
            
            ViewBag.TotaleScaduti = tuttiRecords.Count(r => r.Scadenza.HasValue && r.Scadenza.Value < oggi);
            ViewBag.TotaleInScadenza = tuttiRecords.Count(r => r.Scadenza.HasValue && 
                                                                 r.Scadenza.Value >= oggi && 
                                                                 r.Scadenza.Value <= dataAvviso);
            ViewBag.TotaleValidi = tuttiRecords.Count(r => r.Scadenza.HasValue && r.Scadenza.Value > dataAvviso);
            ViewBag.TotaleSenzaScadenza = tuttiRecords.Count(r => !r.Scadenza.HasValue);
            ViewBag.TotaleConDocumento = tuttiRecords.Count(r => !string.IsNullOrEmpty(r.NumDoc));
            ViewBag.TotaleSoggetti = tuttiRecords.Count;

            // Parametri di ricerca per mantenere i filtri
            ViewBag.SearchNome = searchNome;
            ViewBag.TipoFiltro = tipoFiltro;
            ViewBag.StatoScadenza = statoScadenza;
            ViewBag.SearchCliente = searchCliente;
            ViewBag.GiorniAvviso = giorniAvviso;
            ViewBag.Oggi = oggi;
            ViewBag.DataAvviso = dataAvviso;

            return View(risultati);
        }

        // GET: Clienti/ExportScadenzeExcel
        public async Task<IActionResult> ExportScadenzeExcel(
            string? searchNome, 
            string? tipoFiltro, 
            string? statoScadenza,
            string? searchCliente,
            int? giorniAvviso)
        {
            giorniAvviso ??= 30;
            var oggi = DateTime.Today;
            var dataAvviso = oggi.AddDays(giorniAvviso.Value);
            
            var risultati = new List<ScadenzaDocumentoViewModel>();
            
            // === 1. Carica Soggetti ===
            var soggetti = await _context.ClientiSoggetti
                .Include(s => s.Cliente)
                .Where(s => s.Cliente != null && s.Cliente.IsActive)
                .ToListAsync();
            
            foreach (var s in soggetti)
            {
                var tipoLabel = s.TipoSoggetto switch
                {
                    TipoSoggetto.LegaleRappresentante => "Legale Rapp.",
                    TipoSoggetto.Consigliere => "Consigliere",
                    TipoSoggetto.Socio => "Socio",
                    _ => "Altro"
                };
                
                risultati.Add(new ScadenzaDocumentoViewModel
                {
                    Id = s.Id,
                    IsCliente = false,
                    ClienteId = s.ClienteId,
                    ClienteNome = s.Cliente?.RagioneSociale ?? "",
                    TipoLabel = tipoLabel,
                    Cognome = s.Cognome,
                    Nome = s.Nome,
                    CodiceFiscale = s.CodiceFiscale,
                    DocumentoNumero = s.DocumentoNumero,
                    DocumentoDataRilascio = s.DocumentoDataRilascio,
                    DocumentoRilasciatoDa = s.DocumentoRilasciatoDa,
                    DocumentoScadenza = s.DocumentoScadenza
                });
            }
            
            // === 2. Carica Clienti PF, DI, PROF ===
            var tipiClienteConDocumento = new[] { "PF", "DI", "PROF" };
            var clienti = await _context.Clienti
                .Where(c => c.IsActive && c.TipoSoggetto != null && tipiClienteConDocumento.Contains(c.TipoSoggetto.ToUpper()))
                .ToListAsync();
            
            foreach (var c in clienti)
            {
                var tipo = c.TipoSoggetto?.ToUpper() ?? "PF";
                var parti = c.RagioneSociale.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                
                risultati.Add(new ScadenzaDocumentoViewModel
                {
                    Id = c.Id,
                    IsCliente = true,
                    ClienteId = c.Id,
                    ClienteNome = c.RagioneSociale,
                    TipoLabel = tipo,
                    Cognome = parti.Length > 0 ? parti[0] : "",
                    Nome = parti.Length > 1 ? parti[1] : "",
                    CodiceFiscale = c.CodiceFiscale,
                    DocumentoNumero = c.DocumentoNumero,
                    DocumentoDataRilascio = c.DocumentoDataRilascio,
                    DocumentoRilasciatoDa = c.DocumentoRilasciatoDa,
                    DocumentoScadenza = c.DocumentoScadenza
                });
            }
            
            // === Applica Filtri ===
            if (!string.IsNullOrWhiteSpace(searchNome))
            {
                var searchLower = searchNome.ToLower();
                risultati = risultati.Where(r => 
                    (r.Nome != null && r.Nome.ToLower().Contains(searchLower)) ||
                    (r.Cognome != null && r.Cognome.ToLower().Contains(searchLower)) ||
                    (r.CodiceFiscale != null && r.CodiceFiscale.ToLower().Contains(searchLower)) ||
                    (r.ClienteNome.ToLower().Contains(searchLower))).ToList();
            }
            
            if (!string.IsNullOrWhiteSpace(tipoFiltro))
            {
                risultati = risultati.Where(r => 
                    r.TipoLabel.Equals(tipoFiltro, StringComparison.OrdinalIgnoreCase) ||
                    (tipoFiltro == "LegaleRappresentante" && r.TipoLabel == "Legale Rapp.")
                ).ToList();
            }
            
            if (!string.IsNullOrWhiteSpace(searchCliente))
            {
                var clienteLower = searchCliente.ToLower();
                risultati = risultati.Where(r => r.ClienteNome.ToLower().Contains(clienteLower)).ToList();
            }
            
            if (!string.IsNullOrWhiteSpace(statoScadenza))
            {
                switch (statoScadenza)
                {
                    case "scaduti":
                        risultati = risultati.Where(r => r.DocumentoScadenza.HasValue && r.DocumentoScadenza.Value < oggi).ToList();
                        break;
                    case "inScadenza":
                        risultati = risultati.Where(r => r.DocumentoScadenza.HasValue && 
                                                          r.DocumentoScadenza.Value >= oggi && 
                                                          r.DocumentoScadenza.Value <= dataAvviso).ToList();
                        break;
                    case "validi":
                        risultati = risultati.Where(r => r.DocumentoScadenza.HasValue && r.DocumentoScadenza.Value > dataAvviso).ToList();
                        break;
                    case "senzaScadenza":
                        risultati = risultati.Where(r => !r.DocumentoScadenza.HasValue).ToList();
                        break;
                }
            }
            
            // Ordinamento
            risultati = risultati
                .OrderBy(r => r.DocumentoScadenza.HasValue ? 0 : 1)
                .ThenBy(r => r.DocumentoScadenza)
                .ThenBy(r => r.ClienteNome)
                .ToList();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Scadenze Documenti");

            // Header
            var headers = new[] { "Cliente", "Tipo", "Cognome", "Nome", "Codice Fiscale", "N° Documento", "Data Rilascio", "Rilasciato Da", "Scadenza", "Stato" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            // Dati
            int row = 2;
            foreach (var r in risultati)
            {
                var stato = "N/D";
                if (r.DocumentoScadenza.HasValue)
                {
                    if (r.DocumentoScadenza.Value < oggi)
                        stato = "SCADUTO";
                    else if (r.DocumentoScadenza.Value <= dataAvviso)
                        stato = "IN SCADENZA";
                    else
                        stato = "VALIDO";
                }

                worksheet.Cell(row, 1).Value = r.ClienteNome;
                worksheet.Cell(row, 2).Value = r.TipoLabel;
                worksheet.Cell(row, 3).Value = r.Cognome ?? "";
                worksheet.Cell(row, 4).Value = r.Nome ?? "";
                worksheet.Cell(row, 5).Value = r.CodiceFiscale ?? "";
                worksheet.Cell(row, 6).Value = r.DocumentoNumero ?? "";
                worksheet.Cell(row, 7).Value = r.DocumentoDataRilascio?.ToString("dd/MM/yyyy") ?? "";
                worksheet.Cell(row, 8).Value = r.DocumentoRilasciatoDa ?? "";
                worksheet.Cell(row, 9).Value = r.DocumentoScadenza?.ToString("dd/MM/yyyy") ?? "";
                worksheet.Cell(row, 10).Value = stato;

                // Colora in base allo stato
                if (stato == "SCADUTO")
                    worksheet.Cell(row, 10).Style.Fill.BackgroundColor = XLColor.Red;
                else if (stato == "IN SCADENZA")
                    worksheet.Cell(row, 10).Style.Fill.BackgroundColor = XLColor.Orange;
                else if (stato == "VALIDO")
                    worksheet.Cell(row, 10).Style.Fill.BackgroundColor = XLColor.LightGreen;

                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"ScadenzeDocumenti_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        private string GetTipoSoggettoDisplay(TipoSoggetto tipo)
        {
            return tipo switch
            {
                TipoSoggetto.LegaleRappresentante => "Legale Rapp.",
                TipoSoggetto.Consigliere => "Consigliere",
                TipoSoggetto.Socio => "Socio",
                _ => tipo.ToString()
            };
        }

        // ==================== IMPORTAZIONE CLIENTI DA EXCEL ====================

        // GET: Clienti/ImportaClienti
        public IActionResult ImportaClienti()
        {
            return View();
        }

        // POST: Clienti/ImportaClienti - Carica e mostra anteprima
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportaClienti(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Seleziona un file Excel valido.";
                return View();
            }

            if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Il file deve essere in formato .xlsx";
                return View();
            }

            var clientiDaImportare = new List<ClienteImportDto>();
            var errori = new List<string>();

            try
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;

                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();

                if (worksheet == null)
                {
                    TempData["Error"] = "Il file Excel non contiene fogli di lavoro.";
                    return View();
                }

                var dimension = worksheet.Dimension;
                if (dimension == null || dimension.Rows < 2)
                {
                    TempData["Error"] = "Il file Excel non contiene dati (solo intestazione o vuoto).";
                    return View();
                }

                // Leggi intestazioni per mappare le colonne
                var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                for (int col = 1; col <= dimension.Columns; col++)
                {
                    var headerValue = worksheet.Cells[1, col].Text?.Trim();
                    if (!string.IsNullOrEmpty(headerValue))
                    {
                        headers[headerValue] = col;
                    }
                }

                // Verifica colonne obbligatorie
                if (!headers.ContainsKey("RagioneSociale") && !headers.ContainsKey("Ragione Sociale"))
                {
                    TempData["Error"] = "Il file deve contenere la colonna 'RagioneSociale' o 'Ragione Sociale'.";
                    return View();
                }

                // Leggi i dati
                for (int row = 2; row <= dimension.Rows; row++)
                {
                    var ragioneSociale = GetCellValue(worksheet, row, headers, "RagioneSociale", "Ragione Sociale");
                    
                    if (string.IsNullOrWhiteSpace(ragioneSociale))
                    {
                        if (row == 2) continue; // Prima riga vuota, salta
                        errori.Add($"Riga {row}: Ragione Sociale mancante, riga ignorata.");
                        continue;
                    }

                    var cliente = new ClienteImportDto
                    {
                        Riga = row,
                        RagioneSociale = ragioneSociale.Trim(),
                        TipoSoggetto = GetCellValue(worksheet, row, headers, "TipoSoggetto", "Tipo Soggetto", "Tipo")?.Trim().ToUpper(),
                        PartitaIVA = GetCellValue(worksheet, row, headers, "PartitaIVA", "Partita IVA", "P.IVA", "PIVA")?.Trim(),
                        CodiceFiscale = GetCellValue(worksheet, row, headers, "CodiceFiscale", "Codice Fiscale", "CF")?.Trim().ToUpper(),
                        CodiceAteco = GetCellValue(worksheet, row, headers, "CodiceAteco", "Codice Ateco", "Ateco")?.Trim(),
                        Indirizzo = GetCellValue(worksheet, row, headers, "Indirizzo", "Via")?.Trim(),
                        Citta = GetCellValue(worksheet, row, headers, "Citta", "Città", "Comune")?.Trim(),
                        Provincia = GetCellValue(worksheet, row, headers, "Provincia", "Prov")?.Trim().ToUpper(),
                        CAP = GetCellValue(worksheet, row, headers, "CAP", "C.A.P.")?.Trim(),
                        Email = GetCellValue(worksheet, row, headers, "Email", "E-mail")?.Trim(),
                        PEC = GetCellValue(worksheet, row, headers, "PEC")?.Trim(),
                        Telefono = GetCellValue(worksheet, row, headers, "Telefono", "Tel")?.Trim(),
                        Note = GetCellValue(worksheet, row, headers, "Note", "Nota")?.Trim()
                    };

                    // Normalizza Tipo Soggetto
                    cliente.TipoSoggetto = NormalizeTipoSoggetto(cliente.TipoSoggetto);

                    // Verifica se esiste già
                    var esistente = await _context.Clienti.FirstOrDefaultAsync(c => 
                        c.RagioneSociale.ToLower() == cliente.RagioneSociale.ToLower() ||
                        (!string.IsNullOrEmpty(cliente.PartitaIVA) && c.PartitaIVA == cliente.PartitaIVA) ||
                        (!string.IsNullOrEmpty(cliente.CodiceFiscale) && c.CodiceFiscale == cliente.CodiceFiscale));

                    if (esistente != null)
                    {
                        cliente.EsisteGia = true;
                        cliente.ClienteEsistenteId = esistente.Id;
                        cliente.ClienteEsistenteNome = esistente.RagioneSociale;
                    }

                    clientiDaImportare.Add(cliente);
                }

                if (!clientiDaImportare.Any())
                {
                    TempData["Error"] = "Nessun cliente valido trovato nel file.";
                    return View();
                }

                // Salva i dati in Session (lato server) per evitare errore 431
                HttpContext.Session.SetString("ClientiDaImportare", System.Text.Json.JsonSerializer.Serialize(clientiDaImportare));
                TempData["ErroriImportazione"] = errori;
                TempData["NomeFile"] = file.FileName;

                return View("AnteprimaImportazione", clientiDaImportare);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore nella lettura del file: {ex.Message}";
                return View();
            }
        }

        // POST: Clienti/EseguiImportazione
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EseguiImportazione(List<int> righeSelezionate, IFormCollection form)
        {
            var json = HttpContext.Session.GetString("ClientiDaImportare");
            if (string.IsNullOrEmpty(json))
            {
                TempData["Error"] = "Sessione scaduta. Ricaricare il file.";
                return RedirectToAction(nameof(ImportaClienti));
            }

            var clientiDaImportare = System.Text.Json.JsonSerializer.Deserialize<List<ClienteImportDto>>(json);
            if (clientiDaImportare == null || !clientiDaImportare.Any())
            {
                TempData["Error"] = "Nessun cliente da importare.";
                return RedirectToAction(nameof(ImportaClienti));
            }

            int importati = 0;
            int aggiornati = 0;
            int saltati = 0;

            foreach (var dto in clientiDaImportare)
            {
                // Se non è selezionato, salta
                if (righeSelezionate == null || !righeSelezionate.Contains(dto.Riga))
                {
                    saltati++;
                    continue;
                }

                if (dto.EsisteGia)
                {
                    // Leggi l'azione specifica per questo duplicato dal form
                    var azioneDuplicato = form[$"azioneDuplicato_{dto.Riga}"].ToString();
                    
                    if (azioneDuplicato == "sovrascrivi" && dto.ClienteEsistenteId.HasValue)
                    {
                        // Aggiorna il cliente esistente
                        var esistente = await _context.Clienti.FindAsync(dto.ClienteEsistenteId.Value);
                        if (esistente != null)
                        {
                            AggiornaClienteDaDto(esistente, dto);
                            aggiornati++;
                        }
                    }
                    else
                    {
                        // Azione = "salta" o altro
                        saltati++;
                    }
                }
                else
                {
                    // Crea nuovo cliente
                    var nuovoCliente = new Cliente
                    {
                        RagioneSociale = dto.RagioneSociale,
                        TipoSoggetto = dto.TipoSoggetto,
                        PartitaIVA = dto.PartitaIVA,
                        CodiceFiscale = dto.CodiceFiscale,
                        CodiceAteco = dto.CodiceAteco,
                        Indirizzo = dto.Indirizzo,
                        Citta = dto.Citta,
                        Provincia = dto.Provincia,
                        CAP = dto.CAP,
                        Email = dto.Email,
                        PEC = dto.PEC,
                        Telefono = dto.Telefono,
                        Note = dto.Note,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };
                    _context.Clienti.Add(nuovoCliente);
                    importati++;
                }
            }

            await _context.SaveChangesAsync();

            // Pulisci la Session
            HttpContext.Session.Remove("ClientiDaImportare");

            var messaggio = $"Importazione completata: {importati} nuovi clienti";
            if (aggiornati > 0) messaggio += $", {aggiornati} aggiornati";
            if (saltati > 0) messaggio += $", {saltati} saltati";
            TempData["Success"] = messaggio;

            return RedirectToAction(nameof(Index));
        }

        // GET: Clienti/DownloadTemplateClienti
        public IActionResult DownloadTemplateClienti()
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Clienti");

            // Intestazioni
            var headers = new[] { "RagioneSociale", "TipoSoggetto", "PartitaIVA", "CodiceFiscale", "CodiceAteco", 
                                  "Indirizzo", "Citta", "Provincia", "CAP", "Email", "PEC", "Telefono", "Note" };
            
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                worksheet.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(68, 114, 196));
                worksheet.Cells[1, i + 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
            }

            // Righe di esempio
            var esempi = new List<object[]>
            {
                new object[] { "MARIO ROSSI SRL", "SC", "12345678901", "12345678901", "631021", "Via Roma 1", "Milano", "MI", "20100", "info@mariorossi.it", "mariorossi@pec.it", "0212345678", "Cliente esempio" },
                new object[] { "BIANCHI GIUSEPPE", "PF", "", "BNCGPP80A01F205X", "692100", "Via Verdi 25", "Roma", "RM", "00100", "g.bianchi@email.it", "", "0612345678", "" },
                new object[] { "VERDI & ASSOCIATI SNC", "SP", "98765432109", "98765432109", "462500", "Piazza Duomo 10", "Firenze", "FI", "50100", "info@verdi.it", "verdi@pec.it", "055123456", "Società di persone" },
                new object[] { "DOTT. NERI ANTONIO", "PROF", "", "NRNNTN75B15L219K", "692000", "Corso Italia 50", "Torino", "TO", "10100", "a.neri@studio.it", "neri@pec.it", "011987654", "Professionista" }
            };

            for (int row = 0; row < esempi.Count; row++)
            {
                for (int col = 0; col < esempi[row].Length; col++)
                {
                    worksheet.Cells[row + 2, col + 1].Value = esempi[row][col];
                }
            }

            // Foglio istruzioni
            var istruzioni = package.Workbook.Worksheets.Add("Istruzioni");
            istruzioni.Cells[1, 1].Value = "ISTRUZIONI PER L'IMPORTAZIONE CLIENTI";
            istruzioni.Cells[1, 1].Style.Font.Bold = true;
            istruzioni.Cells[1, 1].Style.Font.Size = 14;

            var rigaIstr = 3;
            istruzioni.Cells[rigaIstr++, 1].Value = "COLONNE OBBLIGATORIE:";
            istruzioni.Cells[rigaIstr++, 1].Value = "• RagioneSociale - Nome del cliente (obbligatorio)";
            rigaIstr++;
            istruzioni.Cells[rigaIstr++, 1].Value = "COLONNE OPZIONALI:";
            istruzioni.Cells[rigaIstr++, 1].Value = "• TipoSoggetto - Tipo cliente: SC (Società Capitali), PF (Persona Fisica), SP (Società Persone), PROF (Professionista), ALTRO";
            istruzioni.Cells[rigaIstr++, 1].Value = "• PartitaIVA - Partita IVA (11 caratteri)";
            istruzioni.Cells[rigaIstr++, 1].Value = "• CodiceFiscale - Codice Fiscale (16 caratteri)";
            istruzioni.Cells[rigaIstr++, 1].Value = "• CodiceAteco - Codice ATECO attività";
            istruzioni.Cells[rigaIstr++, 1].Value = "• Indirizzo - Via e numero civico";
            istruzioni.Cells[rigaIstr++, 1].Value = "• Citta - Comune";
            istruzioni.Cells[rigaIstr++, 1].Value = "• Provincia - Sigla provincia (2 lettere)";
            istruzioni.Cells[rigaIstr++, 1].Value = "• CAP - Codice Avviamento Postale (5 cifre)";
            istruzioni.Cells[rigaIstr++, 1].Value = "• Email - Indirizzo email";
            istruzioni.Cells[rigaIstr++, 1].Value = "• PEC - Posta Elettronica Certificata";
            istruzioni.Cells[rigaIstr++, 1].Value = "• Telefono - Numero di telefono";
            istruzioni.Cells[rigaIstr++, 1].Value = "• Note - Note aggiuntive";
            rigaIstr++;
            istruzioni.Cells[rigaIstr++, 1].Value = "NOTE:";
            istruzioni.Cells[rigaIstr++, 1].Value = "• Le intestazioni delle colonne possono essere scritte anche con spazi (es. 'Ragione Sociale' invece di 'RagioneSociale')";
            istruzioni.Cells[rigaIstr++, 1].Value = "• I clienti con stessa Ragione Sociale, Partita IVA o Codice Fiscale verranno segnalati come duplicati";
            istruzioni.Cells[rigaIstr++, 1].Value = "• In fase di importazione potrai scegliere se sovrascrivere i duplicati o saltarli";

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            istruzioni.Column(1).Width = 120;

            var content = package.GetAsByteArray();
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Template_Importazione_Clienti.xlsx");
        }

        // Helper methods per importazione
        private string? GetCellValue(ExcelWorksheet worksheet, int row, Dictionary<string, int> headers, params string[] possibleHeaders)
        {
            foreach (var header in possibleHeaders)
            {
                if (headers.TryGetValue(header, out int col))
                {
                    return worksheet.Cells[row, col].Text;
                }
            }
            return null;
        }

        private string? NormalizeTipoSoggetto(string? tipo)
        {
            if (string.IsNullOrWhiteSpace(tipo)) return null;
            
            return tipo.ToUpper() switch
            {
                "SC" or "SRL" or "SPA" or "SAPA" or "SOCIETÀ CAPITALI" or "SOCIETA CAPITALI" => "SC",
                "SP" or "SNC" or "SAS" or "SOCIETÀ PERSONE" or "SOCIETA PERSONE" => "SP",
                "PF" or "PERSONA FISICA" => "PF",
                "PROF" or "PROFESSIONISTA" => "PROF",
                _ => "ALTRO"
            };
        }

        private async Task SalvaCampiCustom(int clienteId, IFormCollection form)
        {
            var campiCustom = await _context.CampiCustomClienti
                .Where(c => c.IsActive)
                .ToListAsync();

            foreach (var campo in campiCustom)
            {
                var formKey = $"custom_{campo.Id}";
                var valore = form[formKey].ToString();

                // Gestione checkbox
                if (campo.TipoCampo == "checkbox")
                {
                    valore = form.Keys.Contains(formKey) ? "true" : "false";
                }

                // Cerca valore esistente
                var valoreEsistente = await _context.ValoriCampiCustomClienti
                    .FirstOrDefaultAsync(v => v.ClienteId == clienteId && v.CampoCustomClienteId == campo.Id);

                if (valoreEsistente != null)
                {
                    // Aggiorna
                    valoreEsistente.Valore = valore;
                    valoreEsistente.UpdatedAt = DateTime.Now;
                }
                else if (!string.IsNullOrEmpty(valore))
                {
                    // Crea nuovo
                    _context.ValoriCampiCustomClienti.Add(new ValoreCampoCustomCliente
                    {
                        ClienteId = clienteId,
                        CampoCustomClienteId = campo.Id,
                        Valore = valore,
                        UpdatedAt = DateTime.Now
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        private void AggiornaClienteDaDto(Cliente cliente, ClienteImportDto dto)
        {
            if (!string.IsNullOrEmpty(dto.TipoSoggetto)) cliente.TipoSoggetto = dto.TipoSoggetto;
            if (!string.IsNullOrEmpty(dto.PartitaIVA)) cliente.PartitaIVA = dto.PartitaIVA;
            if (!string.IsNullOrEmpty(dto.CodiceFiscale)) cliente.CodiceFiscale = dto.CodiceFiscale;
            if (!string.IsNullOrEmpty(dto.CodiceAteco)) cliente.CodiceAteco = dto.CodiceAteco;
            if (!string.IsNullOrEmpty(dto.Indirizzo)) cliente.Indirizzo = dto.Indirizzo;
            if (!string.IsNullOrEmpty(dto.Citta)) cliente.Citta = dto.Citta;
            if (!string.IsNullOrEmpty(dto.Provincia)) cliente.Provincia = dto.Provincia;
            if (!string.IsNullOrEmpty(dto.CAP)) cliente.CAP = dto.CAP;
            if (!string.IsNullOrEmpty(dto.Email)) cliente.Email = dto.Email;
            if (!string.IsNullOrEmpty(dto.PEC)) cliente.PEC = dto.PEC;
            if (!string.IsNullOrEmpty(dto.Telefono)) cliente.Telefono = dto.Telefono;
            if (!string.IsNullOrEmpty(dto.Note)) cliente.Note = dto.Note;
        }

        // ============ GESTIONE CAMPI CUSTOM CLIENTE ============

        // GET: Clienti/CampiCustom
        public async Task<IActionResult> CampiCustom()
        {
            var campi = await _context.CampiCustomClienti
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Nome)
                .ToListAsync();

            return View(campi);
        }

        // POST: Clienti/AddCampoCustom
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCampoCustom(CampoCustomCliente campo)
        {
            if (string.IsNullOrWhiteSpace(campo.Nome))
            {
                TempData["Error"] = "Il nome del campo è obbligatorio.";
                return RedirectToAction(nameof(CampiCustom));
            }

            // Verifica duplicati
            var esistente = await _context.CampiCustomClienti
                .AnyAsync(c => c.Nome == campo.Nome);

            if (esistente)
            {
                TempData["Error"] = $"Esiste già un campo con nome '{campo.Nome}'.";
                return RedirectToAction(nameof(CampiCustom));
            }

            // Imposta ordine
            var maxOrder = await _context.CampiCustomClienti.MaxAsync(c => (int?)c.DisplayOrder) ?? 0;
            campo.DisplayOrder = maxOrder + 1;
            campo.CreatedAt = DateTime.Now;
            campo.IsActive = true;

            _context.CampiCustomClienti.Add(campo);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Campo '{campo.Label}' creato con successo.";
            return RedirectToAction(nameof(CampiCustom));
        }

        // POST: Clienti/EditCampoCustom
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCampoCustom(int id, string label, string tipoCampo, bool isRequired, 
            bool showInList, bool useAsFilter, string? options, string? defaultValue, string? placeholder)
        {
            var campo = await _context.CampiCustomClienti.FindAsync(id);
            if (campo == null)
            {
                TempData["Error"] = "Campo non trovato.";
                return RedirectToAction(nameof(CampiCustom));
            }

            campo.Label = label;
            campo.TipoCampo = tipoCampo;
            campo.IsRequired = isRequired;
            campo.ShowInList = showInList;
            campo.UseAsFilter = useAsFilter;
            campo.Options = options;
            campo.DefaultValue = defaultValue;
            campo.Placeholder = placeholder;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Campo '{campo.Label}' aggiornato con successo.";
            return RedirectToAction(nameof(CampiCustom));
        }

        // POST: Clienti/DeleteCampoCustom
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCampoCustom(int id)
        {
            var campo = await _context.CampiCustomClienti
                .Include(c => c.Valori)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (campo == null)
            {
                TempData["Error"] = "Campo non trovato.";
                return RedirectToAction(nameof(CampiCustom));
            }

            // Elimina anche tutti i valori associati
            _context.ValoriCampiCustomClienti.RemoveRange(campo.Valori);
            _context.CampiCustomClienti.Remove(campo);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Campo '{campo.Label}' eliminato con successo.";
            return RedirectToAction(nameof(CampiCustom));
        }

        // POST: Clienti/MoveCampoCustomUp
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveCampoCustomUp(int id)
        {
            var campo = await _context.CampiCustomClienti.FindAsync(id);
            if (campo == null) return RedirectToAction(nameof(CampiCustom));

            var campoPrecedente = await _context.CampiCustomClienti
                .Where(c => c.DisplayOrder < campo.DisplayOrder)
                .OrderByDescending(c => c.DisplayOrder)
                .FirstOrDefaultAsync();

            if (campoPrecedente != null)
            {
                var tempOrder = campo.DisplayOrder;
                campo.DisplayOrder = campoPrecedente.DisplayOrder;
                campoPrecedente.DisplayOrder = tempOrder;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(CampiCustom));
        }

        // POST: Clienti/MoveCampoCustomDown
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveCampoCustomDown(int id)
        {
            var campo = await _context.CampiCustomClienti.FindAsync(id);
            if (campo == null) return RedirectToAction(nameof(CampiCustom));

            var campoSuccessivo = await _context.CampiCustomClienti
                .Where(c => c.DisplayOrder > campo.DisplayOrder)
                .OrderBy(c => c.DisplayOrder)
                .FirstOrDefaultAsync();

            if (campoSuccessivo != null)
            {
                var tempOrder = campo.DisplayOrder;
                campo.DisplayOrder = campoSuccessivo.DisplayOrder;
                campoSuccessivo.DisplayOrder = tempOrder;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(CampiCustom));
        }

        // ===== API per ricerca e copia dati =====

        /// <summary>
        /// Cerca clienti PF/DI per copiare dati quando si crea un soggetto
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchClientiPerCopia(string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return Json(new List<object>());

            var clienti = await _context.Clienti
                .Where(c => c.IsActive && c.RagioneSociale.Contains(q))
                .OrderBy(c => c.RagioneSociale)
                .Take(15)
                .Select(c => new
                {
                    id = c.Id,
                    ragioneSociale = c.RagioneSociale,
                    tipoSoggetto = c.TipoSoggetto ?? "",
                    codiceFiscale = c.CodiceFiscale ?? "",
                    indirizzo = c.Indirizzo ?? "",
                    citta = c.Citta ?? "",
                    provincia = c.Provincia ?? "",
                    cap = c.CAP ?? "",
                    email = c.Email ?? "",
                    telefono = c.Telefono ?? "",
                    documentoNumero = c.DocumentoNumero ?? "",
                    documentoRilasciatoDa = c.DocumentoRilasciatoDa ?? "",
                    documentoDataRilascio = c.DocumentoDataRilascio,
                    documentoScadenza = c.DocumentoScadenza
                })
                .ToListAsync();

            return Json(clienti);
        }

        /// <summary>
        /// Cerca soggetti (di tutti i clienti) per copiare dati quando si crea un nuovo cliente
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchSoggettiPerCopia(string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return Json(new List<object>());

            var soggetti = await _context.ClientiSoggetti
                .Include(s => s.Cliente)
                .Where(s => (s.Nome + " " + s.Cognome).Contains(q) || 
                           s.Cognome.Contains(q) || 
                           s.Nome.Contains(q) ||
                           (s.CodiceFiscale != null && s.CodiceFiscale.Contains(q)))
                .OrderBy(s => s.Cognome)
                .ThenBy(s => s.Nome)
                .Take(15)
                .Select(s => new
                {
                    id = s.Id,
                    nome = s.Nome,
                    cognome = s.Cognome,
                    nomeCompleto = s.Cognome + " " + s.Nome,
                    tipoSoggetto = s.TipoSoggetto.ToString(),
                    clienteNome = s.Cliente != null ? s.Cliente.RagioneSociale : "",
                    codiceFiscale = s.CodiceFiscale ?? "",
                    indirizzo = s.Indirizzo ?? "",
                    citta = s.Citta ?? "",
                    provincia = s.Provincia ?? "",
                    cap = s.CAP ?? "",
                    email = s.Email ?? "",
                    telefono = s.Telefono ?? "",
                    documentoNumero = s.DocumentoNumero ?? "",
                    documentoRilasciatoDa = s.DocumentoRilasciatoDa ?? "",
                    documentoDataRilascio = s.DocumentoDataRilascio,
                    documentoScadenza = s.DocumentoScadenza
                })
                .ToListAsync();

            return Json(soggetti);
        }
    }

    // DTO per l'importazione
    public class ClienteImportDto
    {
        public int Riga { get; set; }
        public string RagioneSociale { get; set; } = string.Empty;
        public string? TipoSoggetto { get; set; }
        public string? PartitaIVA { get; set; }
        public string? CodiceFiscale { get; set; }
        public string? CodiceAteco { get; set; }
        public string? Indirizzo { get; set; }
        public string? Citta { get; set; }
        public string? Provincia { get; set; }
        public string? CAP { get; set; }
        public string? Email { get; set; }
        public string? PEC { get; set; }
        public string? Telefono { get; set; }
        public string? Note { get; set; }
        public bool EsisteGia { get; set; }
        public int? ClienteEsistenteId { get; set; }
        public string? ClienteEsistenteNome { get; set; }
    }
}


