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

            // Carica macro voci con le voci analitiche
            var macroVoci = await _context.MacroVociBudget
                .Where(m => m.IsActive)
                .Include(m => m.VociAnalitiche.Where(v => v.IsActive))
                .OrderBy(m => m.Ordine)
                .ThenBy(m => m.Codice)
                .ToListAsync();

            // Carica tutte le voci analitiche (con relazione MacroVoce)
            var voci = await _context.VociSpesaBudget
                .Include(v => v.MacroVoce)
                .Where(v => v.IsActive)
                .OrderBy(v => v.MacroVoce != null ? v.MacroVoce.Ordine : 9999)
                .ThenBy(v => v.CodiceSpesa)
                .ToListAsync();

            // Prima carica TUTTE le righe per determinare quali voci hanno importi (per filtro "nascondi vuote")
            var tutteRighe = await _context.BudgetSpeseMensili
                .Where(r => r.Anno == annoSelezionato && r.Mese >= da && r.Mese <= a)
                .ToListAsync();

            // Poi applica il filtro "pagate" per la visualizzazione
            var righe = tutteRighe.AsEnumerable();
            if (filtroPagate == BudgetPagateFiltro.NonPagate)
                righe = righe.Where(r => !r.Pagata);
            else if (filtroPagate == BudgetPagateFiltro.Pagate)
                righe = righe.Where(r => r.Pagata);
            var righeList = righe.ToList();

            // Se nascondiVuote è attivo, filtra le voci che hanno almeno un importo != 0 
            // INDIPENDENTEMENTE dallo stato pagata/non pagata
            var vociFiltrate = voci;
            if (filtroNascondiVuote)
            {
                var vociConImporti = tutteRighe
                    .Where(r => r.Importo != 0)
                    .Select(r => r.VoceSpesaBudgetId)
                    .Distinct()
                    .ToHashSet();

                vociFiltrate = voci.Where(v => vociConImporti.Contains(v.Id)).ToList();
            }

            // Voci senza macro voce assegnata
            var vociSenzaMacro = vociFiltrate.Where(v => v.MacroVoceBudgetId == null).ToList();

            // Carica banche e saldi
            var banche = await _context.BancheBudget
                .Where(b => b.IsActive)
                .OrderBy(b => b.Ordine)
                .ThenBy(b => b.Nome)
                .ToListAsync();

            var saldiBanche = await _context.SaldiBancheMese
                .Where(s => s.Anno == annoSelezionato && s.Mese >= da && s.Mese <= a)
                .ToListAsync();

            var vm = new BudgetStudioViewModel
            {
                Anno = annoSelezionato,
                MeseDa = da,
                MeseA = a,
                PagateFiltro = filtroPagate,
                NascondiVuote = filtroNascondiVuote,
                MacroVoci = macroVoci,
                VociTutte = voci, // TUTTE le voci per Anagrafica
                Voci = vociFiltrate, // Voci filtrate per Prospetto
                VociSenzaMacro = vociSenzaMacro,
                RigheMensili = righeList,
                Banche = banche,
                SaldiBanche = saldiBanche
            };

            vm.Map = righeList.ToDictionary(x => (x.VoceSpesaBudgetId, x.Mese), x => x);
            vm.MapSaldiBanche = saldiBanche.ToDictionary(x => (x.BancaBudgetId, x.Mese), x => x.Saldo);

            // Calcola totali spese per mese
            for (var m = da; m <= a; m++)
            {
                var tot = righeList.Where(r => r.Mese == m).Sum(r => r.Importo);
                vm.TotaliMese[m] = tot;
                vm.TotaleAnno += tot;

                // Totale saldi banche per mese
                var totBanche = saldiBanche.Where(s => s.Mese == m).Sum(s => s.Saldo);
                vm.TotaliBancheMese[m] = totBanche;
            }

            // Calcola Cash Flow: saldo iniziale e differenza per ogni mese
            decimal riporto = 0;
            for (var m = da; m <= a; m++)
            {
                // Saldo iniziale = riporto mese precedente + banche/versamenti mese corrente
                var bancheMese = vm.TotaliBancheMese.GetValueOrDefault(m);
                var saldoIniziale = riporto + bancheMese;
                vm.SaldoInizialeMese[m] = saldoIniziale;

                // Saldo finale (differenza) = saldo iniziale - spese del mese
                var saldoFinale = saldoIniziale - vm.TotaliMese.GetValueOrDefault(m);
                vm.DifferenzaMese[m] = saldoFinale;

                // Il riporto per il mese successivo = saldo finale di questo mese
                riporto = saldoFinale;
            }

            ViewBag.ActiveTab = string.Equals(tab, "prospetto", StringComparison.OrdinalIgnoreCase) ? "prospetto" : "anagrafica";
            ViewData["Title"] = "Budget Studio";
            return View(vm);
        }

        // POST: /BudgetStudio/CreateVoceSpesaBudget
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateVoceSpesaBudget(string codiceSpesa, string descrizione, MetodoPagamentoBudget metodoPagamentoDefault, string? noteDefault, int? macroVoceId, int anno)
        {
            try
            {
                // Verifica se il codice esiste già
                var esistente = await _context.VociSpesaBudget.AnyAsync(v => v.CodiceSpesa == codiceSpesa.Trim());
                if (esistente)
                {
                    TempData["Error"] = $"Esiste già una voce con codice '{codiceSpesa}'. Usa un codice diverso.";
                    return RedirectToAction(nameof(Index), new { anno, tab = "anagrafica" });
                }

                var voce = new VoceSpesaBudget
                {
                    CodiceSpesa = codiceSpesa.Trim(),
                    Descrizione = descrizione.Trim(),
                    MetodoPagamentoDefault = metodoPagamentoDefault,
                    NoteDefault = string.IsNullOrWhiteSpace(noteDefault) ? null : noteDefault.Trim(),
                    MacroVoceBudgetId = macroVoceId,
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
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                TempData["Error"] = $"Errore creazione voce: {innerMsg}";
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
                    formula = (string?)null,
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
                formula = cell.Formula,
                metodoPagamentoDefault = (int)(cell.VoceSpesaBudget?.MetodoPagamentoDefault ?? MetodoPagamentoBudget.Bonifico)
            });
        }

        // POST: /BudgetStudio/SaveBudgetCell (inline)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveBudgetCell(int voceId, int anno, int mese, string importo, string? formula = null)
        {
            try
            {
                var importoDec = decimal.Parse(importo.Replace(",", "."), CultureInfo.InvariantCulture);
                mese = Math.Clamp(mese, 1, 12);

                var voce = await _context.VociSpesaBudget.FindAsync(voceId);
                if (voce == null) return NotFound();

                var cell = await _context.BudgetSpeseMensili
                    .FirstOrDefaultAsync(x => x.VoceSpesaBudgetId == voceId && x.Anno == anno && x.Mese == mese);

                // Determina se la formula va salvata (solo se contiene operatori matematici)
                string? formulaDaSalvare = null;
                if (!string.IsNullOrWhiteSpace(formula) && 
                    (formula.Contains('+') || formula.Contains('-') || formula.Contains('*') || formula.Contains('/')))
                {
                    formulaDaSalvare = formula.Trim();
                }

                if (cell == null)
                {
                    if (importoDec == 0m && string.IsNullOrWhiteSpace(formulaDaSalvare))
                        return Json(new { ok = true });

                    cell = new BudgetSpesaMensile
                    {
                        VoceSpesaBudgetId = voceId,
                        Anno = anno,
                        Mese = mese,
                        Importo = importoDec,
                        Formula = formulaDaSalvare,
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
                    if (importoDec == 0m && !cell.Pagata && cell.MetodoPagamento == null && string.IsNullOrWhiteSpace(cell.Note) && string.IsNullOrWhiteSpace(formulaDaSalvare))
                    {
                        _context.BudgetSpeseMensili.Remove(cell);
                    }
                    else
                    {
                        cell.Importo = importoDec;
                        cell.Formula = formulaDaSalvare;
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
                .Include(v => v.MacroVoce)
                .Where(v => v.IsActive)
                .OrderBy(v => v.MacroVoce != null ? v.MacroVoce.Ordine : 9999)
                .ThenBy(v => v.CodiceSpesa)
                .ToListAsync();

            var macroVoci = await _context.MacroVociBudget
                .Where(m => m.IsActive)
                .OrderBy(m => m.Ordine)
                .ThenBy(m => m.Codice)
                .ToListAsync();

            // Carica tutte le righe per filtro voci
            var tutteRighe = await _context.BudgetSpeseMensili
                .Where(r => r.Anno == annoSelezionato && r.Mese >= da && r.Mese <= a)
                .ToListAsync();

            // Applica filtro pagate per visualizzazione
            var righe = tutteRighe.AsEnumerable();
            if (filtroPagate == BudgetPagateFiltro.NonPagate)
                righe = righe.Where(r => !r.Pagata);
            else if (filtroPagate == BudgetPagateFiltro.Pagate)
                righe = righe.Where(r => r.Pagata);
            var righeList = righe.ToList();

            var map = righeList.ToDictionary(x => (x.VoceSpesaBudgetId, x.Mese), x => x);

            // Filtra voci vuote se richiesto (usando tutte le righe)
            var vociFiltrate = voci;
            if (filtroNascondiVuote)
            {
                var vociConImporti = tutteRighe
                    .Where(r => r.Importo != 0)
                    .Select(r => r.VoceSpesaBudgetId)
                    .Distinct()
                    .ToHashSet();

                vociFiltrate = voci.Where(v => vociConImporti.Contains(v.Id)).ToList();
            }

            // Carica banche e saldi
            var banche = await _context.BancheBudget
                .Where(b => b.IsActive)
                .OrderBy(b => b.Ordine)
                .ThenBy(b => b.Nome)
                .ToListAsync();

            var saldiBanche = await _context.SaldiBancheMese
                .Where(s => s.Anno == annoSelezionato && s.Mese >= da && s.Mese <= a)
                .ToListAsync();

            var mapSaldiBanche = saldiBanche.ToDictionary(x => (x.BancaBudgetId, x.Mese), x => x.Saldo);

            // Calcolo totali spese
            var totaliMese = new Dictionary<int, decimal>();
            decimal totaleAnno = 0;
            for (var m = da; m <= a; m++)
            {
                var tot = righeList.Where(r => r.Mese == m).Sum(r => r.Importo);
                totaliMese[m] = tot;
                totaleAnno += tot;
            }

            // Calcolo totali banche
            var totaliBancheMese = new Dictionary<int, decimal>();
            for (var m = da; m <= a; m++)
            {
                totaliBancheMese[m] = saldiBanche.Where(s => s.Mese == m).Sum(s => s.Saldo);
            }

            // Calcolo Cash Flow
            var saldoInizialeMese = new Dictionary<int, decimal>();
            var saldoFinaleMese = new Dictionary<int, decimal>();
            decimal riporto = 0;
            for (var m = da; m <= a; m++)
            {
                var bancheMese = totaliBancheMese.GetValueOrDefault(m);
                var speseMese = totaliMese.GetValueOrDefault(m);
                var saldoIniziale = riporto + bancheMese;
                var saldoFinale = saldoIniziale - speseMese;

                saldoInizialeMese[m] = saldoIniziale;
                saldoFinaleMese[m] = saldoFinale;
                riporto = saldoFinale;
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
            int lastCol = col;

            // Stile header
            var headerRange = worksheet.Range(1, 1, 1, col);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#343a40");
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            int row = 2;

            // ========== SEZIONE SPESE (per macro voce) ==========
            foreach (var macro in macroVoci)
            {
                var vociMacro = vociFiltrate.Where(v => v.MacroVoceBudgetId == macro.Id).ToList();
                if (!vociMacro.Any()) continue;

                // Riga macro voce (header gruppo)
                col = 1;
                worksheet.Cell(row, col++).Value = $"▶ {macro.Codice} - {macro.Descrizione}";
                
                decimal totMacroAnno = 0;
                for (int m = da; m <= a; m++)
                {
                    var totMacroMese = vociMacro.Sum(v => {
                        map.TryGetValue((v.Id, m), out var cell);
                        return cell?.Importo ?? 0m;
                    });
                    totMacroAnno += totMacroMese;
                    
                    var xlCell = worksheet.Cell(row, col++);
                    xlCell.Value = totMacroMese;
                    xlCell.Style.NumberFormat.Format = "#,##0";
                    xlCell.Style.Font.Bold = true;
                }
                
                var macroTotCell = worksheet.Cell(row, col);
                macroTotCell.Value = totMacroAnno;
                macroTotCell.Style.NumberFormat.Format = "#,##0";
                macroTotCell.Style.Font.Bold = true;

                // Stile riga macro
                var macroRange = worksheet.Range(row, 1, row, lastCol);
                macroRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#e3f2fd");
                macroRange.Style.Font.Bold = true;
                row++;

                // Voci analitiche sotto la macro
                foreach (var v in vociMacro)
                {
                    col = 1;
                    worksheet.Cell(row, col++).Value = $"   {v.CodiceSpesa} - {v.Descrizione}";

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
            }

            // Voci senza macro voce
            var vociSenzaMacro = vociFiltrate.Where(v => v.MacroVoceBudgetId == null).ToList();
            if (vociSenzaMacro.Any())
            {
                // Header "Non categorizzate"
                col = 1;
                worksheet.Cell(row, col).Value = "⚠ Voci non categorizzate";
                var warningRange = worksheet.Range(row, 1, row, lastCol);
                warningRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#fff3cd");
                warningRange.Style.Font.Italic = true;
                row++;

                foreach (var v in vociSenzaMacro)
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
            }

            // ========== RIGA TOTALE SPESE ==========
            col = 1;
            worksheet.Cell(row, col++).Value = "TOTALE SPESE";
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

            var totalsRange = worksheet.Range(row, 1, row, lastCol);
            totalsRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#6c757d");
            totalsRange.Style.Font.FontColor = XLColor.White;
            totalsRange.Style.Font.Bold = true;
            row++;

            // ========== SEZIONE BANCHE ==========
            row++; // Riga vuota
            
            // Header banche
            col = 1;
            worksheet.Cell(row, col).Value = "💰 BANCHE / ENTRATE";
            var bancheHeaderRange = worksheet.Range(row, 1, row, lastCol);
            bancheHeaderRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#17a2b8");
            bancheHeaderRange.Style.Font.FontColor = XLColor.White;
            bancheHeaderRange.Style.Font.Bold = true;
            row++;

            foreach (var banca in banche)
            {
                col = 1;
                worksheet.Cell(row, col++).Value = banca.Nome;

                decimal totBanca = 0;
                for (int m = da; m <= a; m++)
                {
                    var saldo = mapSaldiBanche.TryGetValue((banca.Id, m), out var s) ? s : 0;
                    totBanca += saldo;

                    var xlCell = worksheet.Cell(row, col++);
                    if (saldo != 0)
                    {
                        xlCell.Value = saldo;
                        xlCell.Style.NumberFormat.Format = "#,##0";
                        xlCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#d1ecf1");
                    }
                }

                var totCell = worksheet.Cell(row, col);
                totCell.Value = totBanca;
                totCell.Style.NumberFormat.Format = "#,##0";
                totCell.Style.Font.Bold = true;
                row++;
            }

            // Riga Totale Banche
            col = 1;
            worksheet.Cell(row, col++).Value = "TOTALE BANCHE";
            worksheet.Cell(row, col - 1).Style.Font.Bold = true;

            decimal totBancheAnno = 0;
            for (int m = da; m <= a; m++)
            {
                var tot = totaliBancheMese.GetValueOrDefault(m);
                totBancheAnno += tot;

                var xlCell = worksheet.Cell(row, col++);
                xlCell.Value = tot;
                xlCell.Style.NumberFormat.Format = "#,##0";
                xlCell.Style.Font.Bold = true;
            }

            var totBancheCell = worksheet.Cell(row, col);
            totBancheCell.Value = totBancheAnno;
            totBancheCell.Style.NumberFormat.Format = "#,##0";
            totBancheCell.Style.Font.Bold = true;

            var totBancheRange = worksheet.Range(row, 1, row, lastCol);
            totBancheRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#17a2b8");
            totBancheRange.Style.Font.FontColor = XLColor.White;
            row++;

            // ========== SEZIONE CASH FLOW ==========
            row++; // Riga vuota

            // Header Cash Flow
            col = 1;
            worksheet.Cell(row, col).Value = "📊 CASH FLOW";
            var cashFlowHeaderRange = worksheet.Range(row, 1, row, lastCol);
            cashFlowHeaderRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#28a745");
            cashFlowHeaderRange.Style.Font.FontColor = XLColor.White;
            cashFlowHeaderRange.Style.Font.Bold = true;
            row++;

            // Riga Saldo Iniziale (Riporto + Banche)
            col = 1;
            worksheet.Cell(row, col++).Value = "Saldo Iniziale (Riporto + Banche)";
            for (int m = da; m <= a; m++)
            {
                var xlCell = worksheet.Cell(row, col++);
                xlCell.Value = saldoInizialeMese.GetValueOrDefault(m);
                xlCell.Style.NumberFormat.Format = "#,##0";
            }
            worksheet.Cell(row, lastCol).Value = ""; // No totale per questa riga
            var saldoInizialeRange = worksheet.Range(row, 1, row, lastCol);
            saldoInizialeRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#e8f5e9");
            row++;

            // Riga Spese
            col = 1;
            worksheet.Cell(row, col++).Value = "- Spese";
            for (int m = da; m <= a; m++)
            {
                var xlCell = worksheet.Cell(row, col++);
                xlCell.Value = totaliMese.GetValueOrDefault(m);
                xlCell.Style.NumberFormat.Format = "#,##0";
                xlCell.Style.Font.FontColor = XLColor.Red;
            }
            var speseCell = worksheet.Cell(row, lastCol);
            speseCell.Value = totaleAnno;
            speseCell.Style.NumberFormat.Format = "#,##0";
            speseCell.Style.Font.FontColor = XLColor.Red;
            row++;

            // Riga Saldo Finale (Differenza/Riporto)
            col = 1;
            worksheet.Cell(row, col++).Value = "= Saldo Finale (Riporto mese succ.)";
            worksheet.Cell(row, col - 1).Style.Font.Bold = true;
            for (int m = da; m <= a; m++)
            {
                var saldo = saldoFinaleMese.GetValueOrDefault(m);
                var xlCell = worksheet.Cell(row, col++);
                xlCell.Value = saldo;
                xlCell.Style.NumberFormat.Format = "#,##0";
                xlCell.Style.Font.Bold = true;
                xlCell.Style.Font.FontColor = saldo >= 0 ? XLColor.Green : XLColor.Red;
            }
            var saldoFinaleCell = worksheet.Cell(row, lastCol);
            saldoFinaleCell.Value = saldoFinaleMese.GetValueOrDefault(a);
            saldoFinaleCell.Style.NumberFormat.Format = "#,##0";
            saldoFinaleCell.Style.Font.Bold = true;
            saldoFinaleCell.Style.Font.FontColor = saldoFinaleMese.GetValueOrDefault(a) >= 0 ? XLColor.Green : XLColor.Red;

            var saldoFinaleRange = worksheet.Range(row, 1, row, lastCol);
            saldoFinaleRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#c8e6c9");
            saldoFinaleRange.Style.Font.Bold = true;

            // Auto-fit colonne
            worksheet.Columns().AdjustToContents();
            worksheet.Column(1).Width = 45; // Colonna Voce più larga

            // Genera file
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"BudgetStudio_{annoSelezionato}_{mesiNomi[da]}-{mesiNomi[a]}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        #region Banche CRUD

        // POST: /BudgetStudio/CreateBanca
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBanca(string nome, string? iban, int anno)
        {
            try
            {
                var banca = new BancaBudget
                {
                    Nome = nome,
                    Iban = iban,
                    IsActive = true,
                    Ordine = await _context.BancheBudget.CountAsync(),
                    CreatedAt = DateTime.Now
                };
                _context.BancheBudget.Add(banca);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Banca '{nome}' creata con successo.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore creazione banca: {ex.Message}";
            }
            return RedirectToAction(nameof(Index), new { anno, tab = "prospetto" });
        }

        // POST: /BudgetStudio/UpdateBanca
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBanca(int id, string nome, string? iban, int anno)
        {
            try
            {
                var banca = await _context.BancheBudget.FindAsync(id);
                if (banca != null)
                {
                    banca.Nome = nome;
                    banca.Iban = iban;
                    banca.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Banca '{nome}' aggiornata.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore aggiornamento banca: {ex.Message}";
            }
            return RedirectToAction(nameof(Index), new { anno, tab = "prospetto" });
        }

        // POST: /BudgetStudio/DeleteBanca
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBanca(int id, int anno)
        {
            try
            {
                var banca = await _context.BancheBudget.FindAsync(id);
                if (banca != null)
                {
                    banca.IsActive = false;
                    banca.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Banca '{banca.Nome}' eliminata.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore eliminazione banca: {ex.Message}";
            }
            return RedirectToAction(nameof(Index), new { anno, tab = "prospetto" });
        }

        // POST: /BudgetStudio/SaveSaldoBanca (inline, AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveSaldoBanca(int bancaId, int anno, int mese, string saldo)
        {
            try
            {
                var saldoDec = decimal.Parse(saldo.Replace(".", "").Replace(",", "."), CultureInfo.InvariantCulture);
                mese = Math.Clamp(mese, 1, 12);

                var existing = await _context.SaldiBancheMese
                    .FirstOrDefaultAsync(x => x.BancaBudgetId == bancaId && x.Anno == anno && x.Mese == mese);

                if (existing == null)
                {
                    if (saldoDec == 0)
                        return Json(new { ok = true });

                    var newSaldo = new SaldoBancaMese
                    {
                        BancaBudgetId = bancaId,
                        Anno = anno,
                        Mese = mese,
                        Saldo = saldoDec,
                        CreatedAt = DateTime.Now
                    };
                    _context.SaldiBancheMese.Add(newSaldo);
                }
                else
                {
                    if (saldoDec == 0)
                    {
                        _context.SaldiBancheMese.Remove(existing);
                    }
                    else
                    {
                        existing.Saldo = saldoDec;
                        existing.UpdatedAt = DateTime.Now;
                    }
                }

                await _context.SaveChangesAsync();
                return Json(new { ok = true, saldo = saldoDec });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, error = ex.Message });
            }
        }

        // GET: /BudgetStudio/GetBanche (per modale)
        [HttpGet]
        public async Task<IActionResult> GetBanche()
        {
            var banche = await _context.BancheBudget
                .Where(b => b.IsActive)
                .OrderBy(b => b.Ordine)
                .ThenBy(b => b.Nome)
                .Select(b => new { b.Id, b.Nome, b.Iban })
                .ToListAsync();

            return Json(banche);
        }

        #endregion

        #region Macro Voci CRUD

        // POST: /BudgetStudio/CreateMacroVoce
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMacroVoce(string codice, string descrizione, int anno)
        {
            try
            {
                var macroVoce = new MacroVoceBudget
                {
                    Codice = codice.Trim().ToUpper(),
                    Descrizione = descrizione.Trim(),
                    IsActive = true,
                    Ordine = await _context.MacroVociBudget.CountAsync(),
                    CreatedAt = DateTime.Now
                };
                _context.MacroVociBudget.Add(macroVoce);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Macro voce '{codice}' creata con successo.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore creazione macro voce: {ex.Message}";
            }
            return RedirectToAction(nameof(Index), new { anno, tab = "anagrafica" });
        }

        // POST: /BudgetStudio/UpdateMacroVoce
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMacroVoce(int id, string codice, string descrizione, int ordine, bool isActive, int anno)
        {
            try
            {
                var macroVoce = await _context.MacroVociBudget.FindAsync(id);
                if (macroVoce != null)
                {
                    macroVoce.Codice = codice.Trim().ToUpper();
                    macroVoce.Descrizione = descrizione.Trim();
                    macroVoce.Ordine = ordine;
                    macroVoce.IsActive = isActive;
                    macroVoce.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Macro voce '{codice}' aggiornata.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore aggiornamento macro voce: {ex.Message}";
            }
            return RedirectToAction(nameof(Index), new { anno, tab = "anagrafica" });
        }

        // POST: /BudgetStudio/DeleteMacroVoce
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMacroVoce(int id, int anno)
        {
            try
            {
                var macroVoce = await _context.MacroVociBudget.FindAsync(id);
                if (macroVoce != null)
                {
                    // Rimuovi associazione dalle voci analitiche
                    var vociAssociate = await _context.VociSpesaBudget
                        .Where(v => v.MacroVoceBudgetId == id)
                        .ToListAsync();
                    foreach (var v in vociAssociate)
                    {
                        v.MacroVoceBudgetId = null;
                    }
                    
                    _context.MacroVociBudget.Remove(macroVoce);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Macro voce '{macroVoce.Codice}' eliminata.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore eliminazione macro voce: {ex.Message}";
            }
            return RedirectToAction(nameof(Index), new { anno, tab = "anagrafica" });
        }

        // POST: /BudgetStudio/AssociaVoceAMacro
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssociaVoceAMacro(int voceId, int? macroVoceId, int anno)
        {
            try
            {
                var voce = await _context.VociSpesaBudget.FindAsync(voceId);
                if (voce != null)
                {
                    voce.MacroVoceBudgetId = macroVoceId;
                    voce.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Voce associata correttamente.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore associazione: {ex.Message}";
            }
            return RedirectToAction(nameof(Index), new { anno, tab = "anagrafica" });
        }

        // GET: /BudgetStudio/GetMacroVoci (JSON per dropdown)
        [HttpGet]
        public async Task<IActionResult> GetMacroVoci()
        {
            var macroVoci = await _context.MacroVociBudget
                .Where(m => m.IsActive)
                .OrderBy(m => m.Ordine)
                .ThenBy(m => m.Codice)
                .Select(m => new { m.Id, m.Codice, m.Descrizione })
                .ToListAsync();

            return Json(macroVoci);
        }

        #endregion
    }
}



