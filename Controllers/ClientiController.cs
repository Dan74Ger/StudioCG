using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
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
            TipoSoggetto? tipoSoggetto, 
            string? statoScadenza,
            string? searchCliente,
            int? giorniAvviso)
        {
            // Valori di default
            giorniAvviso ??= 30;
            
            // Query base: tutti i soggetti con documento
            var query = _context.ClientiSoggetti
                .Include(s => s.Cliente)
                .Where(s => s.Cliente != null && s.Cliente.IsActive)
                .AsQueryable();

            // Filtro per nome/cognome
            if (!string.IsNullOrWhiteSpace(searchNome))
            {
                var searchLower = searchNome.ToLower();
                query = query.Where(s => 
                    (s.Nome != null && s.Nome.ToLower().Contains(searchLower)) ||
                    (s.Cognome != null && s.Cognome.ToLower().Contains(searchLower)) ||
                    (s.CodiceFiscale != null && s.CodiceFiscale.ToLower().Contains(searchLower)));
            }

            // Filtro per tipo soggetto
            if (tipoSoggetto.HasValue)
            {
                query = query.Where(s => s.TipoSoggetto == tipoSoggetto.Value);
            }

            // Filtro per cliente
            if (!string.IsNullOrWhiteSpace(searchCliente))
            {
                var clienteLower = searchCliente.ToLower();
                query = query.Where(s => s.Cliente != null && s.Cliente.RagioneSociale.ToLower().Contains(clienteLower));
            }

            // Filtro per stato scadenza
            var oggi = DateTime.Today;
            var dataAvviso = oggi.AddDays(giorniAvviso.Value);
            
            if (!string.IsNullOrWhiteSpace(statoScadenza))
            {
                switch (statoScadenza)
                {
                    case "scaduti":
                        query = query.Where(s => s.DocumentoScadenza.HasValue && s.DocumentoScadenza.Value < oggi);
                        break;
                    case "inScadenza":
                        query = query.Where(s => s.DocumentoScadenza.HasValue && 
                                                  s.DocumentoScadenza.Value >= oggi && 
                                                  s.DocumentoScadenza.Value <= dataAvviso);
                        break;
                    case "validi":
                        query = query.Where(s => s.DocumentoScadenza.HasValue && s.DocumentoScadenza.Value > dataAvviso);
                        break;
                    case "senzaScadenza":
                        query = query.Where(s => !s.DocumentoScadenza.HasValue);
                        break;
                    case "conDocumento":
                        query = query.Where(s => !string.IsNullOrEmpty(s.DocumentoNumero));
                        break;
                }
            }

            // Ordinamento: prima scaduti, poi in scadenza, poi gli altri
            var soggetti = await query
                .OrderBy(s => s.DocumentoScadenza.HasValue ? 0 : 1)
                .ThenBy(s => s.DocumentoScadenza)
                .ThenBy(s => s.Cliente!.RagioneSociale)
                .ThenBy(s => s.Cognome)
                .ToListAsync();

            // Statistiche
            var tuttiSoggetti = await _context.ClientiSoggetti
                .Include(s => s.Cliente)
                .Where(s => s.Cliente != null && s.Cliente.IsActive)
                .ToListAsync();

            ViewBag.TotaleScaduti = tuttiSoggetti.Count(s => s.DocumentoScadenza.HasValue && s.DocumentoScadenza.Value < oggi);
            ViewBag.TotaleInScadenza = tuttiSoggetti.Count(s => s.DocumentoScadenza.HasValue && 
                                                                 s.DocumentoScadenza.Value >= oggi && 
                                                                 s.DocumentoScadenza.Value <= dataAvviso);
            ViewBag.TotaleValidi = tuttiSoggetti.Count(s => s.DocumentoScadenza.HasValue && s.DocumentoScadenza.Value > dataAvviso);
            ViewBag.TotaleSenzaScadenza = tuttiSoggetti.Count(s => !s.DocumentoScadenza.HasValue);
            ViewBag.TotaleConDocumento = tuttiSoggetti.Count(s => !string.IsNullOrEmpty(s.DocumentoNumero));
            ViewBag.TotaleSoggetti = tuttiSoggetti.Count;

            // Parametri di ricerca per mantenere i filtri
            ViewBag.SearchNome = searchNome;
            ViewBag.TipoSoggetto = tipoSoggetto;
            ViewBag.StatoScadenza = statoScadenza;
            ViewBag.SearchCliente = searchCliente;
            ViewBag.GiorniAvviso = giorniAvviso;
            ViewBag.Oggi = oggi;
            ViewBag.DataAvviso = dataAvviso;

            return View(soggetti);
        }

        // GET: Clienti/ExportScadenzeExcel
        public async Task<IActionResult> ExportScadenzeExcel(
            string? searchNome, 
            TipoSoggetto? tipoSoggetto, 
            string? statoScadenza,
            string? searchCliente,
            int? giorniAvviso)
        {
            giorniAvviso ??= 30;
            
            var query = _context.ClientiSoggetti
                .Include(s => s.Cliente)
                .Where(s => s.Cliente != null && s.Cliente.IsActive)
                .AsQueryable();

            // Applica gli stessi filtri della vista
            if (!string.IsNullOrWhiteSpace(searchNome))
            {
                var searchLower = searchNome.ToLower();
                query = query.Where(s => 
                    (s.Nome != null && s.Nome.ToLower().Contains(searchLower)) ||
                    (s.Cognome != null && s.Cognome.ToLower().Contains(searchLower)));
            }

            if (tipoSoggetto.HasValue)
            {
                query = query.Where(s => s.TipoSoggetto == tipoSoggetto.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchCliente))
            {
                var clienteLower = searchCliente.ToLower();
                query = query.Where(s => s.Cliente != null && s.Cliente.RagioneSociale.ToLower().Contains(clienteLower));
            }

            var oggi = DateTime.Today;
            var dataAvviso = oggi.AddDays(giorniAvviso.Value);
            
            if (!string.IsNullOrWhiteSpace(statoScadenza))
            {
                switch (statoScadenza)
                {
                    case "scaduti":
                        query = query.Where(s => s.DocumentoScadenza.HasValue && s.DocumentoScadenza.Value < oggi);
                        break;
                    case "inScadenza":
                        query = query.Where(s => s.DocumentoScadenza.HasValue && 
                                                  s.DocumentoScadenza.Value >= oggi && 
                                                  s.DocumentoScadenza.Value <= dataAvviso);
                        break;
                    case "validi":
                        query = query.Where(s => s.DocumentoScadenza.HasValue && s.DocumentoScadenza.Value > dataAvviso);
                        break;
                    case "senzaScadenza":
                        query = query.Where(s => !s.DocumentoScadenza.HasValue);
                        break;
                }
            }

            var soggetti = await query
                .OrderBy(s => s.DocumentoScadenza.HasValue ? 0 : 1)
                .ThenBy(s => s.DocumentoScadenza)
                .ThenBy(s => s.Cliente!.RagioneSociale)
                .ToListAsync();

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
            foreach (var s in soggetti)
            {
                var stato = "N/D";
                if (s.DocumentoScadenza.HasValue)
                {
                    if (s.DocumentoScadenza.Value < oggi)
                        stato = "SCADUTO";
                    else if (s.DocumentoScadenza.Value <= dataAvviso)
                        stato = "IN SCADENZA";
                    else
                        stato = "VALIDO";
                }

                worksheet.Cell(row, 1).Value = s.Cliente?.RagioneSociale ?? "";
                worksheet.Cell(row, 2).Value = GetTipoSoggettoDisplay(s.TipoSoggetto);
                worksheet.Cell(row, 3).Value = s.Cognome ?? "";
                worksheet.Cell(row, 4).Value = s.Nome ?? "";
                worksheet.Cell(row, 5).Value = s.CodiceFiscale ?? "";
                worksheet.Cell(row, 6).Value = s.DocumentoNumero ?? "";
                worksheet.Cell(row, 7).Value = s.DocumentoDataRilascio?.ToString("dd/MM/yyyy") ?? "";
                worksheet.Cell(row, 8).Value = s.DocumentoRilasciatoDa ?? "";
                worksheet.Cell(row, 9).Value = s.DocumentoScadenza?.ToString("dd/MM/yyyy") ?? "";
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

                var rowCount = worksheet.Dimension?.Rows ?? 0;
                if (rowCount < 2)
                {
                    TempData["Error"] = "Il file Excel non contiene dati (solo intestazione o vuoto).";
                    return View();
                }

                // Leggi intestazioni per mappare le colonne
                var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                for (int col = 1; col <= worksheet.Dimension.Columns; col++)
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
                for (int row = 2; row <= rowCount; row++)
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

