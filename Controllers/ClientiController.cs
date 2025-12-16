using ClosedXML.Excel;
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
                cliente.TipoSoggetto = model.TipoSoggetto;
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
    }
}

