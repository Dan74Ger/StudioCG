using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioCG.Web.Data;
using StudioCG.Web.Models.BudgetStudio;
using StudioCG.Web.Models.ViewModels;
using System.Globalization;

namespace StudioCG.Web.Controllers
{
    [Authorize]
    public class BudgetStudioController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BudgetStudioController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /BudgetStudio
        [HttpGet]
        public async Task<IActionResult> Index(int? anno, int? meseDa, int? meseA, BudgetPagateFiltro? pagate, bool? nascondiVuote, string? tab)
        {
            var annoSelezionato = anno ?? DateTime.Today.Year;
            var da = Math.Clamp(meseDa ?? 1, 1, 12);
            var a = Math.Clamp(meseA ?? 12, 1, 12);
            if (da > a) (da, a) = (a, da);

            var filtroPagate = pagate ?? BudgetPagateFiltro.NonPagate;
            var filtroNascondiVuote = nascondiVuote ?? true; // Default: nascondi voci vuote

            var voci = await _context.VociSpesaBudget
                .Where(v => v.IsActive)
                .OrderBy(v => v.CodiceSpesa)
                .ToListAsync();

            var queryMensile = _context.BudgetSpeseMensili
                .Include(r => r.VoceSpesaBudget)
                .Where(r => r.Anno == annoSelezionato && r.Mese >= da && r.Mese <= a);

            if (filtroPagate == BudgetPagateFiltro.NonPagate)
                queryMensile = queryMensile.Where(r => !r.Pagata);
            else if (filtroPagate == BudgetPagateFiltro.Pagate)
                queryMensile = queryMensile.Where(r => r.Pagata);

            var righe = await queryMensile.ToListAsync();

            // Se nascondiVuote è attivo, filtra le voci che hanno almeno un importo > 0 nel range
            var vociFiltrate = voci;
            if (filtroNascondiVuote)
            {
                var vociConImporti = righe
                    .Where(r => r.Importo != 0)
                    .Select(r => r.VoceSpesaBudgetId)
                    .Distinct()
                    .ToHashSet();

                vociFiltrate = voci.Where(v => vociConImporti.Contains(v.Id)).ToList();
            }

            var vm = new BudgetStudioViewModel
            {
                Anno = annoSelezionato,
                MeseDa = da,
                MeseA = a,
                PagateFiltro = filtroPagate,
                NascondiVuote = filtroNascondiVuote,
                Voci = vociFiltrate,
                RigheMensili = righe
            };

            vm.Map = righe.ToDictionary(x => (x.VoceSpesaBudgetId, x.Mese), x => x);

            for (var m = da; m <= a; m++)
            {
                var tot = righe.Where(r => r.Mese == m).Sum(r => r.Importo);
                vm.TotaliMese[m] = tot;
                vm.TotaleAnno += tot;
            }

            ViewBag.ActiveTab = string.Equals(tab, "prospetto", StringComparison.OrdinalIgnoreCase) ? "prospetto" : "anagrafica";
            ViewData["Title"] = "Budget Studio";
            return View(vm);
        }

