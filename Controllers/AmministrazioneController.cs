using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioCG.Web.Data;
using StudioCG.Web.Models.Fatturazione;
using StudioCG.Web.Models.ViewModels;
using System.Globalization;
using ClosedXML.Excel;

namespace StudioCG.Web.Controllers
{
    [Authorize]
    public class AmministrazioneController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AmministrazioneController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Helper: Ottiene l'anno corrente di fatturazione dalla tabella AnniFatturazione
        private async Task<int> GetAnnoCorrenteFatturazione()
        {
            var annoCorrente = await _context.AnniFatturazione
                .Where(a => a.IsCurrent)
                .Select(a => a.Anno)
                .FirstOrDefaultAsync();
            
            return annoCorrente > 0 ? annoCorrente : DateTime.Now.Year;
        }

        // GET: /Amministrazione - Dashboard
        public async Task<IActionResult> Index(int? anno)
        {
            // Determina l'anno da visualizzare (usa anno corrente fatturazione se non specificato)
            var annoCorrente = anno ?? await GetAnnoCorrenteFatturazione();
            
            // Statistiche generali
            var mandatiAttivi = await _context.MandatiClienti
                .Where(m => m.Anno == annoCorrente && m.IsActive)
                .CountAsync();
            
            var totaleImportoMandati = await _context.MandatiClienti
                .Where(m => m.Anno == annoCorrente && m.IsActive)
                .SumAsync(m => m.ImportoAnnuo);

            var scadenze = await _context.ScadenzeFatturazione
                .Where(s => s.Anno == annoCorrente)
                .ToListAsync();

            var scadenzeAperte = scadenze.Count(s => s.Stato == StatoScadenza.Aperta);
            var scadenzeProforma = scadenze.Count(s => s.Stato == StatoScadenza.Proforma);
            var scadenzeFatturate = scadenze.Count(s => s.Stato == StatoScadenza.Fatturata);

            var totaleFatturato = scadenze
                .Where(s => s.NumeroFattura.HasValue)
                .Sum(s => s.ImportoMandato);

            // Incassi
            var incassiAnno = await _context.IncassiFatture
                .Include(i => i.ScadenzaFatturazione)
                .Where(i => i.ScadenzaFatturazione!.Anno == annoCorrente)
                .ToListAsync();
            
            var totaleIncassato = incassiAnno.Sum(i => i.ImportoIncassato);
            
            var fattureDaIncassare = scadenze
                .Where(s => s.NumeroFattura.HasValue && s.StatoIncasso != StatoIncasso.Incassata)
                .Count();
            
            // Anni disponibili per dropdown
            var anniDisponibili = await _context.MandatiClienti
                .Select(m => m.Anno)
                .Distinct()
                .OrderByDescending(a => a)
                .ToListAsync();

            if (!anniDisponibili.Contains(annoCorrente))
                anniDisponibili.Add(annoCorrente);
            anniDisponibili = anniDisponibili.OrderByDescending(a => a).ToList();

            // Ultime scadenze
            var prossimeScadenze = await _context.ScadenzeFatturazione
                .Include(s => s.Cliente)
                .Where(s => s.Anno == annoCorrente && s.Stato == StatoScadenza.Aperta)
                .OrderBy(s => s.DataScadenza)
                .Take(10)
                .ToListAsync();

            // ViewBag
            ViewBag.AnnoSelezionato = annoCorrente;
            ViewBag.AnniDisponibili = anniDisponibili;
            ViewBag.MandatiAttivi = mandatiAttivi;
            ViewBag.TotaleImportoMandati = totaleImportoMandati;
            ViewBag.ScadenzeAperte = scadenzeAperte;
            ViewBag.ScadenzeProforma = scadenzeProforma;
            ViewBag.ScadenzeFatturate = scadenzeFatturate;
            ViewBag.TotaleFatturato = totaleFatturato;
            ViewBag.TotaleIncassato = totaleIncassato;
            ViewBag.FattureDaIncassare = fattureDaIncassare;
            ViewBag.ProssimeScadenze = prossimeScadenze;

            // ==========================================
            // RIEPILOGO ECONOMICO CON TRIMESTRI
            // ==========================================

            // Funzione helper per filtrare per trimestre
            Func<DateTime, int> getTrimestre = (data) => (data.Month - 1) / 3 + 1;

            // MANDATI per trimestre (basato sulla data scadenza)
            var mandatiQ1 = scadenze.Where(s => getTrimestre(s.DataScadenza) == 1).Sum(s => s.ImportoMandato);
            var mandatiQ2 = scadenze.Where(s => getTrimestre(s.DataScadenza) == 2).Sum(s => s.ImportoMandato);
            var mandatiQ3 = scadenze.Where(s => getTrimestre(s.DataScadenza) == 3).Sum(s => s.ImportoMandato);
            var mandatiQ4 = scadenze.Where(s => getTrimestre(s.DataScadenza) == 4).Sum(s => s.ImportoMandato);
            var mandatiTotale = mandatiQ1 + mandatiQ2 + mandatiQ3 + mandatiQ4;

            // RIMBORSI SPESE per trimestre
            var rimborsiQ1 = scadenze.Where(s => getTrimestre(s.DataScadenza) == 1).Sum(s => s.RimborsoSpese);
            var rimborsiQ2 = scadenze.Where(s => getTrimestre(s.DataScadenza) == 2).Sum(s => s.RimborsoSpese);
            var rimborsiQ3 = scadenze.Where(s => getTrimestre(s.DataScadenza) == 3).Sum(s => s.RimborsoSpese);
            var rimborsiQ4 = scadenze.Where(s => getTrimestre(s.DataScadenza) == 4).Sum(s => s.RimborsoSpese);
            var rimborsiTotale = rimborsiQ1 + rimborsiQ2 + rimborsiQ3 + rimborsiQ4;

            // SPESE PRATICHE per trimestre
            var spesePratiche = await _context.SpesePratiche
                .Include(sp => sp.ScadenzaFatturazione)
                .Where(sp => sp.ScadenzaFatturazione!.Anno == annoCorrente)
                .ToListAsync();

            var spesePraticheQ1 = spesePratiche.Where(sp => getTrimestre(sp.ScadenzaFatturazione!.DataScadenza) == 1).Sum(sp => sp.Importo);
            var spesePraticheQ2 = spesePratiche.Where(sp => getTrimestre(sp.ScadenzaFatturazione!.DataScadenza) == 2).Sum(sp => sp.Importo);
            var spesePraticheQ3 = spesePratiche.Where(sp => getTrimestre(sp.ScadenzaFatturazione!.DataScadenza) == 3).Sum(sp => sp.Importo);
            var spesePraticheQ4 = spesePratiche.Where(sp => getTrimestre(sp.ScadenzaFatturazione!.DataScadenza) == 4).Sum(sp => sp.Importo);
            var spesePraticheTotale = spesePraticheQ1 + spesePraticheQ2 + spesePraticheQ3 + spesePraticheQ4;

            // FATTURE IN CLOUD (solo totale annuo)
            var fattureCloudTotale = await _context.FattureCloud
                .Where(fc => fc.Anno == annoCorrente)
                .SumAsync(fc => fc.Importo);

            // BILANCI CEE (solo totale annuo)
            var bilanciCEETotale = await _context.BilanciCEE
                .Where(bc => bc.Anno == annoCorrente)
                .SumAsync(bc => bc.Importo);

            // ACCESSI CLIENTI per trimestre
            var accessiClienti = await _context.AccessiClienti
                .Include(ac => ac.ScadenzaFatturazione)
                .Where(ac => ac.ScadenzaFatturazione!.Anno == annoCorrente)
                .ToListAsync();

            var accessiQ1 = accessiClienti.Where(ac => getTrimestre(ac.ScadenzaFatturazione!.DataScadenza) == 1).Sum(ac => ac.TotaleImporto);
            var accessiQ2 = accessiClienti.Where(ac => getTrimestre(ac.ScadenzaFatturazione!.DataScadenza) == 2).Sum(ac => ac.TotaleImporto);
            var accessiQ3 = accessiClienti.Where(ac => getTrimestre(ac.ScadenzaFatturazione!.DataScadenza) == 3).Sum(ac => ac.TotaleImporto);
            var accessiQ4 = accessiClienti.Where(ac => getTrimestre(ac.ScadenzaFatturazione!.DataScadenza) == 4).Sum(ac => ac.TotaleImporto);
            var accessiTotale = accessiQ1 + accessiQ2 + accessiQ3 + accessiQ4;

            // TOTALE COMPLESSIVO
            var totaleComplessivoAnnuo = mandatiTotale + rimborsiTotale + spesePraticheTotale + fattureCloudTotale + bilanciCEETotale + accessiTotale;

            // ViewBag per riepilogo economico
            ViewBag.MandatiQ1 = mandatiQ1;
            ViewBag.MandatiQ2 = mandatiQ2;
            ViewBag.MandatiQ3 = mandatiQ3;
            ViewBag.MandatiQ4 = mandatiQ4;
            ViewBag.MandatiTotale = mandatiTotale;

            ViewBag.RimborsiQ1 = rimborsiQ1;
            ViewBag.RimborsiQ2 = rimborsiQ2;
            ViewBag.RimborsiQ3 = rimborsiQ3;
            ViewBag.RimborsiQ4 = rimborsiQ4;
            ViewBag.RimborsiTotale = rimborsiTotale;

            ViewBag.SpesePraticheQ1 = spesePraticheQ1;
            ViewBag.SpesePraticheQ2 = spesePraticheQ2;
            ViewBag.SpesePraticheQ3 = spesePraticheQ3;
            ViewBag.SpesePraticheQ4 = spesePraticheQ4;
            ViewBag.SpesePraticheTotale = spesePraticheTotale;

            ViewBag.AccessiQ1 = accessiQ1;
            ViewBag.AccessiQ2 = accessiQ2;
            ViewBag.AccessiQ3 = accessiQ3;
            ViewBag.AccessiQ4 = accessiQ4;
            ViewBag.AccessiTotale = accessiTotale;

            ViewBag.FattureCloudTotale = fattureCloudTotale;
            ViewBag.BilanciCEETotale = bilanciCEETotale;
            ViewBag.TotaleComplessivoAnnuo = totaleComplessivoAnnuo;

            ViewData["Title"] = "Dashboard Fatturazione";
            return View();
        }

        // GET: /Amministrazione/Mandati
        public async Task<IActionResult> Mandati(int? anno)
        {
            var annoCorrente = anno ?? await GetAnnoCorrenteFatturazione();
            
            var mandati = await _context.MandatiClienti
                .Include(m => m.Cliente)
                .Include(m => m.Scadenze)
                .Where(m => m.Anno == annoCorrente)
                .OrderBy(m => m.Cliente!.RagioneSociale)
                .ToListAsync();

            // Clienti per dropdown nuovo mandato
            var clientiSenzaMandato = await _context.Clienti
                .Where(c => c.IsActive && !_context.MandatiClienti.Any(m => m.ClienteId == c.Id && m.Anno == annoCorrente))
                .OrderBy(c => c.RagioneSociale)
                .ToListAsync();

            var anniDisponibili = await _context.MandatiClienti
                .Select(m => m.Anno)
                .Distinct()
                .OrderByDescending(a => a)
                .ToListAsync();
            
            if (!anniDisponibili.Contains(annoCorrente))
                anniDisponibili.Add(annoCorrente);
                anniDisponibili = anniDisponibili.OrderByDescending(a => a).ToList();

            ViewBag.AnnoSelezionato = annoCorrente;
            ViewBag.AnniDisponibili = anniDisponibili;
            ViewBag.ClientiSenzaMandato = clientiSenzaMandato;
            ViewData["Title"] = "Mandati Clienti";

            return View(mandati);
        }

        // GET: /Amministrazione/Scadenze
        public async Task<IActionResult> Scadenze(int? anno, int? clienteId, int? stato, int? mese)
        {
            var annoCorrente = anno ?? await GetAnnoCorrenteFatturazione();
            
            var query = _context.ScadenzeFatturazione
                .Include(s => s.Cliente)
                .Include(s => s.MandatoCliente)
                .Include(s => s.SpesePratiche)
                .Include(s => s.AccessiClienti)
                .Include(s => s.FattureCloud)
                .Include(s => s.BilanciCEE)
                .Include(s => s.Incassi)
                .Where(s => s.Anno == annoCorrente);

            if (clienteId.HasValue)
                query = query.Where(s => s.ClienteId == clienteId.Value);

            if (stato.HasValue)
                query = query.Where(s => (int)s.Stato == stato.Value);

            if (mese.HasValue && mese.Value > 0)
                query = query.Where(s => s.DataScadenza.Month == mese.Value);

            var scadenze = await query
                .OrderBy(s => s.DataScadenza)
                .ThenBy(s => s.Cliente!.RagioneSociale)
                .ToListAsync();

            // Dati per filtri
            var clienti = await _context.Clienti
                .Where(c => c.IsActive)
                .OrderBy(c => c.RagioneSociale)
                .ToListAsync();
            
            // Includi anni da scadenze + anni da AnniFatturazione
            var anniScadenze = await _context.ScadenzeFatturazione
                .Select(s => s.Anno)
                .Distinct()
                .ToListAsync();
            
            var anniFatturazione = await _context.AnniFatturazione
                .Select(a => a.Anno)
                .ToListAsync();
            
            var anniDisponibili = anniScadenze.Union(anniFatturazione).ToList();
            
            if (!anniDisponibili.Contains(annoCorrente))
                anniDisponibili.Add(annoCorrente);
            
            anniDisponibili = anniDisponibili.OrderByDescending(a => a).ToList();

            ViewBag.AnnoSelezionato = annoCorrente;
            ViewBag.AnniDisponibili = anniDisponibili;
            ViewBag.Clienti = clienti;
            ViewBag.ClienteIdSelezionato = clienteId;
            ViewBag.StatoSelezionato = stato;
            ViewBag.MeseSelezionato = mese;
            ViewData["Title"] = "Scadenze Fatturazione";

            return View(scadenze);
        }