        // POST: /BudgetStudio/CreateVoceSpesaBudget
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateVoceSpesaBudget(string codiceSpesa, string descrizione, MetodoPagamentoBudget metodoPagamentoDefault, string? noteDefault, int anno)
        {
            try
            {
                var voce = new VoceSpesaBudget
                {
                    CodiceSpesa = codiceSpesa.Trim(),
                    Descrizione = descrizione.Trim(),
                    MetodoPagamentoDefault = metodoPagamentoDefault,
                    NoteDefault = string.IsNullOrWhiteSpace(noteDefault) ? null : noteDefault.Trim(),
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.VociSpesaBudget.Add(voce);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Voce spesa creata con successo.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore creazione voce: {ex.Message}";
            }

            return RedirectToAction(nameof(Index), new { anno, tab = "anagrafica" });
        }

        // POST: /BudgetStudio/UpdateVoceSpesaBudget
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateVoceSpesaBudget(int id, string codiceSpesa, string descrizione, MetodoPagamentoBudget metodoPagamentoDefault, string? noteDefault, bool isActive, int anno)
        {
            try
            {
                var voce = await _context.VociSpesaBudget.FindAsync(id);
                if (voce == null)
                {
                    TempData["Error"] = "Voce non trovata.";
                    return RedirectToAction(nameof(Index), new { anno, tab = "anagrafica" });
                }

                voce.CodiceSpesa = codiceSpesa.Trim();
                voce.Descrizione = descrizione.Trim();
                voce.MetodoPagamentoDefault = metodoPagamentoDefault;
                voce.NoteDefault = string.IsNullOrWhiteSpace(noteDefault) ? null : noteDefault.Trim();
                voce.IsActive = isActive;
                voce.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                TempData["Success"] = "Voce aggiornata con successo.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore aggiornamento voce: {ex.Message}";
            }

            return RedirectToAction(nameof(Index), new { anno, tab = "anagrafica" });
        }

        // POST: /BudgetStudio/DeleteVoceSpesaBudget
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVoceSpesaBudget(int id, int anno)
        {
            try
            {
                var voce = await _context.VociSpesaBudget.FindAsync(id);
                if (voce == null)
                {
                    TempData["Error"] = "Voce non trovata.";
                    return RedirectToAction(nameof(Index), new { anno, tab = "anagrafica" });
                }

                _context.VociSpesaBudget.Remove(voce);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Voce eliminata.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore eliminazione voce: {ex.Message}";
            }

            return RedirectToAction(nameof(Index), new { anno, tab = "anagrafica" });
        }

        // GET: /BudgetStudio/GetBudgetCell?voceId=1&anno=2025&mese=3
        [HttpGet]
        public async Task<IActionResult> GetBudgetCell(int voceId, int anno, int mese)
        {
            mese = Math.Clamp(mese, 1, 12);

            var cell = await _context.BudgetSpeseMensili
                .Include(x => x.VoceSpesaBudget)
                .FirstOrDefaultAsync(x => x.VoceSpesaBudgetId == voceId && x.Anno == anno && x.Mese == mese);

            if (cell == null)
            {
                var voce = await _context.VociSpesaBudget.FindAsync(voceId);
                if (voce == null) return NotFound();

                return Json(new
                {
                    voceId,
                    voceCodice = voce.CodiceSpesa,
                    voceDescrizione = voce.Descrizione,
                    anno,
                    mese,
                    importo = 0m,
                    pagata = false,
                    metodoPagamento = (int?)null,
                    note = (string?)null,
                    metodoPagamentoDefault = (int)voce.MetodoPagamentoDefault
                });
            }

            return Json(new
            {
                voceId,
                voceCodice = cell.VoceSpesaBudget?.CodiceSpesa,
                voceDescrizione = cell.VoceSpesaBudget?.Descrizione,
                anno = cell.Anno,
                mese = cell.Mese,
                importo = cell.Importo,
                pagata = cell.Pagata,
                metodoPagamento = (int?)cell.MetodoPagamento,
                note = cell.Note,
                metodoPagamentoDefault = (int)(cell.VoceSpesaBudget?.MetodoPagamentoDefault ?? MetodoPagamentoBudget.Bonifico)
            });
        }

        // POST: /BudgetStudio/SaveBudgetCell (inline)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveBudgetCell(int voceId, int anno, int mese, string importo)
        {
            try
            {
                var importoDec = decimal.Parse(importo.Replace(",", "."), CultureInfo.InvariantCulture);
                mese = Math.Clamp(mese, 1, 12);

                var voce = await _context.VociSpesaBudget.FindAsync(voceId);
                if (voce == null) return NotFound();

                var cell = await _context.BudgetSpeseMensili
                    .FirstOrDefaultAsync(x => x.VoceSpesaBudgetId == voceId && x.Anno == anno && x.Mese == mese);

                if (cell == null)
                {
                    if (importoDec == 0m)
                        return Json(new { ok = true });

                    cell = new BudgetSpesaMensile
                    {
                        VoceSpesaBudgetId = voceId,
                        Anno = anno,
                        Mese = mese,
                        Importo = importoDec,
                        Pagata = false,
                        MetodoPagamento = null,
                        Note = null,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                    _context.BudgetSpeseMensili.Add(cell);
                }
                else
                {
                    if (importoDec == 0m && !cell.Pagata && cell.MetodoPagamento == null && string.IsNullOrWhiteSpace(cell.Note))
                    {
                        _context.BudgetSpeseMensili.Remove(cell);
                    }
                    else
                    {
                        cell.Importo = importoDec;
                        cell.UpdatedAt = DateTime.Now;
                    }
                }

                await _context.SaveChangesAsync();
                return Json(new { ok = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { ok = false, error = ex.Message });
            }
        }

        // POST: /BudgetStudio/SaveBudgetCellDetail (modale)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveBudgetCellDetail(int voceId, int anno, int mese, string importo, bool pagata, MetodoPagamentoBudget? metodoPagamento, string? note)
        {
            try
            {
                var importoDec = decimal.Parse(importo.Replace(",", "."), CultureInfo.InvariantCulture);
                mese = Math.Clamp(mese, 1, 12);

                var voce = await _context.VociSpesaBudget.FindAsync(voceId);
                if (voce == null) return NotFound();

                var cell = await _context.BudgetSpeseMensili
                    .FirstOrDefaultAsync(x => x.VoceSpesaBudgetId == voceId && x.Anno == anno && x.Mese == mese);

                if (cell == null)
                {
                    var noteTrim = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
                    if (importoDec == 0m && !pagata && metodoPagamento == null && string.IsNullOrWhiteSpace(noteTrim))
                        return Json(new { ok = true });

                    cell = new BudgetSpesaMensile
                    {
                        VoceSpesaBudgetId = voceId,
                        Anno = anno,
                        Mese = mese,
                        CreatedAt = DateTime.Now
                    };
                    _context.BudgetSpeseMensili.Add(cell);
                }

                cell.Importo = importoDec;
                cell.Pagata = pagata;
                cell.MetodoPagamento = metodoPagamento;
                cell.Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();

                if (cell.Importo == 0m && !cell.Pagata && cell.MetodoPagamento == null && string.IsNullOrWhiteSpace(cell.Note))
                {
                    _context.BudgetSpeseMensili.Remove(cell);
                }
                else
                {
                    cell.UpdatedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                return Json(new { ok = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { ok = false, error = ex.Message });
            }
        }

        // POST: /BudgetStudio/ApplyBudgetVoce (bulk: compila mesi)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyBudgetVoce(int voceId, int anno, int meseDa, int meseA, string importo, int? ritornoMeseDa, int? ritornoMeseA, BudgetPagateFiltro? ritornoPagate, string? tab)
        {
            try
            {
                var voce = await _context.VociSpesaBudget.FindAsync(voceId);
                if (voce == null) return NotFound();

                var da = Math.Clamp(meseDa, 1, 12);
                var a = Math.Clamp(meseA, 1, 12);
                if (da > a) (da, a) = (a, da);

                var importoDec = decimal.Parse(importo.Replace(",", "."), CultureInfo.InvariantCulture);

                var esistenti = await _context.BudgetSpeseMensili
                    .Where(x => x.VoceSpesaBudgetId == voceId && x.Anno == anno && x.Mese >= da && x.Mese <= a)
                    .ToListAsync();

                var map = esistenti.ToDictionary(x => x.Mese, x => x);

                for (int m = da; m <= a; m++)
                {
                    map.TryGetValue(m, out var cell);

                    if (importoDec == 0m)
                    {
                        if (cell != null && !cell.Pagata && cell.MetodoPagamento == null && string.IsNullOrWhiteSpace(cell.Note))
                            _context.BudgetSpeseMensili.Remove(cell);
                        else if (cell != null)
                        {
                            cell.Importo = 0m;
                            cell.UpdatedAt = DateTime.Now;
                        }
                        continue;
                    }

                    if (cell == null)
                    {
                        _context.BudgetSpeseMensili.Add(new BudgetSpesaMensile
                        {
                            VoceSpesaBudgetId = voceId,
                            Anno = anno,
                            Mese = m,
                            Importo = importoDec,
                            Pagata = false,
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        });
                    }
                    else
                    {
                        cell.Importo = importoDec;
                        cell.UpdatedAt = DateTime.Now;
                    }
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Budget aggiornato per la voce selezionata.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore aggiornamento budget: {ex.Message}";
            }

            return RedirectToAction(nameof(Index), new { anno, meseDa = ritornoMeseDa, meseA = ritornoMeseA, pagate = ritornoPagate, tab = tab ?? "prospetto" });
        }

        // POST: /BudgetStudio/ApplyBudgetVoceMesi (bulk: mesi selezionati)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyBudgetVoceMesi(int voceId, int anno, string mesiCsv, string importo, int? ritornoMeseDa, int? ritornoMeseA, BudgetPagateFiltro? ritornoPagate, string? tab)
        {
            try
            {
                var voce = await _context.VociSpesaBudget.FindAsync(voceId);
                if (voce == null) return NotFound();

                var mesi = (mesiCsv ?? "")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(s => int.TryParse(s, out var m) ? m : 0)
                    .Where(m => m >= 1 && m <= 12)
                    .Distinct()
                    .ToList();

                if (!mesi.Any())
                {
                    TempData["Warning"] = "Nessun mese selezionato.";
                    return RedirectToAction(nameof(Index), new { anno, meseDa = ritornoMeseDa, meseA = ritornoMeseA, pagate = ritornoPagate, tab = tab ?? "prospetto" });
                }

                var importoDec = decimal.Parse(importo.Replace(",", "."), CultureInfo.InvariantCulture);

                var esistenti = await _context.BudgetSpeseMensili
                    .Where(x => x.VoceSpesaBudgetId == voceId && x.Anno == anno && mesi.Contains(x.Mese))
                    .ToListAsync();

                var map = esistenti.ToDictionary(x => x.Mese, x => x);

                foreach (var m in mesi)
                {
                    map.TryGetValue(m, out var cell);

                    if (importoDec == 0m)
                    {
                        if (cell != null && !cell.Pagata && cell.MetodoPagamento == null && string.IsNullOrWhiteSpace(cell.Note))
                            _context.BudgetSpeseMensili.Remove(cell);
                        else if (cell != null)
                        {
                            cell.Importo = 0m;
                            cell.UpdatedAt = DateTime.Now;
                        }
                        continue;
                    }

                    if (cell == null)
                    {
                        _context.BudgetSpeseMensili.Add(new BudgetSpesaMensile
                        {
                            VoceSpesaBudgetId = voceId,
                            Anno = anno,
                            Mese = m,
                            Importo = importoDec,
                            Pagata = false,
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        });
                    }
                    else
                    {
                        cell.Importo = importoDec;
                        cell.UpdatedAt = DateTime.Now;
                    }
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Budget aggiornato per i mesi selezionati.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore aggiornamento budget: {ex.Message}";
            }

            return RedirectToAction(nameof(Index), new { anno, meseDa = ritornoMeseDa, meseA = ritornoMeseA, pagate = ritornoPagate, tab = tab ?? "prospetto" });
        }

        // POST: /BudgetStudio/ClearBudgetVoce (bulk: azzera voce per anno)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearBudgetVoce(int voceId, int anno, int? ritornoMeseDa, int? ritornoMeseA, BudgetPagateFiltro? ritornoPagate, string? tab)
        {
            try
            {
                var righe = await _context.BudgetSpeseMensili
                    .Where(x => x.VoceSpesaBudgetId == voceId && x.Anno == anno)
                    .ToListAsync();

                if (righe.Any())
                {
                    _context.BudgetSpeseMensili.RemoveRange(righe);
                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = "Budget azzerato per la voce selezionata.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore azzeramento budget: {ex.Message}";
            }

            return RedirectToAction(nameof(Index), new { anno, meseDa = ritornoMeseDa, meseA = ritornoMeseA, pagate = ritornoPagate, tab = tab ?? "prospetto" });
        }

        // POST: /BudgetStudio/CopyFromPreviousYear (copia dati da anno precedente)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CopyFromPreviousYear(int anno)
        {
            try
            {
                var annoPrecedente = anno - 1;

                // Carica tutte le righe dell'anno precedente
                var righePrecedenti = await _context.BudgetSpeseMensili
                    .Where(x => x.Anno == annoPrecedente)
                    .ToListAsync();

                if (!righePrecedenti.Any())
                {
                    TempData["Warning"] = $"Nessun dato trovato per l'anno {annoPrecedente}.";
                    return RedirectToAction(nameof(Index), new { anno, tab = "prospetto" });
                }

                // Verifica se ci sono già dati nell'anno corrente
                var righeEsistenti = await _context.BudgetSpeseMensili
                    .Where(x => x.Anno == anno)
                    .ToListAsync();

                if (righeEsistenti.Any())
                {
                    // Rimuovi le righe esistenti prima di copiare
                    _context.BudgetSpeseMensili.RemoveRange(righeEsistenti);
                }

                // Copia le righe dall'anno precedente
                var nuoveRighe = righePrecedenti.Select(r => new BudgetSpesaMensile
                {
                    VoceSpesaBudgetId = r.VoceSpesaBudgetId,
                    Anno = anno,
                    Mese = r.Mese,
                    Importo = r.Importo,
                    Pagata = false, // Le nuove righe non sono pagate
                    MetodoPagamento = r.MetodoPagamento,
                    Note = r.Note,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                }).ToList();

                _context.BudgetSpeseMensili.AddRange(nuoveRighe);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Copiati {nuoveRighe.Count} record dall'anno {annoPrecedente} all'anno {anno}.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore copia dati: {ex.Message}";
            }

            return RedirectToAction(nameof(Index), new { anno, tab = "prospetto" });
        }

        // GET: /BudgetStudio/ExportExcel (export del prospetto corrente)
        [HttpGet]
        public async Task<IActionResult> ExportExcel(int? anno, int? meseDa, int? meseA, BudgetPagateFiltro? pagate, bool? nascondiVuote)
        {
            var annoSelezionato = anno ?? DateTime.Today.Year;
            var da = Math.Clamp(meseDa ?? 1, 1, 12);
            var a = Math.Clamp(meseA ?? 12, 1, 12);
            if (da > a) (da, a) = (a, da);

            var filtroPagate = pagate ?? BudgetPagateFiltro.NonPagate;
            var filtroNascondiVuote = nascondiVuote ?? true;

            // Nomi mesi italiani
            var mesiNomi = new Dictionary<int, string>
            {
                { 1, "Gen" }, { 2, "Feb" }, { 3, "Mar" }, { 4, "Apr" },
                { 5, "Mag" }, { 6, "Giu" }, { 7, "Lug" }, { 8, "Ago" },
                { 9, "Set" }, { 10, "Ott" }, { 11, "Nov" }, { 12, "Dic" }
            };

            var voci = await _context.VociSpesaBudget
                .Where(v => v.IsActive)
                .OrderBy(v => v.CodiceSpesa)
                .ToListAsync();

            var queryMensile = _context.BudgetSpeseMensili
                .Include(r => r.VoceSpesaBudget)
                .Where(r => r.Anno == annoSelezionato && r.Mese >= da && r.Mese <= a);

            if (filtroPagate == BudgetPagateFiltro.NonPagate)
                queryMensile = queryMensile.Where(r => !r.Pagata);
            else if (filtroPagate == BudgetPagateFiltro.Pagate)
                queryMensile = queryMensile.Where(r => r.Pagata);

            var righe = await queryMensile.ToListAsync();
            var map = righe.ToDictionary(x => (x.VoceSpesaBudgetId, x.Mese), x => x);

            // Filtra voci vuote se richiesto
            var vociFiltrate = voci;
            if (filtroNascondiVuote)
            {
                var vociConImporti = righe
                    .Where(r => r.Importo != 0)
                    .Select(r => r.VoceSpesaBudgetId)
                    .Distinct()
                    .ToHashSet();

                vociFiltrate = voci.Where(v => vociConImporti.Contains(v.Id)).ToList();
            }

            // Calcolo totali
            var totaliMese = new Dictionary<int, decimal>();
            decimal totaleAnno = 0;
            for (var m = da; m <= a; m++)
            {
                var tot = righe.Where(r => r.Mese == m).Sum(r => r.Importo);
                totaliMese[m] = tot;
                totaleAnno += tot;
            }

            // Crea workbook Excel
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add($"Budget {annoSelezionato}");

            // Header
            int col = 1;
            worksheet.Cell(1, col++).Value = "Voce";
            for (int m = da; m <= a; m++)
            {
                worksheet.Cell(1, col++).Value = mesiNomi[m];
            }
            worksheet.Cell(1, col).Value = "Totale";

            // Stile header
            var headerRange = worksheet.Range(1, 1, 1, col);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Dati
            int row = 2;
            foreach (var v in vociFiltrate)
            {
                col = 1;
                worksheet.Cell(row, col++).Value = $"{v.CodiceSpesa} - {v.Descrizione}";

                decimal totRiga = 0;
                for (int m = da; m <= a; m++)
                {
                    var cell = map.TryGetValue((v.Id, m), out var c) ? c : null;
                    var importo = cell?.Importo ?? 0;
                    totRiga += importo;

                    var xlCell = worksheet.Cell(row, col++);
                    if (importo != 0)
                    {
                        xlCell.Value = importo;
                        xlCell.Style.NumberFormat.Format = "#,##0";

                        // Colori: verde se pagata, rosso chiaro se da pagare
                        if (cell?.Pagata == true)
                            xlCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#d4edda");
                        else
                            xlCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#ffe5e5");
                    }
                }

                var totCell = worksheet.Cell(row, col);
                totCell.Value = totRiga;
                totCell.Style.NumberFormat.Format = "#,##0";
                totCell.Style.Font.Bold = true;

                row++;
            }

            // Riga totali
            col = 1;
            worksheet.Cell(row, col++).Value = "TOTALE";
            worksheet.Cell(row, col - 1).Style.Font.Bold = true;

            for (int m = da; m <= a; m++)
            {
                var totCell = worksheet.Cell(row, col++);
                totCell.Value = totaliMese.GetValueOrDefault(m);
                totCell.Style.NumberFormat.Format = "#,##0";
                totCell.Style.Font.Bold = true;
            }

            var grandTotCell = worksheet.Cell(row, col);
            grandTotCell.Value = totaleAnno;
            grandTotCell.Style.NumberFormat.Format = "#,##0";
            grandTotCell.Style.Font.Bold = true;
            grandTotCell.Style.Fill.BackgroundColor = XLColor.LightBlue;

            // Stile riga totali
            var totalsRange = worksheet.Range(row, 1, row, col);
            totalsRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            // Auto-fit colonne
            worksheet.Columns().AdjustToContents();
            worksheet.Column(1).Width = 40; // Colonna Voce più larga

            // Genera file
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"BudgetStudio_{annoSelezionato}_{mesiNomi[da]}-{mesiNomi[a]}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}