        // GET: /Amministrazione/SpesePratiche
        public async Task<IActionResult> SpesePratiche(int? anno, int? clienteId, int? mese)
        {
            var annoCorrente = anno ?? await GetAnnoCorrenteFatturazione();
            
            // Filtra per anno: usa l'anno della scadenza se presente, altrimenti l'anno della data spesa
            var query = _context.SpesePratiche
                .Include(s => s.Cliente)
                .Include(s => s.ScadenzaFatturazione)
                .Include(s => s.Utente)
                .Where(s => (s.ScadenzaFatturazione != null && s.ScadenzaFatturazione.Anno == annoCorrente) 
                         || (s.ScadenzaFatturazione == null && s.Data.Year == annoCorrente));

            if (clienteId.HasValue)
                query = query.Where(s => s.ClienteId == clienteId.Value);

            if (mese.HasValue && mese.Value > 0)
                query = query.Where(s => s.Data.Month == mese.Value);

            var spese = await query
                .OrderByDescending(s => s.Data)
                .ToListAsync();

            var clienti = await _context.Clienti
                .Where(c => c.IsActive)
                .OrderBy(c => c.RagioneSociale)
                .ToListAsync();
            
            // Includi anni da scadenze
            var anniScadenze = await _context.ScadenzeFatturazione
                .Select(s => s.Anno)
                .Distinct()
                .ToListAsync();
            
            // Includi anche anni dalle spese pratiche stesse (anche senza scadenza)
            var anniSpese = await _context.SpesePratiche
                .Select(s => s.ScadenzaFatturazione != null ? s.ScadenzaFatturazione.Anno : s.Data.Year)
                .Distinct()
                .ToListAsync();
            
            // Unisci tutti gli anni
            var anniDisponibili = anniScadenze.Union(anniSpese).ToList();
            
            if (!anniDisponibili.Contains(annoCorrente))
                anniDisponibili.Add(annoCorrente);
            
            ViewBag.AnnoSelezionato = annoCorrente;
            ViewBag.AnniDisponibili = anniDisponibili.OrderByDescending(a => a).ToList();
            ViewBag.Clienti = clienti;
            ViewBag.ClienteIdSelezionato = clienteId;
            ViewBag.MeseSelezionato = mese;
            ViewData["Title"] = "Spese Pratiche";
            
            return View(spese);
        }

        // GET: /Amministrazione/GetScadenzeCliente - API per dropdown scadenze
        [HttpGet]
        public async Task<IActionResult> GetScadenzeCliente(int clienteId, int anno)
        {
            var scadenze = await _context.ScadenzeFatturazione
                .Where(s => s.ClienteId == clienteId && s.Anno == anno && s.Stato == StatoScadenza.Aperta)
                .OrderBy(s => s.DataScadenza)
                .Select(s => new
                {
                    id = s.Id,
                    dataScadenza = s.DataScadenza.ToString("dd/MM/yyyy"),
                    stato = s.Stato.ToString()
                })
                .ToListAsync();

            return Json(scadenze);
        }

        // GET: /Amministrazione/GetDettaglioSpese/{id} - API per dettaglio spese di una scadenza
        [HttpGet]
        public async Task<IActionResult> GetDettaglioSpese(int id)
        {
            var scadenza = await _context.ScadenzeFatturazione
                .Include(s => s.SpesePratiche)
                .Include(s => s.AccessiClienti)
                .Include(s => s.FattureCloud)
                .Include(s => s.BilanciCEE)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (scadenza == null)
            {
                return NotFound();
            }

            var dettaglio = new
            {
                spesePratiche = scadenza.SpesePratiche?.Select(sp => new
                {
                    descrizione = sp.Descrizione,
                    importo = sp.Importo,
                    data = sp.Data.ToString("dd/MM/yyyy")
                }).ToList(),

                accessiClienti = scadenza.AccessiClienti?.Select(ac => new
                {
                    ore = ac.TotaleOre,
                    tariffa = ac.TariffaOraria,
                    importo = ac.TotaleImporto,
                    data = ac.Data.ToString("dd/MM/yyyy")
                }).ToList(),

                fattureCloud = scadenza.FattureCloud?.Select(fc => new
                {
                    importo = fc.Importo
                }).ToList(),

                bilanciCEE = scadenza.BilanciCEE?.Select(b => new
                {
                    importo = b.Importo
                }).ToList()
            };

            return Json(dettaglio);
        }

        // POST: /Amministrazione/CreateSpesaPratica
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSpesaPratica(int clienteId, string? scadenzaFatturazioneId, 
            string descrizione, string importo, DateTime data, int anno, DateTime? dataNuovaScadenza)
        {
            try
            {
                // Parsing importo (gestisce sia virgola che punto)
                var importoDecimal = decimal.Parse(importo.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);

                // Ottieni l'utente corrente
                var username = User.Identity?.Name;
                var utente = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

                // LOGICA DI COLLEGAMENTO SCADENZA
                int? scadenzaFinalId = null;
                bool scadenzaCreataAutomaticamente = false;

                // Se l'utente ha selezionato "NUOVA" → crea nuova scadenza (anche senza mandato)
                if (scadenzaFatturazioneId == "NUOVA" && dataNuovaScadenza.HasValue)
                {
                    var nuovaScadenza = new ScadenzaFatturazione
                    {
                        ClienteId = clienteId,
                        MandatoClienteId = null, // Scadenza libera, senza mandato
                        DataScadenza = dataNuovaScadenza.Value,
                        Anno = anno,
                        Stato = StatoScadenza.Aperta,
                        ImportoMandato = 0, // L'importo viene dalle spese/ft cloud/bilanci
                        RimborsoSpese = 0,
                        Note = $"Scadenza creata per: {descrizione}",
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };

                    _context.ScadenzeFatturazione.Add(nuovaScadenza);
                    await _context.SaveChangesAsync();

                    scadenzaFinalId = nuovaScadenza.Id;
                    scadenzaCreataAutomaticamente = true;
                }
                else if (!string.IsNullOrEmpty(scadenzaFatturazioneId) && int.TryParse(scadenzaFatturazioneId, out int scadenzaId) && scadenzaId > 0)
                {
                    // Scadenza selezionata manualmente → verifica che sia aperta
                    var scadenza = await _context.ScadenzeFatturazione.FindAsync(scadenzaId);
                    if (scadenza == null || scadenza.Stato != StatoScadenza.Aperta)
                    {
                        TempData["Error"] = "La scadenza selezionata non è disponibile o non è aperta.";
                        return RedirectToAction(nameof(SpesePratiche), new { anno });
                    }
                    scadenzaFinalId = scadenzaId;
                }
                // Se nessuna scadenza selezionata → la spesa resta senza scadenza (verrà mostrata comunque)

                var spesa = new SpesaPratica
                {
                    ClienteId = clienteId,
                    ScadenzaFatturazioneId = scadenzaFinalId,
                    Descrizione = descrizione,
                    Importo = importoDecimal,
                    Data = data,
                    UtenteId = utente?.Id,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.SpesePratiche.Add(spesa);
                await _context.SaveChangesAsync();

                if (scadenzaCreataAutomaticamente)
                {
                    TempData["Success"] = "Spesa creata e collegata a nuova scadenza automatica in Fatturazione.";
                }
                else if (scadenzaFinalId.HasValue)
                {
                    TempData["Success"] = "Spesa creata e collegata a scadenza esistente in Fatturazione.";
                }
                else
                {
                    TempData["Warning"] = "Spesa creata ma non collegata (nessun mandato attivo trovato per il cliente).";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore nella creazione: {ex.Message}";
            }

            return RedirectToAction(nameof(SpesePratiche), new { anno });
        }

        // POST: /Amministrazione/UpdateSpesaPratica
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSpesaPratica(int id, string? scadenzaFatturazioneId, 
            string descrizione, string importo, DateTime data, int anno, DateTime? dataNuovaScadenza)
        {
            try
            {
                var spesa = await _context.SpesePratiche
                    .Include(s => s.ScadenzaFatturazione)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (spesa == null)
                {
                    TempData["Error"] = "Spesa non trovata.";
                    return RedirectToAction(nameof(SpesePratiche), new { anno });
                }

                // Le spese pratiche sono SEMPRE modificabili
                int? scadenzaFinalId = null;

                // Se l'utente ha selezionato "NUOVA" → crea nuova scadenza (anche senza mandato)
                if (scadenzaFatturazioneId == "NUOVA" && dataNuovaScadenza.HasValue)
                {
                    var nuovaScadenza = new ScadenzaFatturazione
                    {
                        ClienteId = spesa.ClienteId,
                        MandatoClienteId = null, // Scadenza libera
                        DataScadenza = dataNuovaScadenza.Value,
                        Anno = anno,
                        Stato = StatoScadenza.Aperta,
                        ImportoMandato = 0,
                        RimborsoSpese = 0,
                        Note = $"Scadenza creata per: {descrizione}",
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };

                    _context.ScadenzeFatturazione.Add(nuovaScadenza);
                    await _context.SaveChangesAsync();

                    scadenzaFinalId = nuovaScadenza.Id;
                }
                else if (!string.IsNullOrEmpty(scadenzaFatturazioneId) && int.TryParse(scadenzaFatturazioneId, out int scadenzaId) && scadenzaId > 0)
                {
                    var nuovaScadenza = await _context.ScadenzeFatturazione.FindAsync(scadenzaId);
                    if (nuovaScadenza == null || nuovaScadenza.Stato != StatoScadenza.Aperta)
                    {
                        TempData["Error"] = "La scadenza selezionata non è disponibile.";
                        return RedirectToAction(nameof(SpesePratiche), new { anno });
                    }
                    scadenzaFinalId = scadenzaId;
                }

                var importoDecimal = decimal.Parse(importo.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);

                spesa.ScadenzaFatturazioneId = scadenzaFinalId;
                spesa.Descrizione = descrizione;
                spesa.Importo = importoDecimal;
                spesa.Data = data;
                spesa.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                TempData["Success"] = "Spesa pratica aggiornata con successo.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore nell'aggiornamento: {ex.Message}";
            }

            return RedirectToAction(nameof(SpesePratiche), new { anno });
        }

        // POST: /Amministrazione/DeleteSpesaPratica
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSpesaPratica(int id, int anno)
        {
            try
            {
                var spesa = await _context.SpesePratiche
                    .Include(s => s.ScadenzaFatturazione)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (spesa == null)
                {
                    TempData["Error"] = "Spesa non trovata.";
                    return RedirectToAction(nameof(SpesePratiche), new { anno });
                }

                // Verifica se la spesa è collegata a una scadenza già fatturata o incassata
                if (spesa.ScadenzaFatturazione != null)
                {
                    if (spesa.ScadenzaFatturazione.Stato == StatoScadenza.Proforma)
                    {
                        TempData["Error"] = "Impossibile eliminare: la spesa è in una scadenza in stato Proforma.";
                        return RedirectToAction(nameof(SpesePratiche), new { anno });
                    }
                    if (spesa.ScadenzaFatturazione.Stato == StatoScadenza.Fatturata)
                    {
                        TempData["Error"] = "Impossibile eliminare: la spesa è già stata fatturata.";
                        return RedirectToAction(nameof(SpesePratiche), new { anno });
                    }
                    if (spesa.ScadenzaFatturazione.StatoIncasso != StatoIncasso.DaIncassare)
                    {
                        TempData["Error"] = "Impossibile eliminare: la fattura collegata è già stata incassata (totalmente o parzialmente).";
                        return RedirectToAction(nameof(SpesePratiche), new { anno });
                    }
                }

                // Salva l'id della scadenza prima di eliminare la spesa
                var scadenzaId = spesa.ScadenzaFatturazioneId;

                // La spesa è cancellabile solo se la scadenza è ancora Aperta e non incassata
                _context.SpesePratiche.Remove(spesa);
                await _context.SaveChangesAsync();

                // Verifica se la scadenza è rimasta vuota (nessun mandato, nessun accesso, nessun'altra spesa)
                if (scadenzaId.HasValue)
                {
                    var scadenza = await _context.ScadenzeFatturazione
                        .Include(s => s.AccessiClienti)
                        .Include(s => s.SpesePratiche)
                        .FirstOrDefaultAsync(s => s.Id == scadenzaId.Value);

                    if (scadenza != null && 
                        !scadenza.MandatoClienteId.HasValue &&  // Non ha mandato associato
                        (scadenza.AccessiClienti == null || !scadenza.AccessiClienti.Any()) &&  // Nessun accesso
                        (scadenza.SpesePratiche == null || !scadenza.SpesePratiche.Any()) &&  // Nessuna spesa
                        scadenza.Stato == StatoScadenza.Aperta)  // Ancora aperta
                    {
                        _context.ScadenzeFatturazione.Remove(scadenza);
                        await _context.SaveChangesAsync();
                        TempData["Success"] = "Spesa pratica e scadenza vuota eliminate.";
                    }
                    else
                    {
                TempData["Success"] = "Spesa pratica eliminata.";
                    }
                }
                else
                {
                    TempData["Success"] = "Spesa pratica eliminata.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore nell'eliminazione: {ex.Message}";
            }

            return RedirectToAction(nameof(SpesePratiche), new { anno });
        }

        // GET: /Amministrazione/AccessiClienti
        public async Task<IActionResult> AccessiClienti(int? anno, int? clienteId, int? mese)
        {
            var annoCorrente = anno ?? await GetAnnoCorrenteFatturazione();
            
            var query = _context.AccessiClienti
                .Include(a => a.Cliente)
                .Include(a => a.ScadenzaFatturazione)
                .Include(a => a.Utente)
                .Where(a => a.ScadenzaFatturazione!.Anno == annoCorrente);

            if (clienteId.HasValue)
                query = query.Where(a => a.ClienteId == clienteId.Value);

            if (mese.HasValue && mese.Value > 0)
                query = query.Where(a => a.Data.Month == mese.Value);

            var accessi = await query
                .OrderByDescending(a => a.Data)
                .ToListAsync();

            var clienti = await _context.Clienti
                .Where(c => c.IsActive)
                .OrderBy(c => c.RagioneSociale)
                .ToListAsync();
            
            var anniDisponibili = await _context.ScadenzeFatturazione
                .Select(s => s.Anno)
                .Distinct()
                .OrderByDescending(a => a)
                .ToListAsync();
            
            if (!anniDisponibili.Contains(annoCorrente))
                anniDisponibili.Add(annoCorrente);
            
            ViewBag.AnnoSelezionato = annoCorrente;
            ViewBag.AnniDisponibili = anniDisponibili.OrderByDescending(a => a).ToList();
            ViewBag.Clienti = clienti;
            ViewBag.ClienteIdSelezionato = clienteId;
            ViewBag.MeseSelezionato = mese;
            ViewData["Title"] = "Accessi Clienti";

            return View(accessi);
        }

        #region AccessiClienti CRUD

        // POST: /Amministrazione/CreateAccessoCliente
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAccessoCliente(int clienteId, string scadenzaFatturazioneId, 
            DateTime data, string? oraInizioMattina, string? oraFineMattina,
            string? oraInizioPomeriggio, string? oraFinePomeriggio,
            string tariffaOraria, string? note, int anno, DateTime? dataNuovaScadenza)
        {
            try
            {
                int scadenzaId;
                
                // Se è richiesta una nuova scadenza, la creiamo
                if (scadenzaFatturazioneId == "NUOVA")
                {
                    if (!dataNuovaScadenza.HasValue)
                    {
                        TempData["Error"] = "Specificare la data per la nuova scadenza.";
                        return RedirectToAction(nameof(AccessiClienti), new { anno });
                    }
                    
                    var nuovaScadenza = new ScadenzaFatturazione
                    {
                        ClienteId = clienteId,
                        Anno = anno,
                        DataScadenza = dataNuovaScadenza.Value,
                        ImportoMandato = 0, // Verrà calcolato dal totale degli accessi
                        Stato = StatoScadenza.Aperta,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                    
                    _context.ScadenzeFatturazione.Add(nuovaScadenza);
                    await _context.SaveChangesAsync();
                    scadenzaId = nuovaScadenza.Id;
                }
                else
                {
                    scadenzaId = int.Parse(scadenzaFatturazioneId);
                }
                
                var scadenza = await _context.ScadenzeFatturazione.FindAsync(scadenzaId);
                if (scadenza == null || scadenza.Stato != StatoScadenza.Aperta)
                {
                    TempData["Error"] = "La scadenza selezionata non è disponibile.";
                    return RedirectToAction(nameof(AccessiClienti), new { anno });
                }

                var tariffaDecimal = decimal.Parse(tariffaOraria.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);

                var username = User.Identity?.Name;
                var utente = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

                var accesso = new AccessoCliente
                {
                    ClienteId = clienteId,
                    ScadenzaFatturazioneId = scadenzaId,
                    Data = data,
                    OraInizioMattino = string.IsNullOrEmpty(oraInizioMattina) ? null : TimeSpan.Parse(oraInizioMattina),
                    OraFineMattino = string.IsNullOrEmpty(oraFineMattina) ? null : TimeSpan.Parse(oraFineMattina),
                    OraInizioPomeriggio = string.IsNullOrEmpty(oraInizioPomeriggio) ? null : TimeSpan.Parse(oraInizioPomeriggio),
                    OraFinePomeriggio = string.IsNullOrEmpty(oraFinePomeriggio) ? null : TimeSpan.Parse(oraFinePomeriggio),
                    TariffaOraria = tariffaDecimal,
                    Note = note,
                    UtenteId = utente?.Id,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.AccessiClienti.Add(accesso);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Accesso registrato: {accesso.TotaleOre:0.00} ore = {accesso.TotaleImporto:C}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore: {ex.Message}";
            }

            return RedirectToAction(nameof(AccessiClienti), new { anno });
        }

        // POST: /Amministrazione/UpdateAccessoCliente
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAccessoCliente(int id, string scadenzaFatturazioneId, 
            DateTime data, string? oraInizioMattina, string? oraFineMattina,
            string? oraInizioPomeriggio, string? oraFinePomeriggio,
            string tariffaOraria, string? note, int anno, DateTime? dataNuovaScadenza)
        {
            try
            {
                var accesso = await _context.AccessiClienti.FindAsync(id);

                if (accesso == null)
                {
                    TempData["Error"] = "Accesso non trovato.";
                    return RedirectToAction(nameof(AccessiClienti), new { anno });
                }

                // Gli accessi clienti sono SEMPRE modificabili

                int scadenzaId;
                
                // Se è richiesta una nuova scadenza, la creiamo
                if (scadenzaFatturazioneId == "NUOVA")
                {
                    if (!dataNuovaScadenza.HasValue)
                    {
                        TempData["Error"] = "Specificare la data per la nuova scadenza.";
                        return RedirectToAction(nameof(AccessiClienti), new { anno });
                    }
                    
                    var nuovaScadenzaEntity = new ScadenzaFatturazione
                    {
                        ClienteId = accesso.ClienteId,
                        Anno = anno,
                        DataScadenza = dataNuovaScadenza.Value,
                        ImportoMandato = 0,
                        Stato = StatoScadenza.Aperta,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                    
                    _context.ScadenzeFatturazione.Add(nuovaScadenzaEntity);
                    await _context.SaveChangesAsync();
                    scadenzaId = nuovaScadenzaEntity.Id;
                }
                else
                {
                    scadenzaId = int.Parse(scadenzaFatturazioneId);
                }

                var nuovaScadenza = await _context.ScadenzeFatturazione.FindAsync(scadenzaId);
                if (nuovaScadenza == null || nuovaScadenza.Stato != StatoScadenza.Aperta)
                {
                    TempData["Error"] = "La scadenza selezionata non è disponibile.";
                    return RedirectToAction(nameof(AccessiClienti), new { anno });
                }

                accesso.ScadenzaFatturazioneId = scadenzaId;
                accesso.Data = data;
                accesso.OraInizioMattino = string.IsNullOrEmpty(oraInizioMattina) ? null : TimeSpan.Parse(oraInizioMattina);
                accesso.OraFineMattino = string.IsNullOrEmpty(oraFineMattina) ? null : TimeSpan.Parse(oraFineMattina);
                accesso.OraInizioPomeriggio = string.IsNullOrEmpty(oraInizioPomeriggio) ? null : TimeSpan.Parse(oraInizioPomeriggio);
                accesso.OraFinePomeriggio = string.IsNullOrEmpty(oraFinePomeriggio) ? null : TimeSpan.Parse(oraFinePomeriggio);
                accesso.TariffaOraria = decimal.Parse(tariffaOraria.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
                accesso.Note = note;
                accesso.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                TempData["Success"] = $"Accesso aggiornato: {accesso.TotaleOre:0.00} ore = {accesso.TotaleImporto:C}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore: {ex.Message}";
            }

            return RedirectToAction(nameof(AccessiClienti), new { anno });
        }

        // POST: /Amministrazione/DeleteAccessoCliente
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccessoCliente(int id, int anno)
        {
            try
            {
                var accesso = await _context.AccessiClienti
                    .Include(a => a.ScadenzaFatturazione)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (accesso == null)
                {
                    TempData["Error"] = "Accesso non trovato.";
                    return RedirectToAction(nameof(AccessiClienti), new { anno });
                }

                // Verifica se l'accesso è collegato a una scadenza già fatturata o incassata
                if (accesso.ScadenzaFatturazione != null)
                {
                    if (accesso.ScadenzaFatturazione.Stato == StatoScadenza.Proforma)
                    {
                        TempData["Error"] = "Impossibile eliminare: l'accesso è in una scadenza in stato Proforma.";
                        return RedirectToAction(nameof(AccessiClienti), new { anno });
                    }
                    if (accesso.ScadenzaFatturazione.Stato == StatoScadenza.Fatturata)
                    {
                        TempData["Error"] = "Impossibile eliminare: l'accesso è già stato fatturato.";
                        return RedirectToAction(nameof(AccessiClienti), new { anno });
                    }
                    if (accesso.ScadenzaFatturazione.StatoIncasso != StatoIncasso.DaIncassare)
                    {
                        TempData["Error"] = "Impossibile eliminare: la fattura collegata è già stata incassata (totalmente o parzialmente).";
                        return RedirectToAction(nameof(AccessiClienti), new { anno });
                    }
                }

                // Salva l'id della scadenza prima di eliminare l'accesso
                var scadenzaId = accesso.ScadenzaFatturazioneId;

                // L'accesso è cancellabile solo se la scadenza è ancora Aperta e non incassata
                _context.AccessiClienti.Remove(accesso);
                await _context.SaveChangesAsync();

                // Verifica se la scadenza è rimasta vuota (nessun mandato, nessun altro accesso, nessuna spesa)
                var scadenza = await _context.ScadenzeFatturazione
                    .Include(s => s.AccessiClienti)
                    .Include(s => s.SpesePratiche)
                    .FirstOrDefaultAsync(s => s.Id == scadenzaId);

                if (scadenza != null && 
                    !scadenza.MandatoClienteId.HasValue &&  // Non ha mandato associato
                    (scadenza.AccessiClienti == null || !scadenza.AccessiClienti.Any()) &&  // Nessun accesso
                    (scadenza.SpesePratiche == null || !scadenza.SpesePratiche.Any()) &&  // Nessuna spesa
                    scadenza.Stato == StatoScadenza.Aperta)  // Ancora aperta
                {
                    _context.ScadenzeFatturazione.Remove(scadenza);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Accesso e scadenza vuota eliminati.";
                }
                else
                {
                TempData["Success"] = "Accesso eliminato.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore: {ex.Message}";
            }

            return RedirectToAction(nameof(AccessiClienti), new { anno });
        }

        #endregion

        // GET: /Amministrazione/FattureCloud
        public async Task<IActionResult> FattureCloud(int? anno, int? mese)
        {
            var annoCorrente = anno ?? await GetAnnoCorrenteFatturazione();
            
            var query = _context.FattureCloud
                .Include(f => f.Cliente)
                .Include(f => f.ScadenzaFatturazione)
                .Where(f => f.Anno == annoCorrente);

            if (mese.HasValue && mese.Value > 0)
                query = query.Where(f => f.DataScadenza.Month == mese.Value);

            var fattureCloud = await query
                .OrderBy(f => f.Cliente!.RagioneSociale)
                .ToListAsync();

            var clientiSenza = await _context.Clienti
                .Where(c => c.IsActive && !_context.FattureCloud.Any(f => f.ClienteId == c.Id && f.Anno == annoCorrente))
                .OrderBy(c => c.RagioneSociale)
                .ToListAsync();
            
            // Includi anni da FattureCloud + anni da AnniFatturazione
            var anniFattureCloud = await _context.FattureCloud
                .Select(f => f.Anno)
                .Distinct()
                .ToListAsync();
            
            var anniFatturazione = await _context.AnniFatturazione
                .Select(a => a.Anno)
                .ToListAsync();
            
            var anniDisponibili = anniFattureCloud.Union(anniFatturazione).ToList();
            
            if (!anniDisponibili.Contains(annoCorrente))
                anniDisponibili.Add(annoCorrente);
            
            ViewBag.AnnoSelezionato = annoCorrente;
            ViewBag.AnniDisponibili = anniDisponibili.OrderByDescending(a => a).ToList();
            ViewBag.ClientiSenza = clientiSenza;
            ViewBag.MeseSelezionato = mese;
            ViewData["Title"] = "Fatture in Cloud";
            
            return View(fattureCloud);
        }

        // GET: /Amministrazione/BilanciCEE
        public async Task<IActionResult> BilanciCEE(int? anno, int? mese)
        {
            var annoCorrente = anno ?? await GetAnnoCorrenteFatturazione();
            
            var query = _context.BilanciCEE
                .Include(b => b.Cliente)
                .Include(b => b.ScadenzaFatturazione)
                .Where(b => b.Anno == annoCorrente);

            if (mese.HasValue && mese.Value > 0)
                query = query.Where(b => b.DataScadenza.Month == mese.Value);

            var bilanci = await query
                .OrderBy(b => b.Cliente!.RagioneSociale)
                .ToListAsync();

            // Solo clienti SC senza bilancio per quest'anno
            var clientiSC = await _context.Clienti
                .Where(c => c.IsActive && c.TipoSoggetto == "SC" && !_context.BilanciCEE.Any(b => b.ClienteId == c.Id && b.Anno == annoCorrente))
                .OrderBy(c => c.RagioneSociale)
                .ToListAsync();
            
            // Includi anni da BilanciCEE + anni da AnniFatturazione
            var anniBilanci = await _context.BilanciCEE
                .Select(b => b.Anno)
                .Distinct()
                .ToListAsync();
            
            var anniFatturazione = await _context.AnniFatturazione
                .Select(a => a.Anno)
                .ToListAsync();
            
            var anniDisponibili = anniBilanci.Union(anniFatturazione).ToList();
            
            if (!anniDisponibili.Contains(annoCorrente))
                anniDisponibili.Add(annoCorrente);
            
            ViewBag.AnnoSelezionato = annoCorrente;
            ViewBag.AnniDisponibili = anniDisponibili.OrderByDescending(a => a).ToList();
            ViewBag.ClientiSC = clientiSC;
            ViewBag.MeseSelezionato = mese;
            ViewData["Title"] = "Bilanci CEE";

            return View(bilanci);
        }

        #region FattureCloud CRUD

        // POST: /Amministrazione/CreateFatturaCloud
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFatturaCloud(int clienteId, int? scadenzaFatturazioneId, string importo, DateTime dataScadenza, int anno)
        {
            try
            {
                var importoDecimal = decimal.Parse(importo.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);

                // LOGICA AUTOMATICA: cerca o crea scadenza
                int? scadenzaFinalId = scadenzaFatturazioneId;
                bool scadenzaCreataAutomaticamente = false;

                if (!scadenzaFinalId.HasValue || scadenzaFinalId.Value == 0)
                {
                    // 1. Cerca scadenza esistente APERTA con la stessa data
                    var scadenzaEsistente = await _context.ScadenzeFatturazione
                        .Where(s => s.ClienteId == clienteId 
                                 && s.DataScadenza.Date == dataScadenza.Date 
                                 && s.Stato == StatoScadenza.Aperta
                                 && s.Anno == anno)
                        .OrderByDescending(s => s.Id)
                        .FirstOrDefaultAsync();

                    if (scadenzaEsistente != null)
                    {
                        scadenzaFinalId = scadenzaEsistente.Id;
                    }
                    else
                    {
                        // 2. Crea nuova scadenza automaticamente (anche senza mandato)
                        // Cerca mandato se esiste, altrimenti crea scadenza libera
                        var mandato = await _context.MandatiClienti
                            .Where(m => m.ClienteId == clienteId && m.Anno == anno && m.IsActive)
                            .FirstOrDefaultAsync();

                        var nuovaScadenza = new ScadenzaFatturazione
                        {
                            ClienteId = clienteId,
                            MandatoClienteId = mandato?.Id, // null se non c'è mandato
                            DataScadenza = dataScadenza,
                            Anno = anno,
                            Stato = StatoScadenza.Aperta,
                            ImportoMandato = 0,
                            RimborsoSpese = 0,
                            Note = "Scadenza creata automaticamente per Fattura in Cloud",
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        };

                        _context.ScadenzeFatturazione.Add(nuovaScadenza);
                        await _context.SaveChangesAsync();

                        scadenzaFinalId = nuovaScadenza.Id;
                        scadenzaCreataAutomaticamente = true;
                    }
                }

                var fc = new FatturaCloud
                {
                    ClienteId = clienteId,
                    Anno = anno,
                    ScadenzaFatturazioneId = scadenzaFinalId,
                    DataScadenza = dataScadenza,
                    Importo = importoDecimal,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.FattureCloud.Add(fc);
                await _context.SaveChangesAsync();

                if (scadenzaCreataAutomaticamente)
                {
                    TempData["Success"] = "Fattura in Cloud aggiunta e collegata a nuova scadenza automatica.";
                }
                else if (scadenzaFinalId.HasValue)
                {
                    TempData["Success"] = "Fattura in Cloud aggiunta e collegata a scadenza esistente.";
                }
                else
                {
                    TempData["Warning"] = "Fattura in Cloud aggiunta ma non collegata (nessun mandato attivo).";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore: {ex.Message}";
            }

            return RedirectToAction(nameof(FattureCloud), new { anno });
        }

        // POST: /Amministrazione/UpdateFatturaCloud
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateFatturaCloud(int id, string importo, int anno)
        {
            try
            {
                var fc = await _context.FattureCloud.FindAsync(id);

                if (fc == null)
                {
                    TempData["Error"] = "Record non trovato.";
                    return RedirectToAction(nameof(FattureCloud), new { anno });
                }

                // Fatture Cloud sono SEMPRE modificabili

                fc.Importo = decimal.Parse(importo.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
                fc.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                TempData["Success"] = "Importo aggiornato con successo.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore: {ex.Message}";
            }

            return RedirectToAction(nameof(FattureCloud), new { anno });
        }

        // POST: /Amministrazione/DeleteFatturaCloud
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFatturaCloud(int id, int anno)
        {
            try
            {
                var fc = await _context.FattureCloud
                    .Include(f => f.ScadenzaFatturazione)
                    .FirstOrDefaultAsync(f => f.Id == id);

                if (fc == null)
                {
                    TempData["Error"] = "Record non trovato.";
                    return RedirectToAction(nameof(FattureCloud), new { anno });
                }

                // Verifica se la FT Cloud è collegata a una scadenza già fatturata o incassata
                if (fc.ScadenzaFatturazione != null)
                {
                    if (fc.ScadenzaFatturazione.Stato == StatoScadenza.Proforma)
                    {
                        TempData["Error"] = "Impossibile eliminare: la FT Cloud è in una scadenza in stato Proforma.";
                        return RedirectToAction(nameof(FattureCloud), new { anno });
                    }
                    if (fc.ScadenzaFatturazione.Stato == StatoScadenza.Fatturata)
                    {
                        TempData["Error"] = "Impossibile eliminare: la FT Cloud è già stata fatturata.";
                        return RedirectToAction(nameof(FattureCloud), new { anno });
                    }
                    if (fc.ScadenzaFatturazione.StatoIncasso != StatoIncasso.DaIncassare)
                    {
                        TempData["Error"] = "Impossibile eliminare: la fattura collegata è già stata incassata (totalmente o parzialmente).";
                        return RedirectToAction(nameof(FattureCloud), new { anno });
                    }
                }

                // La FT Cloud è cancellabile solo se la scadenza è ancora Aperta e non incassata
                _context.FattureCloud.Remove(fc);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Fattura in Cloud rimossa.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore: {ex.Message}";
            }

            return RedirectToAction(nameof(FattureCloud), new { anno });
        }

        #endregion

        #region BilanciCEE CRUD

        // POST: /Amministrazione/CreateBilancioCEE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBilancioCEE(int clienteId, int? scadenzaFatturazioneId, string importo, DateTime dataScadenza, int anno)
        {
            try
            {
                var importoDecimal = decimal.Parse(importo.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);

                // LOGICA AUTOMATICA: cerca o crea scadenza
                int? scadenzaFinalId = scadenzaFatturazioneId;
                bool scadenzaCreataAutomaticamente = false;

                if (!scadenzaFinalId.HasValue || scadenzaFinalId.Value == 0)
                {
                    // 1. Cerca scadenza esistente APERTA con la stessa data
                    var scadenzaEsistente = await _context.ScadenzeFatturazione
                        .Where(s => s.ClienteId == clienteId 
                                 && s.DataScadenza.Date == dataScadenza.Date 
                                 && s.Stato == StatoScadenza.Aperta
                                 && s.Anno == anno)
                        .OrderByDescending(s => s.Id)
                        .FirstOrDefaultAsync();

                    if (scadenzaEsistente != null)
                    {
                        scadenzaFinalId = scadenzaEsistente.Id;
                    }
                    else
                    {
                        // 2. Crea nuova scadenza automaticamente (anche senza mandato)
                        var mandato = await _context.MandatiClienti
                            .Where(m => m.ClienteId == clienteId && m.Anno == anno && m.IsActive)
                            .FirstOrDefaultAsync();

                        var nuovaScadenza = new ScadenzaFatturazione
                        {
                            ClienteId = clienteId,
                            MandatoClienteId = mandato?.Id, // null se non c'è mandato
                            DataScadenza = dataScadenza,
                            Anno = anno,
                            Stato = StatoScadenza.Aperta,
                            ImportoMandato = 0,
                            RimborsoSpese = 0,
                            Note = "Scadenza creata automaticamente per Bilancio CEE",
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        };

                        _context.ScadenzeFatturazione.Add(nuovaScadenza);
                        await _context.SaveChangesAsync();

                        scadenzaFinalId = nuovaScadenza.Id;
                        scadenzaCreataAutomaticamente = true;
                    }
                }

                var b = new BilancioCEE
                {
                    ClienteId = clienteId,
                    Anno = anno,
                    ScadenzaFatturazioneId = scadenzaFinalId,
                    DataScadenza = dataScadenza,
                    Importo = importoDecimal,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.BilanciCEE.Add(b);
                await _context.SaveChangesAsync();

                if (scadenzaCreataAutomaticamente)
                {
                    TempData["Success"] = "Bilancio CEE aggiunto e collegato a nuova scadenza automatica.";
                }
                else if (scadenzaFinalId.HasValue)
                {
                    TempData["Success"] = "Bilancio CEE aggiunto e collegato a scadenza esistente.";
                }
                else
                {
                    TempData["Warning"] = "Bilancio CEE aggiunto ma non collegato (nessun mandato attivo).";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore: {ex.Message}";
            }

            return RedirectToAction(nameof(BilanciCEE), new { anno });
        }

        // POST: /Amministrazione/UpdateBilancioCEE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBilancioCEE(int id, string importo, int anno)
        {
            try
            {
                var b = await _context.BilanciCEE.FindAsync(id);

                if (b == null)
                {
                    TempData["Error"] = "Record non trovato.";
                    return RedirectToAction(nameof(BilanciCEE), new { anno });
                }

                // Bilanci CEE sono SEMPRE modificabili

                b.Importo = decimal.Parse(importo.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
                b.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                TempData["Success"] = "Importo aggiornato con successo.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore: {ex.Message}";
            }

            return RedirectToAction(nameof(BilanciCEE), new { anno });
        }

        // POST: /Amministrazione/DeleteBilancioCEE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBilancioCEE(int id, int anno)
        {
            try
            {
                var b = await _context.BilanciCEE
                    .Include(x => x.ScadenzaFatturazione)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (b == null)
                {
                    TempData["Error"] = "Record non trovato.";
                    return RedirectToAction(nameof(BilanciCEE), new { anno });
                }

                // Verifica se il Bilancio è collegato a una scadenza già fatturata o incassata
                if (b.ScadenzaFatturazione != null)
                {
                    if (b.ScadenzaFatturazione.Stato == StatoScadenza.Proforma)
                    {
                        TempData["Error"] = "Impossibile eliminare: il Bilancio CEE è in una scadenza in stato Proforma.";
                        return RedirectToAction(nameof(BilanciCEE), new { anno });
                    }
                    if (b.ScadenzaFatturazione.Stato == StatoScadenza.Fatturata)
                    {
                        TempData["Error"] = "Impossibile eliminare: il Bilancio CEE è già stato fatturato.";
                        return RedirectToAction(nameof(BilanciCEE), new { anno });
                    }
                    if (b.ScadenzaFatturazione.StatoIncasso != StatoIncasso.DaIncassare)
                    {
                        TempData["Error"] = "Impossibile eliminare: la fattura collegata è già stata incassata (totalmente o parzialmente).";
                        return RedirectToAction(nameof(BilanciCEE), new { anno });
                    }
                }

                // Il Bilancio CEE è cancellabile solo se la scadenza è ancora Aperta e non incassata
                _context.BilanciCEE.Remove(b);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Bilancio CEE rimosso.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore: {ex.Message}";
            }

            return RedirectToAction(nameof(BilanciCEE), new { anno });
        }

        #endregion

        // GET: /Amministrazione/Incassi
        public async Task<IActionResult> Incassi(int? anno, int? stato, int? mese)
        {
            var annoCorrente = anno ?? await GetAnnoCorrenteFatturazione();
            
            var query = _context.ScadenzeFatturazione
                .Include(s => s.Cliente)
                .Include(s => s.Incassi)
                    .ThenInclude(i => i.SuddivisioneProfessionisti)
                        .ThenInclude(p => p.Utente)
                .Include(s => s.SpesePratiche)
                .Include(s => s.FattureCloud)
                .Include(s => s.BilanciCEE)
                .Include(s => s.AccessiClienti)
                .Where(s => s.Anno == annoCorrente && s.NumeroFattura.HasValue);
            
            if (stato.HasValue)
                query = query.Where(s => (int)s.StatoIncasso == stato.Value);
            
            if (mese.HasValue)
                query = query.Where(s => s.DataFattura!.Value.Month == mese.Value);

            var fatture = await query
                .OrderByDescending(s => s.DataFattura)
                .ThenBy(s => s.Cliente!.RagioneSociale)
                .ToListAsync();

            // Statistiche
            var totDaIncassare = fatture.Where(f => f.StatoIncasso == StatoIncasso.DaIncassare).Sum(f => f.TotaleScadenza);
            var totParziale = fatture.Where(f => f.StatoIncasso == StatoIncasso.ParzialmenteIncassata).Sum(f => f.ResiduoDaIncassare);
            var totIncassato = fatture.Sum(f => f.TotaleIncassato);
            
            var anniDisponibili = await _context.ScadenzeFatturazione
                .Where(s => s.NumeroFattura.HasValue)
                .Select(s => s.Anno)
                .Distinct()
                .OrderByDescending(a => a)
                .ToListAsync();
            
            if (!anniDisponibili.Contains(annoCorrente))
                anniDisponibili.Add(annoCorrente);

            // Professionisti per suddivisione incasso
            var professionisti = await _context.Users
                .Where(u => u.IsActive)
                .OrderBy(u => u.Cognome)
                .ThenBy(u => u.Nome)
                .ToListAsync();

            ViewBag.AnnoSelezionato = annoCorrente;
            ViewBag.AnniDisponibili = anniDisponibili.OrderByDescending(a => a).ToList();
            ViewBag.StatoSelezionato = stato;
            ViewBag.MeseSelezionato = mese;
            ViewBag.TotDaIncassare = totDaIncassare;
            ViewBag.TotParziale = totParziale;
            ViewBag.TotIncassato = totIncassato;
            ViewBag.Professionisti = professionisti;
            ViewData["Title"] = "Incassi";

            return View(fatture);
        }

        // POST: /Amministrazione/RegistraIncasso
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistraIncasso(int scadenzaId, DateTime dataIncasso, 
            string importo, string? note, int anno)
        {
            try
            {
                // Validazione data incasso obbligatoria
                if (dataIncasso == default || dataIncasso == DateTime.MinValue)
                {
                    TempData["Error"] = "La data incasso è obbligatoria.";
                    return RedirectToAction(nameof(Incassi), new { anno });
                }

                var scadenza = await _context.ScadenzeFatturazione
                    .Include(s => s.Incassi)
                    .Include(s => s.SpesePratiche)
                    .Include(s => s.FattureCloud)
                    .Include(s => s.BilanciCEE)
                    .Include(s => s.AccessiClienti)
                    .FirstOrDefaultAsync(s => s.Id == scadenzaId);

                if (scadenza == null)
                {
                    TempData["Error"] = "Fattura non trovata.";
                    return RedirectToAction(nameof(Incassi), new { anno });
                }

                if (scadenza.StatoIncasso == StatoIncasso.Incassata)
                {
                    TempData["Error"] = "La fattura è già completamente incassata.";
                    return RedirectToAction(nameof(Incassi), new { anno });
                }

                var importoDecimal = decimal.Parse(importo.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);

                if (importoDecimal <= 0)
                {
                    TempData["Error"] = "L'importo deve essere maggiore di zero.";
                    return RedirectToAction(nameof(Incassi), new { anno });
                }

                if (importoDecimal > scadenza.ResiduoDaIncassare)
                {
                    TempData["Error"] = $"L'importo non può superare il residuo ({scadenza.ResiduoDaIncassare:C}).";
                    return RedirectToAction(nameof(Incassi), new { anno });
                }

                // Crea incasso
                var incasso = new IncassoFattura
                {
                    ScadenzaFatturazioneId = scadenzaId,
                    DataIncasso = dataIncasso,
                    ImportoIncassato = importoDecimal,
                    Note = note,
                    CreatedAt = DateTime.Now
                };

                _context.IncassiFatture.Add(incasso);
                await _context.SaveChangesAsync();

                // Suddivisione tra professionisti - leggi dal form
                var professionistiIds = await _context.Users.Where(u => u.IsActive).Select(u => u.Id).ToListAsync();
                foreach (var profId in professionistiIds)
                {
                    var selezionatoKey = $"professionisti[{profId}].selezionato";
                    var importoKey = $"professionisti[{profId}].importo";
                    
                    var isSelezionato = Request.Form.ContainsKey(selezionatoKey);
                    var importoProfStr = Request.Form[importoKey].FirstOrDefault();
                    
                    if (isSelezionato && !string.IsNullOrEmpty(importoProfStr))
                    {
                        var importoProfDecimal = decimal.Parse(importoProfStr.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
                        if (importoProfDecimal > 0)
                        {
                            var suddivisione = new IncassoProfessionista
                            {
                                IncassoFatturaId = incasso.Id,
                                UtenteId = profId,
                                Importo = importoProfDecimal,
                                CreatedAt = DateTime.Now
                            };
                            _context.IncassiProfessionisti.Add(suddivisione);
                        }
                    }
                }
                await _context.SaveChangesAsync();

                // Aggiorna stato incasso della scadenza
                var totaleIncassato = scadenza.Incassi!.Sum(i => i.ImportoIncassato) + importoDecimal;
                if (totaleIncassato >= scadenza.TotaleScadenza)
                {
                    scadenza.StatoIncasso = StatoIncasso.Incassata;
                }
                else
                {
                    scadenza.StatoIncasso = StatoIncasso.ParzialmenteIncassata;
                }
                scadenza.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                TempData["Success"] = $"Incasso di {importoDecimal:C} registrato con successo.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore: {ex.Message}";
            }

            return RedirectToAction(nameof(Incassi), new { anno });
        }

        // POST: /Amministrazione/AnnullaIncassi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AnnullaIncassi(int scadenzaId, int anno)
        {
            try
            {
                var scadenza = await _context.ScadenzeFatturazione
                    .Include(s => s.Incassi)!
                        .ThenInclude(i => i.SuddivisioneProfessionisti)
                    .Include(s => s.Cliente)
                    .FirstOrDefaultAsync(s => s.Id == scadenzaId);

                if (scadenza == null)
                {
                    TempData["Error"] = "Fattura non trovata.";
                    return RedirectToAction(nameof(Incassi), new { anno });
                }

                if (scadenza.Incassi == null || !scadenza.Incassi.Any())
                {
                    TempData["Error"] = "Nessun incasso da annullare.";
                    return RedirectToAction(nameof(Incassi), new { anno });
                }

                var totaleAnnullato = scadenza.Incassi.Sum(i => i.ImportoIncassato);
                var numeroIncassi = scadenza.Incassi.Count;

                // Elimina prima le suddivisioni professionisti
                foreach (var incasso in scadenza.Incassi)
                {
                    if (incasso.SuddivisioneProfessionisti?.Any() == true)
                    {
                        _context.IncassiProfessionisti.RemoveRange(incasso.SuddivisioneProfessionisti);
                    }
                }
                await _context.SaveChangesAsync();

                // Elimina gli incassi
                _context.IncassiFatture.RemoveRange(scadenza.Incassi);

                // Reimposta stato a "Da Incassare"
                scadenza.StatoIncasso = StatoIncasso.DaIncassare;
                scadenza.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["Success"] = $"Annullati {numeroIncassi} incasso/i per un totale di {totaleAnnullato:C} " +
                                     $"dalla fattura {scadenza.NumeroFatturaFormattato} di <strong>{scadenza.Cliente?.RagioneSociale}</strong>";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore nell'annullamento: {ex.Message}";
            }

            return RedirectToAction(nameof(Incassi), new { anno });
        }

        // GET: /Amministrazione/ReportProfessionisti
        public async Task<IActionResult> ReportProfessionisti(int? anno, int? mese, bool progressivo = false)
        {
            var annoCorrente = anno ?? await GetAnnoCorrenteFatturazione();
            var meseCorrente = mese ?? DateTime.Now.Month;

            // Query incassi
            var queryIncassi = _context.IncassiProfessionisti
                .Include(ip => ip.IncassoFattura)
                    .ThenInclude(i => i!.ScadenzaFatturazione)
                .Include(ip => ip.Utente)
                .Where(ip => ip.IncassoFattura!.ScadenzaFatturazione!.Anno == annoCorrente);

            if (progressivo)
            {
                // Da gennaio al mese selezionato
                queryIncassi = queryIncassi.Where(ip => ip.IncassoFattura!.DataIncasso.Month <= meseCorrente);
            }
            else
            {
                // Solo mese selezionato
                queryIncassi = queryIncassi.Where(ip => ip.IncassoFattura!.DataIncasso.Month == meseCorrente);
            }
            
            var incassiProfessionisti = await queryIncassi.ToListAsync();
            
            // Raggruppa per professionista
            var reportData = incassiProfessionisti
                .GroupBy(ip => new { ip.UtenteId, ip.Utente?.Nome, ip.Utente?.Cognome })
                .Select(g => new {
                    UtenteId = g.Key.UtenteId,
                    NomeCompleto = $"{g.Key.Nome} {g.Key.Cognome}",
                    TotaleIncassato = g.Sum(ip => ip.Importo)
                })
                .OrderByDescending(r => r.TotaleIncassato)
                .ToList();
            
            // Lista professionisti (utenti attivi)
            var professionisti = await _context.Users
                .Where(u => u.IsActive)
                .OrderBy(u => u.Cognome)
                .ThenBy(u => u.Nome)
                .ToListAsync();

            var anniDisponibili = await _context.ScadenzeFatturazione
                .Where(s => s.NumeroFattura.HasValue)
                .Select(s => s.Anno)
                .Distinct()
                .OrderByDescending(a => a)
                .ToListAsync();
            
            if (!anniDisponibili.Contains(annoCorrente))
                anniDisponibili.Add(annoCorrente);

            // ==========================================
            // GRIGLIA MENSILE PER PROFESSIONISTA
            // ==========================================
            var tuttiIncassiAnno = await _context.IncassiProfessionisti
                .Include(ip => ip.IncassoFattura)
                .Include(ip => ip.Utente)
                .Where(ip => ip.IncassoFattura!.ScadenzaFatturazione!.Anno == annoCorrente)
                .ToListAsync();

            // Crea matrice: Professionista -> Mese -> Importo
            var gridMensile = professionisti.Select(p => new {
                Professionista = $"{p.Nome} {p.Cognome}",
                ProfessionistaId = p.Id,
                Mesi = Enumerable.Range(1, 12).Select(m => 
                    tuttiIncassiAnno
                        .Where(i => i.UtenteId == p.Id && i.IncassoFattura!.DataIncasso.Month == m)
                        .Sum(i => i.Importo)
                ).ToList(),
                Totale = tuttiIncassiAnno.Where(i => i.UtenteId == p.Id).Sum(i => i.Importo)
            }).Where(p => p.Totale > 0 || professionisti.Any()).ToList();

            // Totali per mese
            var totaliMensili = Enumerable.Range(1, 12).Select(m =>
                tuttiIncassiAnno.Where(i => i.IncassoFattura!.DataIncasso.Month == m).Sum(i => i.Importo)
            ).ToList();

            ViewBag.AnnoSelezionato = annoCorrente;
            ViewBag.MeseSelezionato = meseCorrente;
            ViewBag.Progressivo = progressivo;
            ViewBag.AnniDisponibili = anniDisponibili.OrderByDescending(a => a).ToList();
            ViewBag.ReportData = reportData;
            ViewBag.Professionisti = professionisti;
            ViewBag.TotaleGenerale = reportData.Sum(r => r.TotaleIncassato);
            ViewBag.GridMensile = gridMensile;
            ViewBag.TotaliMensili = totaliMensili;
            ViewBag.TotaleAnnuo = tuttiIncassiAnno.Sum(i => i.Importo);
            ViewData["Title"] = "Report Professionisti";
            
            return View();
        }

        // GET: /Amministrazione/ExportReportProfessionisti
        public async Task<IActionResult> ExportReportProfessionisti(int? anno, int? mese, bool progressivo = false)
        {
            var annoCorrente = anno ?? await GetAnnoCorrenteFatturazione();
            var meseCorrente = mese ?? DateTime.Now.Month;
            var mesi = new[] { "", "Gennaio", "Febbraio", "Marzo", "Aprile", "Maggio", "Giugno", 
                              "Luglio", "Agosto", "Settembre", "Ottobre", "Novembre", "Dicembre" };

            // Query incassi per periodo selezionato
            var queryIncassi = _context.IncassiProfessionisti
                .Include(ip => ip.IncassoFattura)
                    .ThenInclude(i => i!.ScadenzaFatturazione)
                .Include(ip => ip.Utente)
                .Where(ip => ip.IncassoFattura!.ScadenzaFatturazione!.Anno == annoCorrente);

            if (progressivo)
            {
                queryIncassi = queryIncassi.Where(ip => ip.IncassoFattura!.DataIncasso.Month <= meseCorrente);
            }
            else
            {
                queryIncassi = queryIncassi.Where(ip => ip.IncassoFattura!.DataIncasso.Month == meseCorrente);
            }
            
            var incassiProfessionisti = await queryIncassi.ToListAsync();
            
            // Report per professionista
            var reportData = incassiProfessionisti
                .GroupBy(ip => new { ip.UtenteId, ip.Utente?.Nome, ip.Utente?.Cognome })
                .Select(g => new {
                    NomeCompleto = $"{g.Key.Nome} {g.Key.Cognome}",
                    TotaleIncassato = g.Sum(ip => ip.Importo)
                })
                .OrderByDescending(r => r.TotaleIncassato)
                .ToList();
            
            var totaleGenerale = reportData.Sum(r => r.TotaleIncassato);

            // Professionisti
            var professionisti = await _context.Users
                .Where(u => u.IsActive)
                .OrderBy(u => u.Cognome)
                .ThenBy(u => u.Nome)
                .ToListAsync();

            // Griglia mensile
            var tuttiIncassiAnno = await _context.IncassiProfessionisti
                .Include(ip => ip.IncassoFattura)
                .Include(ip => ip.Utente)
                .Where(ip => ip.IncassoFattura!.ScadenzaFatturazione!.Anno == annoCorrente)
                .ToListAsync();

            var gridMensile = professionisti.Select(p => new {
                Professionista = $"{p.Nome} {p.Cognome}",
                Mesi = Enumerable.Range(1, 12).Select(m => 
                    tuttiIncassiAnno
                        .Where(i => i.UtenteId == p.Id && i.IncassoFattura!.DataIncasso.Month == m)
                        .Sum(i => i.Importo)
                ).ToList(),
                Totale = tuttiIncassiAnno.Where(i => i.UtenteId == p.Id).Sum(i => i.Importo)
            }).ToList();

            var totaliMensili = Enumerable.Range(1, 12).Select(m =>
                tuttiIncassiAnno.Where(i => i.IncassoFattura!.DataIncasso.Month == m).Sum(i => i.Importo)
            ).ToList();

            // Crea workbook Excel
            using var workbook = new XLWorkbook();
            
            // === FOGLIO 1: RIEPILOGO ===
            var wsRiepilogo = workbook.Worksheets.Add("Riepilogo");
            
            // Titolo
            wsRiepilogo.Cell("A1").Value = $"Report Professionisti - {mesi[meseCorrente]} {annoCorrente}";
            wsRiepilogo.Cell("A1").Style.Font.Bold = true;
            wsRiepilogo.Cell("A1").Style.Font.FontSize = 16;
            wsRiepilogo.Range("A1:D1").Merge();
            
            wsRiepilogo.Cell("A2").Value = progressivo ? "Modalità: Progressivo (Gen - " + mesi[meseCorrente] + ")" : "Modalità: Solo Mese";
            wsRiepilogo.Cell("A2").Style.Font.Italic = true;
            
            // Riepilogo generale
            wsRiepilogo.Cell("A4").Value = "RIEPILOGO GENERALE";
            wsRiepilogo.Cell("A4").Style.Font.Bold = true;
            wsRiepilogo.Cell("A4").Style.Fill.BackgroundColor = XLColor.FromHtml("#17a2b8");
            wsRiepilogo.Cell("A4").Style.Font.FontColor = XLColor.White;
            wsRiepilogo.Range("A4:D4").Merge();
            
            wsRiepilogo.Cell("A5").Value = "Totale Incassato";
            wsRiepilogo.Cell("B5").Value = totaleGenerale;
            wsRiepilogo.Cell("B5").Style.NumberFormat.Format = "€ #,##0.00";
            wsRiepilogo.Cell("B5").Style.Font.Bold = true;
            
            wsRiepilogo.Cell("A6").Value = "Professionisti Attivi";
            wsRiepilogo.Cell("B6").Value = reportData.Count;
            
            // Tabella Incassi per Professionista
            wsRiepilogo.Cell("A8").Value = "INCASSI PER PROFESSIONISTA";
            wsRiepilogo.Cell("A8").Style.Font.Bold = true;
            wsRiepilogo.Cell("A8").Style.Fill.BackgroundColor = XLColor.FromHtml("#0d6efd");
            wsRiepilogo.Cell("A8").Style.Font.FontColor = XLColor.White;
            wsRiepilogo.Range("A8:D8").Merge();
            
            wsRiepilogo.Cell("A9").Value = "#";
            wsRiepilogo.Cell("B9").Value = "Professionista";
            wsRiepilogo.Cell("C9").Value = "Incassato";
            wsRiepilogo.Cell("D9").Value = "Quota %";
            wsRiepilogo.Range("A9:D9").Style.Font.Bold = true;
            wsRiepilogo.Range("A9:D9").Style.Fill.BackgroundColor = XLColor.FromHtml("#e9ecef");
            
            int row = 10;
            int pos = 1;
            foreach (var item in reportData)
            {
                wsRiepilogo.Cell(row, 1).Value = pos;
                wsRiepilogo.Cell(row, 2).Value = item.NomeCompleto;
                wsRiepilogo.Cell(row, 3).Value = item.TotaleIncassato;
                wsRiepilogo.Cell(row, 3).Style.NumberFormat.Format = "€ #,##0.00";
                var quota = totaleGenerale > 0 ? (item.TotaleIncassato / totaleGenerale * 100) : 0;
                wsRiepilogo.Cell(row, 4).Value = quota / 100;
                wsRiepilogo.Cell(row, 4).Style.NumberFormat.Format = "0.0%";
                row++;
                pos++;
            }
            
            // Riga totale
            wsRiepilogo.Cell(row, 1).Value = "";
            wsRiepilogo.Cell(row, 2).Value = "TOTALE";
            wsRiepilogo.Cell(row, 2).Style.Font.Bold = true;
            wsRiepilogo.Cell(row, 3).Value = totaleGenerale;
            wsRiepilogo.Cell(row, 3).Style.NumberFormat.Format = "€ #,##0.00";
            wsRiepilogo.Cell(row, 3).Style.Font.Bold = true;
            wsRiepilogo.Cell(row, 4).Value = 1;
            wsRiepilogo.Cell(row, 4).Style.NumberFormat.Format = "0%";
            wsRiepilogo.Cell(row, 4).Style.Font.Bold = true;
            wsRiepilogo.Range(row, 1, row, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#d4edda");
            
            wsRiepilogo.Columns().AdjustToContents();
            
            // === FOGLIO 2: DETTAGLIO MENSILE ===
            var wsMensile = workbook.Worksheets.Add("Dettaglio Mensile");
            
            // Titolo
            wsMensile.Cell("A1").Value = $"Dettaglio Mensile per Professionista - Anno {annoCorrente}";
            wsMensile.Cell("A1").Style.Font.Bold = true;
            wsMensile.Cell("A1").Style.Font.FontSize = 14;
            wsMensile.Range("A1:N1").Merge();
            
            // Header
            wsMensile.Cell("A3").Value = "Professionista";
            var mesiBrevi = new[] { "Gen", "Feb", "Mar", "Apr", "Mag", "Giu", "Lug", "Ago", "Set", "Ott", "Nov", "Dic" };
            for (int m = 0; m < 12; m++)
            {
                wsMensile.Cell(3, m + 2).Value = mesiBrevi[m];
            }
            wsMensile.Cell(3, 14).Value = "TOTALE";
            wsMensile.Range(3, 1, 3, 14).Style.Font.Bold = true;
            wsMensile.Range(3, 1, 3, 14).Style.Fill.BackgroundColor = XLColor.FromHtml("#343a40");
            wsMensile.Range(3, 1, 3, 14).Style.Font.FontColor = XLColor.White;
            
            // Dati
            row = 4;
            foreach (var prof in gridMensile)
            {
                wsMensile.Cell(row, 1).Value = prof.Professionista;
                for (int m = 0; m < 12; m++)
                {
                    var val = prof.Mesi[m];
                    if (val > 0)
                    {
                        wsMensile.Cell(row, m + 2).Value = val;
                        wsMensile.Cell(row, m + 2).Style.NumberFormat.Format = "#,##0";
                    }
                    else
                    {
                        wsMensile.Cell(row, m + 2).Value = "-";
                        wsMensile.Cell(row, m + 2).Style.Font.FontColor = XLColor.Gray;
                    }
                }
                wsMensile.Cell(row, 14).Value = prof.Totale;
                wsMensile.Cell(row, 14).Style.NumberFormat.Format = "€ #,##0.00";
                wsMensile.Cell(row, 14).Style.Font.Bold = true;
                
                // Colore alternato righe
                if (row % 2 == 0)
                {
                    wsMensile.Range(row, 1, row, 14).Style.Fill.BackgroundColor = XLColor.FromHtml("#f8f9fa");
                }
                row++;
            }
            
            // Riga totali mese
            wsMensile.Cell(row, 1).Value = "TOTALE MESE";
            wsMensile.Cell(row, 1).Style.Font.Bold = true;
            for (int m = 0; m < 12; m++)
            {
                wsMensile.Cell(row, m + 2).Value = totaliMensili[m];
                wsMensile.Cell(row, m + 2).Style.NumberFormat.Format = "€ #,##0";
            }
            var totaleAnnuo = totaliMensili.Sum();
            wsMensile.Cell(row, 14).Value = totaleAnnuo;
            wsMensile.Cell(row, 14).Style.NumberFormat.Format = "€ #,##0.00";
            wsMensile.Range(row, 1, row, 14).Style.Font.Bold = true;
            wsMensile.Range(row, 1, row, 14).Style.Fill.BackgroundColor = XLColor.FromHtml("#17a2b8");
            wsMensile.Range(row, 1, row, 14).Style.Font.FontColor = XLColor.White;
            
            wsMensile.Columns().AdjustToContents();
            
            // Export
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;
            
            var fileName = $"Report_Professionisti_{(progressivo ? "Progressivo_" : "")}{mesi[meseCorrente]}_{annoCorrente}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        #region Mandati CRUD

        // POST: /Amministrazione/CreateMandato
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMandato(int clienteId, string importoAnnuo, string? rimborsoSpese, int tipoScadenza, string? note, int anno, bool generaScadenze = true)
        {
            try
            {
                // Parse importo (gestisce sia virgola che punto)
                var importoParsed = decimal.Parse(importoAnnuo.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
                var rimborsoParsed = string.IsNullOrEmpty(rimborsoSpese) ? 0 : decimal.Parse(rimborsoSpese.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
                
                var mandato = new MandatoCliente
                {
                    ClienteId = clienteId,
                    Anno = anno,
                    ImportoAnnuo = importoParsed,
                    RimborsoSpese = rimborsoParsed,
                    TipoScadenza = (TipoScadenzaMandato)tipoScadenza,
                    Note = note,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                _context.MandatiClienti.Add(mandato);
                await _context.SaveChangesAsync();

                if (generaScadenze)
                {
                    await GeneraScadenzePerMandato(mandato);
                }

                TempData["Success"] = $"Mandato creato con successo" + (generaScadenze ? " e scadenze generate." : ".");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore nella creazione del mandato: {ex.Message}";
            }

            return RedirectToAction(nameof(Mandati), new { anno });
        }

        // POST: /Amministrazione/UpdateMandato
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMandato(int id, string importoAnnuo, string? rimborsoSpese, int tipoScadenza, string? note, bool isActive, int anno, bool rigeneraScadenze = false)
        {
            try
            {
                var mandato = await _context.MandatiClienti
                    .Include(m => m.Scadenze)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (mandato == null)
                {
                    TempData["Error"] = "Mandato non trovato.";
                    return RedirectToAction(nameof(Mandati), new { anno });
                }

                var importoParsed = decimal.Parse(importoAnnuo.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
                var rimborsoParsed = string.IsNullOrEmpty(rimborsoSpese) ? 0 : decimal.Parse(rimborsoSpese.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
                var nuovoTipoScadenza = (TipoScadenzaMandato)tipoScadenza;

                // Verifica se ci sono scadenze protette (con proforma o fattura)
                var scadenzeProtette = mandato.Scadenze?.Where(s => s.NumeroProforma.HasValue || s.NumeroFattura.HasValue).ToList() ?? new List<ScadenzaFatturazione>();
                var scadenzeNonProtette = mandato.Scadenze?.Where(s => !s.NumeroProforma.HasValue && !s.NumeroFattura.HasValue).ToList() ?? new List<ScadenzaFatturazione>();
                
                // Se ci sono scadenze protette e cambiano importo/tipo, blocca
                if (scadenzeProtette.Any() && (mandato.ImportoAnnuo != importoParsed || mandato.TipoScadenza != nuovoTipoScadenza))
                {
                    TempData["Error"] = "Non è possibile modificare importo o tipo scadenza: esistono scadenze con proforma/fattura.";
                    return RedirectToAction(nameof(Mandati), new { anno });
                }

                // Verifica se il tipo scadenza o l'importo sono cambiati
                bool tipoOImportoCambiato = mandato.TipoScadenza != nuovoTipoScadenza || mandato.ImportoAnnuo != importoParsed || mandato.RimborsoSpese != rimborsoParsed;

                // Aggiorna il mandato
                mandato.ImportoAnnuo = importoParsed;
                mandato.RimborsoSpese = rimborsoParsed;
                mandato.TipoScadenza = nuovoTipoScadenza;
                mandato.Note = note;
                mandato.IsActive = isActive;
                mandato.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                var messaggi = new List<string>();

                // Se tipo/importo cambiato e ci sono scadenze non protette, rigenera automaticamente
                if (tipoOImportoCambiato && scadenzeNonProtette.Any())
                {
                    // Elimina le scadenze non protette
                    _context.ScadenzeFatturazione.RemoveRange(scadenzeNonProtette);
                    await _context.SaveChangesAsync();

                    // Rigenera le nuove scadenze
                    await GeneraScadenzePerMandato(mandato);
                    
                    messaggi.Add($"Mandato {mandato.Anno} aggiornato con {mandato.NumeroRate} scadenze rigenerate");
                }
                else if (rigeneraScadenze && !(mandato.Scadenze?.Any() ?? false))
                {
                    // Rigenera solo se richiesto esplicitamente e non ci sono scadenze
                    await GeneraScadenzePerMandato(mandato);
                    messaggi.Add($"Mandato {mandato.Anno} aggiornato con {mandato.NumeroRate} scadenze generate");
                }
                else
                {
                    messaggi.Add($"Mandato {mandato.Anno} aggiornato");
                }

                // ============ PROPAGAZIONE AGLI ANNI FUTURI (solo in avanti, mai indietro) ============
                if (tipoOImportoCambiato)
                {
                    // Trova tutti i mandati dello stesso cliente per anni SUCCESSIVI a quello modificato
                    // La propagazione va SEMPRE in avanti, indipendentemente dall'anno corrente
                    var mandatiFuturi = await _context.MandatiClienti
                        .Include(m => m.Scadenze)
                        .Where(m => m.ClienteId == mandato.ClienteId && m.Anno > mandato.Anno)
                        .OrderBy(m => m.Anno)
                        .ToListAsync();

                    foreach (var mandatoFuturo in mandatiFuturi)
                    {
                        // Verifica se ha scadenze protette (con proforma o fattura)
                        var scadenzeFutureProtette = mandatoFuturo.Scadenze?
                            .Where(s => s.NumeroProforma.HasValue || s.NumeroFattura.HasValue)
                            .ToList() ?? new List<ScadenzaFatturazione>();

                        if (scadenzeFutureProtette.Any())
                        {
                            // Anno futuro ha scadenze protette, non può essere aggiornato
                            messaggi.Add($"Anno {mandatoFuturo.Anno}: non aggiornato (scadenze con documenti)");
                            continue;
                        }

                        // Aggiorna il mandato futuro
                        mandatoFuturo.ImportoAnnuo = importoParsed;
                        mandatoFuturo.RimborsoSpese = rimborsoParsed;
                        mandatoFuturo.TipoScadenza = nuovoTipoScadenza;
                        mandatoFuturo.UpdatedAt = DateTime.Now;

                        // Elimina tutte le scadenze esistenti (non protette)
                        var scadenzeFutureDaEliminare = mandatoFuturo.Scadenze?
                            .Where(s => !s.NumeroProforma.HasValue && !s.NumeroFattura.HasValue)
                            .ToList() ?? new List<ScadenzaFatturazione>();

                        if (scadenzeFutureDaEliminare.Any())
                        {
                            _context.ScadenzeFatturazione.RemoveRange(scadenzeFutureDaEliminare);
                        }

                        await _context.SaveChangesAsync();

                        // Rigenera le scadenze per l'anno futuro
                        await GeneraScadenzePerMandato(mandatoFuturo);

                        messaggi.Add($"Anno {mandatoFuturo.Anno}: propagato con {mandatoFuturo.NumeroRate} scadenze");
                    }
                }

                TempData["Success"] = string.Join("<br/>", messaggi);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore nell'aggiornamento: {ex.Message}";
            }

            return RedirectToAction(nameof(Mandati), new { anno });
        }

        // POST: /Amministrazione/DeleteMandato
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMandato(int id, int anno)
        {
            try
            {
                var mandato = await _context.MandatiClienti
                    .Include(m => m.Scadenze)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (mandato == null)
                {
                    TempData["Error"] = "Mandato non trovato.";
                    return RedirectToAction(nameof(Mandati), new { anno });
                }

                // Verifica se ci sono scadenze con proforma o fattura (protette)
                var scadenzeProtette = mandato.Scadenze?.Any(s => s.NumeroProforma.HasValue || s.NumeroFattura.HasValue) ?? false;
                
                if (scadenzeProtette)
                {
                    TempData["Error"] = "Non è possibile eliminare il mandato: esistono scadenze con proforma/fattura.";
                    return RedirectToAction(nameof(Mandati), new { anno });
                }

                // Elimina prima tutte le scadenze non protette
                if (mandato.Scadenze?.Any() == true)
                {
                    _context.ScadenzeFatturazione.RemoveRange(mandato.Scadenze);
                    await _context.SaveChangesAsync();
                }

                // Poi elimina il mandato
                _context.MandatiClienti.Remove(mandato);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Mandato e scadenze eliminati con successo.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore nell'eliminazione: {ex.Message}";
            }

            return RedirectToAction(nameof(Mandati), new { anno });
        }

        // GET: /Amministrazione/GetScadenzeMandato
        [HttpGet]
        public async Task<IActionResult> GetScadenzeMandato(int mandatoId)
        {
            try
            {
                var scadenze = await _context.ScadenzeFatturazione
                    .Where(s => s.MandatoClienteId == mandatoId)
                    .OrderBy(s => s.DataScadenza)
                    .Select(s => new
                    {
                        id = s.Id,
                        dataScadenza = s.DataScadenza,
                        importo = s.ImportoMandato,
                        numeroProforma = s.NumeroProforma,
                        numeroFattura = s.NumeroFattura
                    })
                    .ToListAsync();

                return Json(new { success = true, scadenze });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // POST: /Amministrazione/UpdateScadenzeDateMandato
        [HttpPost]
        public async Task<IActionResult> UpdateScadenzeDateMandato([FromBody] UpdateScadenzeDateRequest request)
        {
            try
            {
                if (request?.Scadenze == null || !request.Scadenze.Any())
                {
                    return Json(new { success = false, error = "Nessuna scadenza da aggiornare." });
                }

                foreach (var item in request.Scadenze)
                {
                    var scadenza = await _context.ScadenzeFatturazione.FindAsync(item.Id);
                    if (scadenza != null)
                    {
                        // Non permettere modifica se ha già proforma o fattura
                        if (scadenza.NumeroProforma.HasValue || scadenza.NumeroFattura.HasValue)
                        {
                            continue;
                        }

                        if (DateTime.TryParse(item.DataScadenza, out var nuovaData))
                        {
                            scadenza.DataScadenza = nuovaData;
                        }
                    }
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // POST: /Amministrazione/GeneraScadenze
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GeneraScadenze(int mandatoId, bool forza = false)
        {
            try
            {
                var mandato = await _context.MandatiClienti
                    .Include(m => m.Scadenze)
                    .FirstOrDefaultAsync(m => m.Id == mandatoId);

                if (mandato == null)
                {
                    TempData["Error"] = "Mandato non trovato.";
                    return RedirectToAction(nameof(Mandati));
                }

                // Se ci sono scadenze esistenti
                if (mandato.Scadenze?.Any() == true)
                {
                    // Verifica se ci sono scadenze protette
                    var scadenzeProtette = mandato.Scadenze.Where(s => s.NumeroProforma.HasValue || s.NumeroFattura.HasValue).ToList();
                    
                    if (scadenzeProtette.Any())
                    {
                        TempData["Error"] = "Non è possibile rigenerare: esistono scadenze con proforma/fattura.";
                        return RedirectToAction(nameof(Mandati), new { anno = mandato.Anno });
                    }

                    if (!forza)
                    {
                        TempData["Error"] = "Le scadenze sono già state generate. Usa 'Rigenera' per sovrascriverle.";
                        return RedirectToAction(nameof(Mandati), new { anno = mandato.Anno });
                    }

                    // Elimina le scadenze esistenti (non protette)
                    _context.ScadenzeFatturazione.RemoveRange(mandato.Scadenze);
                    await _context.SaveChangesAsync();
                }

                await GeneraScadenzePerMandato(mandato);
                TempData["Success"] = $"Generate {mandato.NumeroRate} scadenze per il mandato.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore nella generazione: {ex.Message}";
            }

            return RedirectToAction(nameof(Mandati));
        }

        // POST: /Amministrazione/RigeneraScadenze - Forza la rigenerazione
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RigeneraScadenze(int mandatoId)
        {
            return await GeneraScadenze(mandatoId, forza: true);
        }

        // Metodo helper per generare le scadenze
        private async Task GeneraScadenzePerMandato(MandatoCliente mandato)
        {
            var dateScadenze = CalcolaDateScadenze(mandato.Anno, mandato.TipoScadenza);
            var importoRata = Math.Round(mandato.ImportoAnnuo / dateScadenze.Count, 2);

            // Aggiusta l'ultima rata per eventuali arrotondamenti
            var totaleRate = importoRata * (dateScadenze.Count - 1);
            var ultimaRata = mandato.ImportoAnnuo - totaleRate;

            for (int i = 0; i < dateScadenze.Count; i++)
            {
                var scadenza = new ScadenzaFatturazione
                {
                    MandatoClienteId = mandato.Id,
                    ClienteId = mandato.ClienteId,
                    Anno = mandato.Anno,
                    DataScadenza = dateScadenze[i],
                    ImportoMandato = (i == dateScadenze.Count - 1) ? ultimaRata : importoRata,
                    RimborsoSpese = mandato.RimborsoSpese, // Stesso importo per ogni scadenza
                    Stato = StatoScadenza.Aperta,
                    StatoIncasso = StatoIncasso.DaIncassare,
                    CreatedAt = DateTime.Now
                };

                _context.ScadenzeFatturazione.Add(scadenza);
            }

            await _context.SaveChangesAsync();
        }

        // Calcola le date di scadenza in base al tipo
        private List<DateTime> CalcolaDateScadenze(int anno, TipoScadenzaMandato tipo)
        {
            var date = new List<DateTime>();

            switch (tipo)
            {
                case TipoScadenzaMandato.Mensile:
                    // 12 rate: fine di ogni mese
                    for (int mese = 1; mese <= 12; mese++)
                    {
                        date.Add(new DateTime(anno, mese, DateTime.DaysInMonth(anno, mese)));
                    }
                    break;

                case TipoScadenzaMandato.Bimestrale:
                    // 6 rate: fine feb, apr, giu, ago, ott, dic
                    date.Add(new DateTime(anno, 2, DateTime.DaysInMonth(anno, 2)));
                    date.Add(new DateTime(anno, 4, 30));
                    date.Add(new DateTime(anno, 6, 30));
                    date.Add(new DateTime(anno, 8, 31));
                    date.Add(new DateTime(anno, 10, 31));
                    date.Add(new DateTime(anno, 12, 31));
                    break;

                case TipoScadenzaMandato.Trimestrale:
                    // 4 rate: fine mar, giu, set, dic
                    date.Add(new DateTime(anno, 3, 31));
                    date.Add(new DateTime(anno, 6, 30));
                    date.Add(new DateTime(anno, 9, 30));
                    date.Add(new DateTime(anno, 12, 31));
                    break;

                case TipoScadenzaMandato.Semestrale:
                    // 2 rate: fine giu, dic
                    date.Add(new DateTime(anno, 6, 30));
                    date.Add(new DateTime(anno, 12, 31));
                    break;

                case TipoScadenzaMandato.Annuale:
                    // 1 rata: fine dic
                    date.Add(new DateTime(anno, 12, 31));
                    break;
            }

            return date;
        }

        #endregion

        #region Scadenze - Proforma e Fatture

        // POST: /Amministrazione/CreateScadenzaManuale
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateScadenzaManuale(int clienteId, DateTime dataScadenza, string importoMandato, string? rimborsoSpese, int anno)
        {
            try
            {
                var cliente = await _context.Clienti.FindAsync(clienteId);
                if (cliente == null)
                {
                    TempData["Error"] = "Cliente non trovato.";
                    return RedirectToAction(nameof(Scadenze), new { anno });
                }

                var importoParsed = decimal.Parse(importoMandato.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
                var rimborsoParsed = string.IsNullOrEmpty(rimborsoSpese) ? 0 : decimal.Parse(rimborsoSpese.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);

                var nuovaScadenza = new ScadenzaFatturazione
                {
                    ClienteId = clienteId,
                    MandatoClienteId = null, // Scadenza manuale, non collegata a mandato
                    Anno = anno,
                    DataScadenza = dataScadenza,
                    ImportoMandato = importoParsed,
                    RimborsoSpese = rimborsoParsed,
                    Stato = StatoScadenza.Aperta,
                    CreatedAt = DateTime.Now
                };

                _context.ScadenzeFatturazione.Add(nuovaScadenza);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Scadenza manuale creata per {cliente.RagioneSociale} - Totale: {(importoParsed + rimborsoParsed):C}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore nella creazione: {ex.Message}";
            }

            return RedirectToAction(nameof(Scadenze), new { anno });
        }

        // POST: /Amministrazione/EmettiProforma
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmettiProforma(int id, DateTime dataProforma, int anno)
        {
            try
            {
                var scadenza = await _context.ScadenzeFatturazione.FindAsync(id);
                if (scadenza == null)
                {
                    TempData["Error"] = "Scadenza non trovata.";
                    return RedirectToAction(nameof(Scadenze), new { anno });
                }

                if (scadenza.Stato != StatoScadenza.Aperta)
                {
                    TempData["Error"] = "La scadenza non è in stato Aperta.";
                    return RedirectToAction(nameof(Scadenze), new { anno });
                }

                // Ottieni prossimo numero proforma
                var numeroProforma = await GetProssimoNumeroDocumento(scadenza.Anno, TipoDocumento.Proforma);

                scadenza.NumeroProforma = numeroProforma;
                scadenza.DataProforma = dataProforma;
                scadenza.Stato = StatoScadenza.Proforma;
                scadenza.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                TempData["Success"] = $"Proforma {scadenza.NumeroProformaFormattato} emessa con successo.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore nell'emissione della proforma: {ex.Message}";
            }

            return RedirectToAction(nameof(Scadenze), new { anno });
        }

        // POST: /Amministrazione/EmettiFattura
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmettiFattura(int id, DateTime dataFattura, int anno)
        {
            try
            {
                var scadenza = await _context.ScadenzeFatturazione.FindAsync(id);
                if (scadenza == null)
                {
                    TempData["Error"] = "Scadenza non trovata.";
                    return RedirectToAction(nameof(Scadenze), new { anno });
                }

                if (scadenza.Stato == StatoScadenza.Fatturata)
                {
                    TempData["Error"] = "La scadenza è già fatturata.";
                    return RedirectToAction(nameof(Scadenze), new { anno });
                }

                // Ottieni prossimo numero fattura
                var numeroFattura = await GetProssimoNumeroDocumento(scadenza.Anno, TipoDocumento.Fattura);

                scadenza.NumeroFattura = numeroFattura;
                scadenza.DataFattura = dataFattura;
                scadenza.Stato = StatoScadenza.Fatturata;
                scadenza.StatoIncasso = StatoIncasso.DaIncassare;
                scadenza.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                TempData["Success"] = $"Fattura {scadenza.NumeroFatturaFormattato} emessa con successo.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore nell'emissione della fattura: {ex.Message}";
            }

            return RedirectToAction(nameof(Scadenze), new { anno });
        }

        // POST: /Amministrazione/AnnullaProforma
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AnnullaProforma(int id, int anno)
        {
            try
            {
                var scadenza = await _context.ScadenzeFatturazione.FindAsync(id);
                if (scadenza == null)
                {
                    TempData["Error"] = "Scadenza non trovata.";
                    return RedirectToAction(nameof(Scadenze), new { anno });
                }

                if (scadenza.Stato != StatoScadenza.Proforma)
                {
                    TempData["Error"] = "La scadenza non è in stato Proforma.";
                    return RedirectToAction(nameof(Scadenze), new { anno });
                }

                if (scadenza.NumeroFattura.HasValue)
                {
                    TempData["Error"] = "Impossibile annullare: è già stata emessa la fattura.";
                    return RedirectToAction(nameof(Scadenze), new { anno });
                }

                // VERIFICA: deve essere l'ULTIMA proforma dell'anno
                var ultimaProforma = await _context.ScadenzeFatturazione
                    .Include(s => s.Cliente)
                    .Where(s => s.Anno == scadenza.Anno && s.NumeroProforma.HasValue)
                    .OrderByDescending(s => s.NumeroProforma)
                    .FirstOrDefaultAsync();

                if (scadenza.NumeroProforma != ultimaProforma?.NumeroProforma)
                {
                    TempData["Error"] = $"Impossibile annullare: devi prima annullare la Proforma n.{ultimaProforma?.NumeroProforma}/{scadenza.Anno} " +
                                       $"di <strong>{ultimaProforma?.Cliente?.RagioneSociale}</strong> " +
                                       $"(scadenza {ultimaProforma?.DataScadenza:dd/MM/yyyy})";
                    return RedirectToAction(nameof(Scadenze), new { anno });
                }

                var numeroAnnullato = scadenza.NumeroProformaFormattato;

                // Decrementa contatore (è sicuramente l'ultimo)
                var contatore = await _context.ContatoriDocumenti
                    .FirstOrDefaultAsync(c => c.Anno == scadenza.Anno && c.TipoDocumento == TipoDocumento.Proforma);
                if (contatore != null)
                {
                    contatore.UltimoNumero--;
                    contatore.UpdatedAt = DateTime.Now;
                }

                scadenza.NumeroProforma = null;
                scadenza.DataProforma = null;
                scadenza.Stato = StatoScadenza.Aperta;
                scadenza.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                TempData["Success"] = $"Proforma {numeroAnnullato} annullata con successo. Contatore aggiornato.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore nell'annullamento: {ex.Message}";
            }

            return RedirectToAction(nameof(Scadenze), new { anno });
        }

        // POST: /Amministrazione/AnnullaFattura
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AnnullaFattura(int id, int anno)
        {
            try
            {
                var scadenza = await _context.ScadenzeFatturazione
                    .Include(s => s.Incassi)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (scadenza == null)
                {
                    TempData["Error"] = "Scadenza non trovata.";
                    return RedirectToAction(nameof(Scadenze), new { anno });
                }

                if (scadenza.Stato != StatoScadenza.Fatturata)
                {
                    TempData["Error"] = "La scadenza non è fatturata.";
                    return RedirectToAction(nameof(Scadenze), new { anno });
                }

                if (scadenza.Incassi?.Any() == true)
                {
                    TempData["Error"] = "Impossibile annullare: esistono incassi registrati per questa fattura.";
                    return RedirectToAction(nameof(Scadenze), new { anno });
                }

                // VERIFICA: deve essere l'ULTIMA fattura dell'anno
                var ultimaFattura = await _context.ScadenzeFatturazione
                    .Include(s => s.Cliente)
                    .Where(s => s.Anno == scadenza.Anno && s.NumeroFattura.HasValue)
                    .OrderByDescending(s => s.NumeroFattura)
                    .FirstOrDefaultAsync();

                if (scadenza.NumeroFattura != ultimaFattura?.NumeroFattura)
                {
                    TempData["Error"] = $"Impossibile annullare: devi prima annullare la Fattura n.{ultimaFattura?.NumeroFattura}/{scadenza.Anno} " +
                                       $"di <strong>{ultimaFattura?.Cliente?.RagioneSociale}</strong> " +
                                       $"(scadenza {ultimaFattura?.DataScadenza:dd/MM/yyyy})";
                    return RedirectToAction(nameof(Scadenze), new { anno });
                }

                var numeroAnnullato = scadenza.NumeroFatturaFormattato;

                // Decrementa contatore (è sicuramente l'ultimo)
                var contatore = await _context.ContatoriDocumenti
                    .FirstOrDefaultAsync(c => c.Anno == scadenza.Anno && c.TipoDocumento == TipoDocumento.Fattura);
                if (contatore != null)
                {
                    contatore.UltimoNumero--;
                    contatore.UpdatedAt = DateTime.Now;
                }

                scadenza.NumeroFattura = null;
                scadenza.DataFattura = null;
                scadenza.StatoIncasso = StatoIncasso.DaIncassare;
                
                // Torna a Proforma se c'era, altrimenti Aperta
                scadenza.Stato = scadenza.NumeroProforma.HasValue ? StatoScadenza.Proforma : StatoScadenza.Aperta;
                scadenza.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                TempData["Success"] = $"Fattura {numeroAnnullato} annullata con successo. Contatore aggiornato.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore nell'annullamento: {ex.Message}";
            }

            return RedirectToAction(nameof(Scadenze), new { anno });
        }

        // POST: /Amministrazione/ModificaScadenza
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ModificaScadenza(int id, DateTime dataScadenza, string importoMandato, string? rimborsoSpese, int anno)
        {
            try
            {
                var scadenza = await _context.ScadenzeFatturazione.FindAsync(id);

                if (scadenza == null)
                {
                    TempData["Error"] = "Scadenza non trovata.";
                    return RedirectToAction(nameof(Scadenze), new { anno });
                }

                // Non permettere modifica se ha proforma o fattura
                if (scadenza.NumeroProforma.HasValue || scadenza.NumeroFattura.HasValue)
                {
                    TempData["Error"] = "Non è possibile modificare una scadenza con proforma o fattura emessa. Annulla prima il documento.";
                    return RedirectToAction(nameof(Scadenze), new { anno });
                }

                // Parse importi
                var importoParsed = decimal.Parse(importoMandato.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
                var rimborsoParsed = decimal.Parse(rimborsoSpese?.Replace(",", ".") ?? "0", System.Globalization.CultureInfo.InvariantCulture);

                scadenza.DataScadenza = dataScadenza;
                scadenza.ImportoMandato = importoParsed;
                scadenza.RimborsoSpese = rimborsoParsed;
                scadenza.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                TempData["Success"] = "Scadenza modificata con successo.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore nella modifica: {ex.Message}";
            }

            return RedirectToAction(nameof(Scadenze), new { anno });
        }

        // POST: /Amministrazione/EliminaScadenza
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminaScadenza(int id, int anno)
        {
            try
            {
                var scadenza = await _context.ScadenzeFatturazione
                    .Include(s => s.SpesePratiche)
                    .Include(s => s.AccessiClienti)
                    .Include(s => s.FattureCloud)
                    .Include(s => s.BilanciCEE)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (scadenza == null)
                {
                    TempData["Error"] = "Scadenza non trovata.";
                    return RedirectToAction(nameof(Scadenze), new { anno });
                }

                // Non permettere eliminazione se ha proforma o fattura
                if (scadenza.Stato != StatoScadenza.Aperta)
                {
                    TempData["Error"] = "Non è possibile eliminare una scadenza con proforma o fattura. Annulla prima i documenti.";
                    return RedirectToAction(nameof(Scadenze), new { anno });
                }

                // Non permettere eliminazione se ha spese collegate
                var haSpese = (scadenza.SpesePratiche?.Any() ?? false) || 
                              (scadenza.AccessiClienti?.Any() ?? false) ||
                              (scadenza.FattureCloud?.Any() ?? false) ||
                              (scadenza.BilanciCEE?.Any() ?? false);
                
                if (haSpese)
                {
                    TempData["Error"] = "Non è possibile eliminare la scadenza: ha spese collegate. Rimuovi prima le spese dalla pagina corrispondente.";
                    return RedirectToAction(nameof(Scadenze), new { anno });
                }

                _context.ScadenzeFatturazione.Remove(scadenza);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Scadenza eliminata con successo.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore nell'eliminazione: {ex.Message}";
            }

            return RedirectToAction(nameof(Scadenze), new { anno });
        }

        #endregion

        #region Contatori Automatici

        /// <summary>
        /// Ottiene il prossimo numero disponibile per proforma o fattura
        /// </summary>
        private async Task<int> GetProssimoNumeroDocumento(int anno, TipoDocumento tipo)
        {
            var contatore = await _context.ContatoriDocumenti
                .FirstOrDefaultAsync(c => c.Anno == anno && c.TipoDocumento == tipo);

            if (contatore == null)
            {
                contatore = new ContatoreDocumento
                {
                    Anno = anno,
                    TipoDocumento = tipo,
                    UltimoNumero = 0
                };
                _context.ContatoriDocumenti.Add(contatore);
            }

            contatore.UltimoNumero++;
            contatore.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return contatore.UltimoNumero;
        }

        /// <summary>
        /// Ripristina il contatore se viene annullato l'ultimo documento
        /// </summary>

        #endregion

        #region Gestione Anni Fatturazione

        // GET: /Amministrazione/GestioneAnni
        public async Task<IActionResult> GestioneAnni()
        {
            var anni = await _context.AnniFatturazione
                .OrderByDescending(a => a.Anno)
                .ToListAsync();

            // Se non ci sono anni, crea l'anno corrente
            if (!anni.Any())
            {
                var annoCorrente = new AnnoFatturazione
                {
                    Anno = DateTime.Now.Year,
                    IsCurrent = true,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                _context.AnniFatturazione.Add(annoCorrente);
                await _context.SaveChangesAsync();
                anni = new List<AnnoFatturazione> { annoCorrente };
            }

            // Statistiche per ogni anno
            var statistiche = new Dictionary<int, (int Mandati, decimal Fatturato, decimal Incassato)>();
            foreach (var anno in anni)
            {
                var mandati = await _context.MandatiClienti.CountAsync(m => m.Anno == anno.Anno);
                var fatturato = await _context.ScadenzeFatturazione
                    .Where(s => s.Anno == anno.Anno && s.NumeroFattura.HasValue)
                    .SumAsync(s => s.ImportoMandato);
                var incassato = await _context.IncassiFatture
                    .Include(i => i.ScadenzaFatturazione)
                    .Where(i => i.ScadenzaFatturazione!.Anno == anno.Anno)
                    .SumAsync(i => i.ImportoIncassato);

                statistiche[anno.Anno] = (mandati, fatturato, incassato);
            }

            ViewBag.Statistiche = statistiche;
            ViewData["Title"] = "Gestione Anni Fatturazione";
            
            return View(anni);
        }

        // POST: /Amministrazione/CreaAnnoFatturazione
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreaAnnoFatturazione(int anno)
        {
            try
            {
                // Verifica che l'anno non esista già
                if (await _context.AnniFatturazione.AnyAsync(a => a.Anno == anno))
                {
                    TempData["Error"] = $"L'anno {anno} esiste già.";
                    return RedirectToAction(nameof(GestioneAnni));
                }

                var nuovoAnno = new AnnoFatturazione
                {
                    Anno = anno,
                    IsCurrent = false,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                _context.AnniFatturazione.Add(nuovoAnno);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Anno {anno} creato con successo.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore: {ex.Message}";
            }

            return RedirectToAction(nameof(GestioneAnni));
        }

        // POST: /Amministrazione/ImpostaAnnoCorrente
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImpostaAnnoCorrente(int id)
        {
            try
            {
                // Rimuovi flag corrente da tutti
                var tuttiAnni = await _context.AnniFatturazione.ToListAsync();
                foreach (var a in tuttiAnni)
                {
                    a.IsCurrent = false;
                }

                // Imposta nuovo anno corrente
                var anno = await _context.AnniFatturazione.FindAsync(id);
                if (anno != null)
                {
                    anno.IsCurrent = true;
                    anno.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Anno {anno.Anno} impostato come corrente.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore: {ex.Message}";
            }

            return RedirectToAction(nameof(GestioneAnni));
        }

        // POST: /Amministrazione/EliminaAnno
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminaAnno(int id)
        {
            try
            {
                var anno = await _context.AnniFatturazione.FindAsync(id);
                if (anno == null)
                {
                    TempData["Error"] = "Anno non trovato.";
                    return RedirectToAction(nameof(GestioneAnni));
                }

                // Non si può eliminare l'anno corrente
                if (anno.IsCurrent)
                {
                    TempData["Error"] = "Non puoi eliminare l'anno corrente. Imposta prima un altro anno come corrente.";
                    return RedirectToAction(nameof(GestioneAnni));
                }

                // Verifica se ci sono mandati associati
                var mandatiCount = await _context.MandatiClienti.CountAsync(m => m.Anno == anno.Anno);
                if (mandatiCount > 0)
                {
                    TempData["Error"] = $"Non puoi eliminare l'anno {anno.Anno}: ci sono {mandatiCount} mandati associati.";
                    return RedirectToAction(nameof(GestioneAnni));
                }

                // Verifica se ci sono scadenze associate
                var scadenzeCount = await _context.ScadenzeFatturazione.CountAsync(s => s.Anno == anno.Anno);
                if (scadenzeCount > 0)
                {
                    TempData["Error"] = $"Non puoi eliminare l'anno {anno.Anno}: ci sono {scadenzeCount} scadenze associate.";
                    return RedirectToAction(nameof(GestioneAnni));
                }

                _context.AnniFatturazione.Remove(anno);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Anno {anno.Anno} eliminato con successo.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore durante l'eliminazione: {ex.Message}";
            }

            return RedirectToAction(nameof(GestioneAnni));
        }

        // POST: /Amministrazione/CopiaMandatiAnnoSuccessivo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CopiaMandatiAnnoSuccessivo(int annoDa, int annoA, bool generaScadenze)
        {
            try
            {
                // Verifica che l'anno destinazione esista
                var annoDestinazione = await _context.AnniFatturazione.FirstOrDefaultAsync(a => a.Anno == annoA);
                if (annoDestinazione == null)
                {
                    // Crea l'anno se non esiste
                    annoDestinazione = new AnnoFatturazione
                    {
                        Anno = annoA,
                        IsCurrent = false,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };
                    _context.AnniFatturazione.Add(annoDestinazione);
                    await _context.SaveChangesAsync();
                }

                // ============ COPIA MANDATI ============
                var mandatiDaCopiare = await _context.MandatiClienti
                    .Where(m => m.Anno == annoDa && m.IsActive)
                    .ToListAsync();

                int mandatiCopiati = 0;
                foreach (var mandato in mandatiDaCopiare)
                {
                    // Verifica che non esista già un mandato per lo stesso cliente nell'anno destinazione
                    if (await _context.MandatiClienti.AnyAsync(m => m.ClienteId == mandato.ClienteId && m.Anno == annoA))
                        continue;

                    var nuovoMandato = new MandatoCliente
                    {
                        ClienteId = mandato.ClienteId,
                        Anno = annoA,
                        ImportoAnnuo = mandato.ImportoAnnuo,
                        RimborsoSpese = mandato.RimborsoSpese, // Copia anche rimborso spese
                        TipoScadenza = mandato.TipoScadenza,
                        IsActive = true,
                        Note = $"Copiato da anno {annoDa}",
                        CreatedAt = DateTime.Now
                    };

                    _context.MandatiClienti.Add(nuovoMandato);
                    await _context.SaveChangesAsync();

                    // Genera scadenze se richiesto (include RimborsoSpese automaticamente)
                    if (generaScadenze)
                    {
                        await GeneraScadenzePerMandato(nuovoMandato);
                    }

                    mandatiCopiati++;
                }

                // ============ COPIA FATTURE CLOUD ============
                var fattureCloudDaCopiare = await _context.FattureCloud
                    .Where(f => f.Anno == annoDa)
                    .ToListAsync();

                int fattureCloudCopiate = 0;
                foreach (var fattura in fattureCloudDaCopiare)
                {
                    // Verifica che non esista già per lo stesso cliente nell'anno destinazione
                    if (await _context.FattureCloud.AnyAsync(f => f.ClienteId == fattura.ClienteId && f.Anno == annoA))
                        continue;

                    var nuovaFattura = new FatturaCloud
                    {
                        ClienteId = fattura.ClienteId,
                        Anno = annoA,
                        Importo = fattura.Importo,
                        // Aggiorna la data scadenza al nuovo anno (stesso giorno/mese)
                        DataScadenza = new DateTime(annoA, fattura.DataScadenza.Month, fattura.DataScadenza.Day),
                        ScadenzaFatturazioneId = null, // Da associare manualmente
                        Note = $"Copiato da anno {annoDa}",
                        CreatedAt = DateTime.Now
                    };

                    _context.FattureCloud.Add(nuovaFattura);
                    fattureCloudCopiate++;
                }

                // ============ COPIA BILANCI CEE ============
                var bilanciCEEDaCopiare = await _context.BilanciCEE
                    .Where(b => b.Anno == annoDa)
                    .ToListAsync();

                int bilanciCEECopiati = 0;
                foreach (var bilancio in bilanciCEEDaCopiare)
                {
                    // Verifica che non esista già per lo stesso cliente nell'anno destinazione
                    if (await _context.BilanciCEE.AnyAsync(b => b.ClienteId == bilancio.ClienteId && b.Anno == annoA))
                        continue;

                    var nuovoBilancio = new BilancioCEE
                    {
                        ClienteId = bilancio.ClienteId,
                        Anno = annoA,
                        Importo = bilancio.Importo,
                        // Aggiorna la data scadenza al nuovo anno (stesso giorno/mese)
                        DataScadenza = new DateTime(annoA, bilancio.DataScadenza.Month, bilancio.DataScadenza.Day),
                        ScadenzaFatturazioneId = null, // Da associare manualmente
                        Note = $"Copiato da anno {annoDa}",
                        CreatedAt = DateTime.Now
                    };

                    _context.BilanciCEE.Add(nuovoBilancio);
                    bilanciCEECopiati++;
                }

                await _context.SaveChangesAsync();

                // Messaggio riepilogativo
                var messaggi = new List<string>();
                if (mandatiCopiati > 0)
                    messaggi.Add($"{mandatiCopiati} mandati" + (generaScadenze ? " con scadenze" : ""));
                if (fattureCloudCopiate > 0)
                    messaggi.Add($"{fattureCloudCopiate} Fatture Cloud");
                if (bilanciCEECopiati > 0)
                    messaggi.Add($"{bilanciCEECopiati} Bilanci CEE");

                if (messaggi.Any())
                    TempData["Success"] = $"Copiati da {annoDa} a {annoA}: {string.Join(", ", messaggi)}.";
                else
                    TempData["Warning"] = $"Nessun dato da copiare da {annoDa} a {annoA} (già presenti o non esistenti).";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore: {ex.Message}";
            }

            return RedirectToAction(nameof(GestioneAnni));
        }

        // POST: /Amministrazione/CopiaFTCloudAnnoSuccessivo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CopiaFTCloudAnnoSuccessivo(int annoDa, int annoA)
        {
            try
            {
                // Verifica che l'anno destinazione esista
                var annoDestinazione = await _context.AnniFatturazione.FirstOrDefaultAsync(a => a.Anno == annoA);
                if (annoDestinazione == null)
                {
                    annoDestinazione = new AnnoFatturazione
                    {
                        Anno = annoA,
                        IsCurrent = false,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };
                    _context.AnniFatturazione.Add(annoDestinazione);
                    await _context.SaveChangesAsync();
                }

                // Prendi solo i clienti con mandato attivo nell'anno origine
                var clientiConMandatoAttivo = await _context.MandatiClienti
                    .Where(m => m.Anno == annoDa && m.IsActive)
                    .Select(m => m.ClienteId)
                    .ToListAsync();

                var fattureCloudDaCopiare = await _context.FattureCloud
                    .Where(f => f.Anno == annoDa && clientiConMandatoAttivo.Contains(f.ClienteId))
                    .ToListAsync();

                int fattureCloudCopiate = 0;
                foreach (var fattura in fattureCloudDaCopiare)
                {
                    if (await _context.FattureCloud.AnyAsync(f => f.ClienteId == fattura.ClienteId && f.Anno == annoA))
                        continue;

                    var nuovaFattura = new FatturaCloud
                    {
                        ClienteId = fattura.ClienteId,
                        Anno = annoA,
                        Importo = fattura.Importo,
                        DataScadenza = new DateTime(annoA, fattura.DataScadenza.Month, Math.Min(fattura.DataScadenza.Day, DateTime.DaysInMonth(annoA, fattura.DataScadenza.Month))),
                        ScadenzaFatturazioneId = null,
                        Note = $"Copiato da anno {annoDa}",
                        CreatedAt = DateTime.Now
                    };

                    _context.FattureCloud.Add(nuovaFattura);
                    fattureCloudCopiate++;
                }

                await _context.SaveChangesAsync();

                if (fattureCloudCopiate > 0)
                    TempData["Success"] = $"Copiati {fattureCloudCopiate} FT Cloud da {annoDa} a {annoA}.";
                else
                    TempData["Warning"] = $"Nessun FT Cloud da copiare da {annoDa} a {annoA} (già presenti o non esistenti).";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore: {ex.Message}";
            }

            return RedirectToAction(nameof(GestioneAnni));
        }

        // POST: /Amministrazione/CopiaBilanciCEEAnnoSuccessivo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CopiaBilanciCEEAnnoSuccessivo(int annoDa, int annoA)
        {
            try
            {
                // Verifica che l'anno destinazione esista
                var annoDestinazione = await _context.AnniFatturazione.FirstOrDefaultAsync(a => a.Anno == annoA);
                if (annoDestinazione == null)
                {
                    annoDestinazione = new AnnoFatturazione
                    {
                        Anno = annoA,
                        IsCurrent = false,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };
                    _context.AnniFatturazione.Add(annoDestinazione);
                    await _context.SaveChangesAsync();
                }

                // Prendi solo i clienti con mandato attivo nell'anno origine
                var clientiConMandatoAttivo = await _context.MandatiClienti
                    .Where(m => m.Anno == annoDa && m.IsActive)
                    .Select(m => m.ClienteId)
                    .ToListAsync();

                var bilanciCEEDaCopiare = await _context.BilanciCEE
                    .Where(b => b.Anno == annoDa && clientiConMandatoAttivo.Contains(b.ClienteId))
                    .ToListAsync();

                int bilanciCEECopiati = 0;
                foreach (var bilancio in bilanciCEEDaCopiare)
                {
                    if (await _context.BilanciCEE.AnyAsync(b => b.ClienteId == bilancio.ClienteId && b.Anno == annoA))
                        continue;

                    var nuovoBilancio = new BilancioCEE
                    {
                        ClienteId = bilancio.ClienteId,
                        Anno = annoA,
                        Importo = bilancio.Importo,
                        DataScadenza = new DateTime(annoA, bilancio.DataScadenza.Month, Math.Min(bilancio.DataScadenza.Day, DateTime.DaysInMonth(annoA, bilancio.DataScadenza.Month))),
                        ScadenzaFatturazioneId = null,
                        Note = $"Copiato da anno {annoDa}",
                        CreatedAt = DateTime.Now
                    };

                    _context.BilanciCEE.Add(nuovoBilancio);
                    bilanciCEECopiati++;
                }

                await _context.SaveChangesAsync();

                if (bilanciCEECopiati > 0)
                    TempData["Success"] = $"Copiati {bilanciCEECopiati} Bilanci CEE da {annoDa} a {annoA}.";
                else
                    TempData["Warning"] = $"Nessun Bilancio CEE da copiare da {annoDa} a {annoA} (già presenti o non esistenti).";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore: {ex.Message}";
            }

            return RedirectToAction(nameof(GestioneAnni));
        }

        #endregion
    }

    // Request classes for API endpoints
    public class UpdateScadenzeDateRequest
    {
        public List<ScadenzaDateItem> Scadenze { get; set; } = new();
    }

    public class ScadenzaDateItem
    {
        public int Id { get; set; }
        public string DataScadenza { get; set; } = string.Empty;
    }
}


