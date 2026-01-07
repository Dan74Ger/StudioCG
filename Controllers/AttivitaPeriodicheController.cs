using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioCG.Web.Data;
using StudioCG.Web.Filters;
using StudioCG.Web.Models;
using StudioCG.Web.Models.AttivitaPeriodiche;
using StudioCG.Web.Services;
using System.Text.Json;

namespace StudioCG.Web.Controllers
{
    [Authorize]  // Richiede login, ma i permessi sono controllati per singola azione
    public class AttivitaPeriodicheController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPermissionService _permissionService;

        public AttivitaPeriodicheController(ApplicationDbContext context, IPermissionService permissionService)
        {
            _context = context;
            _permissionService = permissionService;
        }

        private async Task<bool> CanAccessAsync(string pageUrl)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return false;
            if (username.Equals("admin", StringComparison.OrdinalIgnoreCase)) return true;
            return await _permissionService.UserHasPermissionAsync(username, pageUrl);
        }

        // DEBUG: Endpoint per visualizzare campi e formule
        [HttpGet]
        public async Task<IActionResult> DebugCampi(int tipoPeriodoId)
        {
            var campi = await _context.CampiPeriodici
                .Where(c => c.TipoPeriodoId == tipoPeriodoId && c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new { c.Nome, c.Label, c.IsCalculated, c.Formula })
                .ToListAsync();
            
            return Json(campi);
        }

        // DEBUG: Endpoint per visualizzare valori salvati
        [HttpGet]
        public async Task<IActionResult> DebugValori(int clienteAttivitaId, int numeroPeriodo)
        {
            var valorePeriodo = await _context.ValoriPeriodi
                .FirstOrDefaultAsync(v => v.ClienteAttivitaPeriodicaId == clienteAttivitaId && v.NumeroPeriodo == numeroPeriodo);
            
            if (valorePeriodo == null)
                return Json(new { error = "Nessun valore trovato" });

            var valori = JsonSerializer.Deserialize<Dictionary<string, string>>(valorePeriodo.Valori);
            var valoriCalcolati = JsonSerializer.Deserialize<Dictionary<string, string>>(valorePeriodo.ValoriCalcolati);
            
            return Json(new { 
                clienteAttivitaId,
                numeroPeriodo,
                valoriInput = valori,
                valoriCalcolati = valoriCalcolati
            });
        }

        // POST: Rinomina una chiave JSON in tutti i ValoriPeriodi di un tipo periodo
        [HttpPost]
        public async Task<IActionResult> RinominaChiaveJson(int tipoPeriodoId, string vecchioNome, string nuovoNome)
        {
            if (string.IsNullOrWhiteSpace(vecchioNome) || string.IsNullOrWhiteSpace(nuovoNome))
            {
                return Json(new { success = false, error = "Nomi non validi" });
            }

            // Trova tutti i ValoriPeriodi per questo tipo periodo
            var clientiAttivita = await _context.ClientiAttivitaPeriodiche
                .Where(c => c.TipoPeriodoId == tipoPeriodoId)
                .Include(c => c.ValoriPeriodi)
                .ToListAsync();

            int aggiornati = 0;
            foreach (var cliente in clientiAttivita)
            {
                foreach (var periodo in cliente.ValoriPeriodi)
                {
                    // Aggiorna Valori
                    var valori = JsonSerializer.Deserialize<Dictionary<string, string>>(periodo.Valori) ?? new Dictionary<string, string>();
                    if (valori.ContainsKey(vecchioNome))
                    {
                        valori[nuovoNome] = valori[vecchioNome];
                        valori.Remove(vecchioNome);
                        periodo.Valori = JsonSerializer.Serialize(valori);
                        aggiornati++;
                    }

                    // Aggiorna ValoriCalcolati
                    var calcolati = JsonSerializer.Deserialize<Dictionary<string, string>>(periodo.ValoriCalcolati) ?? new Dictionary<string, string>();
                    if (calcolati.ContainsKey(vecchioNome))
                    {
                        calcolati[nuovoNome] = calcolati[vecchioNome];
                        calcolati.Remove(vecchioNome);
                        periodo.ValoriCalcolati = JsonSerializer.Serialize(calcolati);
                    }
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = $"Aggiornati {aggiornati} record" });
        }

        // POST: Forza ricalcolo e salva per un cliente
        [HttpPost]
        public async Task<IActionResult> ForzaRicalcolo(int clienteAttivitaId)
        {
            var clienteAttivita = await _context.ClientiAttivitaPeriodiche
                .Include(c => c.TipoPeriodo)
                    .ThenInclude(t => t!.Campi.Where(c => c.IsActive))
                .Include(c => c.ValoriPeriodi)
                .FirstOrDefaultAsync(c => c.Id == clienteAttivitaId);

            if (clienteAttivita?.TipoPeriodo == null)
                return Json(new { success = false, error = "Cliente non trovato" });

            int periodiAggiornati = 0;
            foreach (var periodo in clienteAttivita.ValoriPeriodi.OrderBy(p => p.NumeroPeriodo))
            {
                var risultati = await RicalcolaCampiAsync(clienteAttivitaId, periodo.NumeroPeriodo);
                if (risultati.Any())
                    periodiAggiornati++;
            }

            return Json(new { success = true, message = $"Ricalcolati {periodiAggiornati} periodi" });
        }

        // GET: Forza ricalcolo via URL (per debug)
        [HttpGet]
        public async Task<IActionResult> ForzaRicalcoloGet(int clienteAttivitaId)
        {
            var clienteAttivita = await _context.ClientiAttivitaPeriodiche
                .Include(c => c.TipoPeriodo)
                    .ThenInclude(t => t!.Campi.Where(c => c.IsActive))
                .Include(c => c.ValoriPeriodi)
                .FirstOrDefaultAsync(c => c.Id == clienteAttivitaId);

            if (clienteAttivita?.TipoPeriodo == null)
                return Json(new { success = false, error = "Cliente non trovato" });

            var risultatiTotali = new Dictionary<int, Dictionary<string, string>>();
            var periodi = clienteAttivita.ValoriPeriodi.OrderBy(p => p.NumeroPeriodo).ToList();
            
            for (int i = 0; i < periodi.Count; i++)
            {
                // Prima calcola i campi del periodo corrente
                var risultati = await RicalcolaCampiAsync(clienteAttivitaId, periodi[i].NumeroPeriodo);
                risultatiTotali[periodi[i].NumeroPeriodo] = risultati;
                
                // Poi applica i riporti al periodo successivo
                if (i < periodi.Count - 1)
                {
                    await ApplicaRiportiAsync(clienteAttivitaId, periodi[i].NumeroPeriodo);
                }
            }

            return Json(new { success = true, risultati = risultatiTotali });
        }

        // DEBUG: Mostra regole riporto
        [HttpGet]
        public async Task<IActionResult> DebugRegoleRiporto(int tipoPeriodoId)
        {
            var regole = await _context.RegoleCampi
                .Include(r => r.CampoPeriodico)
                .Where(r => r.CampoPeriodico!.TipoPeriodoId == tipoPeriodoId && r.TipoRegola == "riporto" && r.IsActive)
                .ToListAsync();

            var campi = await _context.CampiPeriodici
                .Where(c => c.TipoPeriodoId == tipoPeriodoId && c.IsActive)
                .ToDictionaryAsync(c => c.Id, c => c.Nome);

            var risultato = regole.Select(r => new {
                id = r.Id,
                campoPeriodicoId = r.CampoPeriodicoId,
                campoOrigineId = r.CampoOrigineId,
                campoOrigineNome = r.CampoOrigineId.HasValue && campi.ContainsKey(r.CampoOrigineId.Value) ? campi[r.CampoOrigineId.Value] : "N/A",
                campoDestinazioneId = r.CampoDestinazioneId,
                campoDestinazioneNome = r.CampoDestinazioneId.HasValue && campi.ContainsKey(r.CampoDestinazioneId.Value) ? campi[r.CampoDestinazioneId.Value] : "N/A",
                condizione = r.CondizioneRiporto
            }).ToList();

            return Json(new { regole = risultato, campi });
        }

        // DEBUG: Forza ricalcolo e mostra risultati
        [HttpGet]
        public async Task<IActionResult> DebugCalcolo(int clienteAttivitaId, int numeroPeriodo)
        {
            var clienteAttivita = await _context.ClientiAttivitaPeriodiche
                .Include(c => c.TipoPeriodo)
                    .ThenInclude(t => t!.Campi.Where(c => c.IsActive))
                .Include(c => c.ValoriPeriodi)
                .FirstOrDefaultAsync(c => c.Id == clienteAttivitaId);

            if (clienteAttivita?.TipoPeriodo == null) 
                return Json(new { error = "Cliente/attività non trovato" });

            var valorePeriodo = clienteAttivita.ValoriPeriodi.FirstOrDefault(v => v.NumeroPeriodo == numeroPeriodo);
            if (valorePeriodo == null) 
                return Json(new { error = "Periodo non trovato" });

            var valori = JsonSerializer.Deserialize<Dictionary<string, string>>(valorePeriodo.Valori) 
                         ?? new Dictionary<string, string>();
            
            var campiCalcolati = clienteAttivita.TipoPeriodo.Campi.Where(c => c.IsCalculated && !string.IsNullOrEmpty(c.Formula)).ToList();
            var campiInput = clienteAttivita.TipoPeriodo.Campi.Where(c => !c.IsCalculated).ToList();
            
            var risultati = new List<object>();
            
            foreach (var campo in campiCalcolati)
            {
                var formula = campo.Formula!;
                var passaggi = new List<string>();
                passaggi.Add($"Formula originale: {formula}");
                
                // Sostituisci campi input
                foreach (var campoDato in campiInput)
                {
                    valori.TryGetValue(campoDato.Nome, out string? val);
                    var valoreNumerico = NormalizzaValore(val);
                    var vecchiaFormula = formula;
                    formula = formula.Replace($"[{campoDato.Nome}]", valoreNumerico);
                    if (vecchiaFormula != formula)
                    {
                        passaggi.Add($"[{campoDato.Nome}] -> {valoreNumerico} (era: {val ?? "null"})");
                    }
                }
                
                // Sostituisci placeholder rimasti
                var formulaFinale = System.Text.RegularExpressions.Regex.Replace(formula, @"\[[^\]]+\]", "0");
                passaggi.Add($"Formula finale: {formulaFinale}");
                
                string risultato;
                try
                {
                    var calc = CalcolaFormula(formulaFinale);
                    risultato = calc.ToString("0.##");
                }
                catch (Exception ex)
                {
                    risultato = $"ERR: {ex.Message}";
                }
                
                risultati.Add(new { 
                    campo = campo.Nome, 
                    passaggi, 
                    risultato 
                });
            }
            
            return Json(new { 
                valoriInput = valori,
                campiInput = campiInput.Select(c => c.Nome).ToList(),
                campiCalcolati = campiCalcolati.Select(c => new { c.Nome, c.Formula }).ToList(),
                risultatiCalcolo = risultati
            });
        }

        // GET: Mostra chiavi JSON esistenti per un tipo periodo
        [HttpGet]
        public async Task<IActionResult> ChiaviJsonEsistenti(int tipoPeriodoId)
        {
            var clientiAttivita = await _context.ClientiAttivitaPeriodiche
                .Where(c => c.TipoPeriodoId == tipoPeriodoId)
                .Include(c => c.ValoriPeriodi)
                .ToListAsync();

            var tutteLeChiavi = new HashSet<string>();
            foreach (var cliente in clientiAttivita)
            {
                foreach (var periodo in cliente.ValoriPeriodi)
                {
                    var valori = JsonSerializer.Deserialize<Dictionary<string, string>>(periodo.Valori) ?? new Dictionary<string, string>();
                    foreach (var key in valori.Keys)
                        tutteLeChiavi.Add(key);
                }
            }

            var campiAttuali = await _context.CampiPeriodici
                .Where(c => c.TipoPeriodoId == tipoPeriodoId && c.IsActive)
                .Select(c => c.Nome)
                .ToListAsync();

            return Json(new { 
                chiaviNelJson = tutteLeChiavi.OrderBy(k => k).ToList(),
                nomiCampiAttuali = campiAttuali,
                chiaviFuoriSincro = tutteLeChiavi.Except(campiAttuali).ToList()
            });
        }

        // ============ GESTIONE SEZIONI ============

        // GET: AttivitaPeriodiche/Gestione
        public async Task<IActionResult> Gestione()
        {
            // Assicura che esista la voce di menu per Attività Periodiche
            await EnsureMenuVoceExistsAsync();

            // Sincronizza i permessi per le attività periodiche esistenti
            await SyncPermissionsAsync();
            
            // Le voci di menu vengono generate dinamicamente da MenuService

            var attivita = await _context.AttivitaPeriodiche
                .Include(a => a.TipiPeriodo)
                .Include(a => a.ClientiAssociati)
                .OrderBy(a => a.OrdineMenu)
                .ToListAsync();

            return View(attivita);
        }

        private async Task SyncPermissionsAsync()
        {
            var attivitaPeriodiche = await _context.AttivitaPeriodiche.Where(a => a.IsActive).ToListAsync();
            var maxPermOrder = await _context.Permissions.MaxAsync(p => (int?)p.DisplayOrder) ?? 0;

            foreach (var att in attivitaPeriodiche)
            {
                var pageUrl = $"/AttivitaPeriodiche/Dati/{att.Id}";
                if (!await _context.Permissions.AnyAsync(p => p.PageUrl == pageUrl))
                {
                    maxPermOrder++;
                    var permission = new Permission
                    {
                        PageName = att.Nome,
                        PageUrl = pageUrl,
                        Description = $"Gestione {att.NomePlurale}",
                        Category = "ATTIVITA_PERIODICHE",
                        Icon = att.Icona,
                        ShowInMenu = false,
                        DisplayOrder = maxPermOrder
                    };
                    _context.Permissions.Add(permission);
                }
            }
            await _context.SaveChangesAsync();
        }

        private async Task SyncMenuAttivitaAsync()
        {
            var attivitaPeriodiche = await _context.AttivitaPeriodiche.Where(a => a.IsActive).ToListAsync();
            
            foreach (var att in attivitaPeriodiche)
            {
                await CreaMenuPerAttivitaAsync(att);
            }
        }

        private async Task EnsureMenuVoceExistsAsync()
        {
            // Verifica se esiste già la voce di menu per Attività Periodiche
            var existingVoce = await _context.VociMenu
                .FirstOrDefaultAsync(v => v.TipoVoce == "DynamicAttivitaPeriodiche");

            if (existingVoce == null)
            {
                // Trova l'ordine dopo ATTIVITÀ
                var attivitaVoce = await _context.VociMenu
                    .FirstOrDefaultAsync(v => v.TipoVoce == "DynamicAttivita");
                var ordine = (attivitaVoce?.DisplayOrder ?? 30) + 1;

                var voceMenu = new VoceMenu
                {
                    Nome = "Attività Periodiche",
                    Icon = "fas fa-calendar-alt",
                    DisplayOrder = ordine,
                    IsVisible = true,
                    IsActive = true,
                    IsGroup = true,
                    TipoVoce = "DynamicAttivitaPeriodiche",
                    Categoria = "PERIODICHE"
                };

                _context.VociMenu.Add(voceMenu);
                await _context.SaveChangesAsync();
            }
        }

        private async Task CreaMenuPerAttivitaAsync(AttivitaPeriodica attivita)
        {
            // Trova la voce padre "Attività Periodiche"
            var parentVoce = await _context.VociMenu
                .FirstOrDefaultAsync(v => v.TipoVoce == "DynamicAttivitaPeriodiche");

            if (parentVoce == null)
            {
                await EnsureMenuVoceExistsAsync();
                parentVoce = await _context.VociMenu
                    .FirstOrDefaultAsync(v => v.TipoVoce == "DynamicAttivitaPeriodiche");
            }

            if (parentVoce == null) return;

            // Verifica se esiste già una voce per questa attività
            var existingVoce = await _context.VociMenu
                .FirstOrDefaultAsync(v => v.TipoVoce == "AttivitaPeriodica" && v.ReferenceId == attivita.Id);

            if (existingVoce == null)
            {
                // Trova l'ordine per la nuova voce
                var maxOrder = await _context.VociMenu
                    .Where(v => v.ParentId == parentVoce.Id)
                    .MaxAsync(v => (int?)v.DisplayOrder) ?? 0;

                var voceMenu = new VoceMenu
                {
                    Nome = attivita.Nome,
                    Url = $"/AttivitaPeriodiche/Dati?attivitaId={attivita.Id}",
                    Icon = attivita.Icona ?? "fas fa-calendar-check",
                    ParentId = parentVoce.Id,
                    DisplayOrder = maxOrder + 1,
                    IsVisible = true,
                    IsActive = true,
                    IsGroup = false,
                    ExpandedByDefault = false,
                    TipoVoce = "AttivitaPeriodica",
                    ReferenceId = attivita.Id
                };

                _context.VociMenu.Add(voceMenu);
                await _context.SaveChangesAsync();
            }
        }

        // POST: AttivitaPeriodiche/CreaSezione
        [HttpPost]
        public async Task<IActionResult> CreaSezione(string nome, string nomePlurale, string? descrizione, 
            string icona, string colore, bool collegataACliente)
        {
            if (string.IsNullOrWhiteSpace(nome))
            {
                TempData["Error"] = "Il nome è obbligatorio.";
                return RedirectToAction(nameof(Gestione));
            }

            // Verifica duplicati
            if (await _context.AttivitaPeriodiche.AnyAsync(a => a.Nome == nome))
            {
                TempData["Error"] = $"Esiste già una sezione con nome '{nome}'.";
                return RedirectToAction(nameof(Gestione));
            }

            var maxOrder = await _context.AttivitaPeriodiche.MaxAsync(a => (int?)a.OrdineMenu) ?? 0;

            var attivita = new AttivitaPeriodica
            {
                Nome = nome,
                NomePlurale = string.IsNullOrWhiteSpace(nomePlurale) ? nome : nomePlurale,
                Descrizione = descrizione,
                Icona = string.IsNullOrWhiteSpace(icona) ? "fas fa-calendar-alt" : icona,
                Colore = string.IsNullOrWhiteSpace(colore) ? "#17a2b8" : colore,
                CollegataACliente = collegataACliente,
                OrdineMenu = maxOrder + 1,
                IsActive = true
            };

            _context.AttivitaPeriodiche.Add(attivita);
            await _context.SaveChangesAsync();

            // Crea automaticamente il permesso per questa sezione
            var pageUrl = $"/AttivitaPeriodiche/Dati/{attivita.Id}";
            if (!await _context.Permissions.AnyAsync(p => p.PageUrl == pageUrl))
            {
                var maxPermOrder = await _context.Permissions.MaxAsync(p => (int?)p.DisplayOrder) ?? 0;
                var permission = new Permission
                {
                    PageName = attivita.Nome,
                    PageUrl = pageUrl,
                    Description = $"Gestione {attivita.NomePlurale}",
                    Category = "ATTIVITA_PERIODICHE",
                    Icon = attivita.Icona,
                    ShowInMenu = false, // Mostrato dal menu dinamico
                    DisplayOrder = maxPermOrder + 1
                };
                _context.Permissions.Add(permission);
                await _context.SaveChangesAsync();
            }

            // Le voci di menu vengono generate dinamicamente da MenuService
            // Non serve creare voci manuali nel database

            TempData["Success"] = $"Sezione '{nome}' creata con successo.";
            return RedirectToAction(nameof(Configura), new { id = attivita.Id });
        }

        // GET: AttivitaPeriodiche/Configura/5
        public async Task<IActionResult> Configura(int id)
        {
            var attivita = await _context.AttivitaPeriodiche
                .Include(a => a.TipiPeriodo.Where(t => t.IsActive).OrderBy(t => t.DisplayOrder))
                    .ThenInclude(t => t.Campi.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder))
                .FirstOrDefaultAsync(a => a.Id == id);

            if (attivita == null)
            {
                TempData["Error"] = "Sezione non trovata.";
                return RedirectToAction(nameof(Gestione));
            }

            return View(attivita);
        }

        // POST: AttivitaPeriodiche/AggiornaSezione
        [HttpPost]
        public async Task<IActionResult> AggiornaSezione(int id, string nome, string nomePlurale, 
            string? descrizione, string icona, string colore, bool collegataACliente,
            int larghezzaColonnaCliente = 150, int larghezzaColonnaTitolo = 200)
        {
            var attivita = await _context.AttivitaPeriodiche.FindAsync(id);
            if (attivita == null)
            {
                TempData["Error"] = "Sezione non trovata.";
                return RedirectToAction(nameof(Gestione));
            }

            attivita.Nome = nome;
            attivita.NomePlurale = nomePlurale;
            attivita.Descrizione = descrizione;
            attivita.Icona = icona;
            attivita.Colore = colore;
            attivita.CollegataACliente = collegataACliente;
            attivita.LarghezzaColonnaCliente = larghezzaColonnaCliente;
            attivita.LarghezzaColonnaTitolo = larghezzaColonnaTitolo;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Sezione aggiornata con successo.";
            return RedirectToAction(nameof(Configura), new { id });
        }

        // POST: AttivitaPeriodiche/EliminaSezione
        [HttpPost]
        public async Task<IActionResult> EliminaSezione(int id)
        {
            var attivita = await _context.AttivitaPeriodiche.FindAsync(id);
            if (attivita == null)
            {
                TempData["Error"] = "Sezione non trovata.";
                return RedirectToAction(nameof(Gestione));
            }

            _context.AttivitaPeriodiche.Remove(attivita);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Sezione '{attivita.Nome}' eliminata.";
            return RedirectToAction(nameof(Gestione));
        }

        // ============ GESTIONE TIPI PERIODO ============

        // POST: AttivitaPeriodiche/CreaTipoPeriodo
        [HttpPost]
        public async Task<IActionResult> CreaTipoPeriodo(int attivitaPeriodicaId, string nome, int numeroPeriodi,
            string? icona, string? colore, bool mostraInteressi, decimal percentualeInteressiDefault)
        {
            if (string.IsNullOrWhiteSpace(nome))
            {
                TempData["Error"] = "Il nome è obbligatorio.";
                return RedirectToAction(nameof(Configura), new { id = attivitaPeriodicaId });
            }

            // Genera etichette e date default in base al numero periodi
            var (etichette, dateInizio, dateFine) = GeneraPeriodiDefault(numeroPeriodi);

            var maxOrder = await _context.TipiPeriodo
                .Where(t => t.AttivitaPeriodicaId == attivitaPeriodicaId)
                .MaxAsync(t => (int?)t.DisplayOrder) ?? 0;

            var tipo = new TipoPeriodo
            {
                AttivitaPeriodicaId = attivitaPeriodicaId,
                Nome = nome,
                NumeroPeriodi = numeroPeriodi,
                EtichettePeriodi = JsonSerializer.Serialize(etichette),
                DateInizioPeriodi = JsonSerializer.Serialize(dateInizio),
                DateFinePeriodi = JsonSerializer.Serialize(dateFine),
                Icona = string.IsNullOrWhiteSpace(icona) ? "fas fa-calendar" : icona,
                Colore = string.IsNullOrWhiteSpace(colore) ? "#007bff" : colore,
                MostraInteressi = mostraInteressi,
                PercentualeInteressiDefault = percentualeInteressiDefault,
                DisplayOrder = maxOrder + 1,
                IsActive = true
            };

            _context.TipiPeriodo.Add(tipo);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Tipo periodo '{nome}' creato con successo.";
            return RedirectToAction(nameof(ConfiguraTipo), new { id = tipo.Id });
        }

        // GET: AttivitaPeriodiche/ConfiguraTipo/5
        public async Task<IActionResult> ConfiguraTipo(int id)
        {
            var tipo = await _context.TipiPeriodo
                .Include(t => t.AttivitaPeriodica)
                .Include(t => t.Campi.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder))
                    .ThenInclude(c => c.Regole.Where(r => r.IsActive))
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tipo == null)
            {
                TempData["Error"] = "Tipo periodo non trovato.";
                return RedirectToAction(nameof(Gestione));
            }

            return View(tipo);
        }

        // POST: AttivitaPeriodiche/AggiornaTipoPeriodo
        [HttpPost]
        public async Task<IActionResult> AggiornaTipoPeriodo(int id, string nome, int numeroPeriodi,
            string etichettePeriodi, string dateInizioPeriodi, string dateFinePeriodi,
            string? icona, string? colore, bool mostraInteressi, decimal percentualeInteressiDefault,
            bool mostraAccordion)
        {
            var tipo = await _context.TipiPeriodo.FindAsync(id);
            if (tipo == null)
            {
                TempData["Error"] = "Tipo periodo non trovato.";
                return RedirectToAction(nameof(Gestione));
            }

            tipo.Nome = nome;
            tipo.NumeroPeriodi = numeroPeriodi;
            tipo.EtichettePeriodi = etichettePeriodi;
            tipo.DateInizioPeriodi = dateInizioPeriodi;
            tipo.DateFinePeriodi = dateFinePeriodi;
            tipo.Icona = icona ?? "fas fa-calendar";
            tipo.Colore = colore ?? "#007bff";
            tipo.MostraInteressi = mostraInteressi;
            tipo.PercentualeInteressiDefault = percentualeInteressiDefault;
            tipo.MostraAccordion = mostraAccordion;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Tipo periodo aggiornato.";
            return RedirectToAction(nameof(ConfiguraTipo), new { id });
        }

        // POST: AttivitaPeriodiche/EliminaTipoPeriodo
        [HttpPost]
        public async Task<IActionResult> EliminaTipoPeriodo(int id)
        {
            var tipo = await _context.TipiPeriodo.FindAsync(id);
            if (tipo == null)
            {
                TempData["Error"] = "Tipo periodo non trovato.";
                return RedirectToAction(nameof(Gestione));
            }

            var attivitaId = tipo.AttivitaPeriodicaId;
            _context.TipiPeriodo.Remove(tipo);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Tipo periodo '{tipo.Nome}' eliminato.";
            return RedirectToAction(nameof(Configura), new { id = attivitaId });
        }

        // ============ GESTIONE CAMPI ============

        // POST: AttivitaPeriodiche/CreaCampo
        [HttpPost]
        public async Task<IActionResult> CreaCampo(CampoPeriodico campo)
        {
            if (string.IsNullOrWhiteSpace(campo.Nome) || string.IsNullOrWhiteSpace(campo.Label))
            {
                TempData["Error"] = "Nome e etichetta sono obbligatori.";
                return RedirectToAction(nameof(ConfiguraTipo), new { id = campo.TipoPeriodoId });
            }

            // Verifica duplicati
            if (await _context.CampiPeriodici.AnyAsync(c => c.TipoPeriodoId == campo.TipoPeriodoId && c.Nome == campo.Nome))
            {
                TempData["Error"] = $"Esiste già un campo con nome '{campo.Nome}'.";
                return RedirectToAction(nameof(ConfiguraTipo), new { id = campo.TipoPeriodoId });
            }

            var maxOrder = await _context.CampiPeriodici
                .Where(c => c.TipoPeriodoId == campo.TipoPeriodoId)
                .MaxAsync(c => (int?)c.DisplayOrder) ?? 0;

            campo.DisplayOrder = maxOrder + 1;
            campo.IsActive = true;

            _context.CampiPeriodici.Add(campo);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Campo '{campo.Label}' creato.";
            return RedirectToAction(nameof(ConfiguraTipo), new { id = campo.TipoPeriodoId });
        }

        // POST: AttivitaPeriodiche/AggiornaCampo
        [HttpPost]
        public async Task<IActionResult> AggiornaCampo(int id, string label, string? labelPrimoPeriodo,
            string tipoCampo, bool isRequired, bool showInList, bool useAsFilter, bool isCampoCliente,
            bool isCompletionIndicator, bool isResultIndicator,
            string? options, string? defaultValue, string? placeholder,
            int colWidth, int columnWidth, bool isCalculated, string? formula, string? periodiVisibili)
        {
            var campo = await _context.CampiPeriodici.FindAsync(id);
            if (campo == null)
            {
                TempData["Error"] = "Campo non trovato.";
                return RedirectToAction(nameof(Gestione));
            }

            campo.Label = label;
            campo.LabelPrimoPeriodo = labelPrimoPeriodo;
            campo.TipoCampo = tipoCampo;
            campo.IsRequired = isRequired;
            campo.ShowInList = showInList;
            campo.UseAsFilter = useAsFilter;
            campo.IsCampoCliente = isCampoCliente;
            campo.IsCompletionIndicator = isCompletionIndicator;
            campo.IsResultIndicator = isResultIndicator;
            campo.Options = options;
            campo.DefaultValue = defaultValue;
            campo.Placeholder = placeholder;
            campo.ColWidth = colWidth;
            campo.ColumnWidth = columnWidth;
            campo.IsCalculated = isCalculated;
            campo.Formula = isCalculated ? formula : null;
            campo.PeriodiVisibili = string.IsNullOrWhiteSpace(periodiVisibili) ? null : periodiVisibili;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Campo '{campo.Label}' aggiornato.";
            return RedirectToAction(nameof(ConfiguraTipo), new { id = campo.TipoPeriodoId });
        }

        // POST: AttivitaPeriodiche/EliminaCampo
        [HttpPost]
        public async Task<IActionResult> EliminaCampo(int id)
        {
            var campo = await _context.CampiPeriodici.FindAsync(id);
            if (campo == null)
            {
                TempData["Error"] = "Campo non trovato.";
                return RedirectToAction(nameof(Gestione));
            }

            var tipoPeriodoId = campo.TipoPeriodoId;
            _context.CampiPeriodici.Remove(campo);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Campo '{campo.Label}' eliminato.";
            return RedirectToAction(nameof(ConfiguraTipo), new { id = tipoPeriodoId });
        }

        // POST: Move campo up/down
        [HttpPost]
        public async Task<IActionResult> MoveCampoUp(int id)
        {
            var campo = await _context.CampiPeriodici.FindAsync(id);
            if (campo == null) return RedirectToAction(nameof(Gestione));

            var campoPrecedente = await _context.CampiPeriodici
                .Where(c => c.TipoPeriodoId == campo.TipoPeriodoId && c.DisplayOrder < campo.DisplayOrder)
                .OrderByDescending(c => c.DisplayOrder)
                .FirstOrDefaultAsync();

            if (campoPrecedente != null)
            {
                (campo.DisplayOrder, campoPrecedente.DisplayOrder) = (campoPrecedente.DisplayOrder, campo.DisplayOrder);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(ConfiguraTipo), new { id = campo.TipoPeriodoId });
        }

        [HttpPost]
        public async Task<IActionResult> MoveCampoDown(int id)
        {
            var campo = await _context.CampiPeriodici.FindAsync(id);
            if (campo == null) return RedirectToAction(nameof(Gestione));

            var campoSuccessivo = await _context.CampiPeriodici
                .Where(c => c.TipoPeriodoId == campo.TipoPeriodoId && c.DisplayOrder > campo.DisplayOrder)
                .OrderBy(c => c.DisplayOrder)
                .FirstOrDefaultAsync();

            if (campoSuccessivo != null)
            {
                (campo.DisplayOrder, campoSuccessivo.DisplayOrder) = (campoSuccessivo.DisplayOrder, campo.DisplayOrder);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(ConfiguraTipo), new { id = campo.TipoPeriodoId });
        }

        // ============ GESTIONE REGOLE ============

        // GET: AttivitaPeriodiche/ConfiguraRegole/5 (tipoPeriodoId)
        public async Task<IActionResult> ConfiguraRegole(int id)
        {
            var tipo = await _context.TipiPeriodo
                .Include(t => t.AttivitaPeriodica)
                .Include(t => t.Campi.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder))
                    .ThenInclude(c => c.Regole.Where(r => r.IsActive))
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tipo == null)
            {
                TempData["Error"] = "Tipo periodo non trovato.";
                return RedirectToAction(nameof(Gestione));
            }

            return View(tipo);
        }

        // POST: AttivitaPeriodiche/CreaRegola
        [HttpPost]
        public async Task<IActionResult> CreaRegola(RegolaCampo regola, string? ValoreConfrontoRiporto)
        {
            if (regola.CampoPeriodicoId == 0)
            {
                TempData["Error"] = "Campo non valido.";
                return RedirectToAction(nameof(Gestione));
            }

            var campo = await _context.CampiPeriodici
                .Include(c => c.TipoPeriodo)
                .FirstOrDefaultAsync(c => c.Id == regola.CampoPeriodicoId);

            if (campo == null)
            {
                TempData["Error"] = "Campo non trovato.";
                return RedirectToAction(nameof(Gestione));
            }

            // Per le regole di riporto: CampoPeriodicoId è l'origine, imposta anche CampoOrigineId
            if (regola.TipoRegola == "riporto")
            {
                regola.CampoOrigineId = regola.CampoPeriodicoId;
                // Il valore confronto per riporto arriva come ValoreConfrontoRiporto
                if (!string.IsNullOrEmpty(ValoreConfrontoRiporto))
                {
                    regola.ValoreConfronto = ValoreConfrontoRiporto;
                }
            }

            regola.IsActive = true;
            _context.RegoleCampi.Add(regola);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Regola creata per campo '{campo.Label}'.";
            return RedirectToAction(nameof(ConfiguraRegole), new { id = campo.TipoPeriodoId });
        }

        // POST: AttivitaPeriodiche/AggiornaRegola
        [HttpPost]
        public async Task<IActionResult> AggiornaRegola(int id, string tipoRegola,
            int? campoOrigineId, int? campoDestinazioneId, string? condizioneRiporto,
            string? operatore, string? valoreConfronto, string? coloreTesto, string? coloreSfondo,
            bool grassetto, string? icona, string applicaA, int priorita)
        {
            var regola = await _context.RegoleCampi
                .Include(r => r.CampoPeriodico)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (regola == null)
            {
                TempData["Error"] = "Regola non trovata.";
                return RedirectToAction(nameof(Gestione));
            }

            regola.TipoRegola = tipoRegola;
            regola.CampoOrigineId = campoOrigineId;
            regola.CampoDestinazioneId = campoDestinazioneId;
            regola.CondizioneRiporto = condizioneRiporto;
            regola.Operatore = operatore;
            regola.ValoreConfronto = valoreConfronto;
            regola.ColoreTesto = coloreTesto;
            regola.ColoreSfondo = coloreSfondo;
            regola.Grassetto = grassetto;
            regola.Icona = icona;
            regola.ApplicaA = applicaA;
            regola.Priorita = priorita;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Regola aggiornata.";
            return RedirectToAction(nameof(ConfiguraRegole), new { id = regola.CampoPeriodico?.TipoPeriodoId });
        }

        // POST: AttivitaPeriodiche/EliminaRegola
        [HttpPost]
        public async Task<IActionResult> EliminaRegola(int id)
        {
            var regola = await _context.RegoleCampi
                .Include(r => r.CampoPeriodico)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (regola == null)
            {
                TempData["Error"] = "Regola non trovata.";
                return RedirectToAction(nameof(Gestione));
            }

            var tipoPeriodoId = regola.CampoPeriodico?.TipoPeriodoId;
            _context.RegoleCampi.Remove(regola);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Regola eliminata.";
            return RedirectToAction(nameof(ConfiguraRegole), new { id = tipoPeriodoId });
        }

        // POST: AttivitaPeriodiche/RicalcolaRiporti (AJAX o POST)
        // IMPORTANTE: Calcolo SEQUENZIALE dal periodo 1 in avanti
        // 1. Calcola Liquidazione periodo 1
        // 2. Se Liquidazione > 0, riporta a Credito Prec. periodo 2, altrimenti azzera
        // 3. Calcola Liquidazione periodo 2
        // 4. Se Liquidazione > 0, riporta a Credito Prec. periodo 3, altrimenti azzera
        // ... e così via
        [HttpPost]
        public async Task<IActionResult> RicalcolaRiporti([FromBody] RicalcolaRiportiRequest? request)
        {
            if (request == null || request.ClienteAttivitaId == 0)
            {
                return Json(new { success = false, error = "ID cliente non valido" });
            }

            var clienteAttivitaId = request.ClienteAttivitaId;
            var clienteAttivita = await _context.ClientiAttivitaPeriodiche
                .Include(c => c.TipoPeriodo)
                    .ThenInclude(t => t!.Campi.Where(c => c.IsActive))
                        .ThenInclude(c => c.Regole.Where(r => r.IsActive && r.TipoRegola == "riporto"))
                .Include(c => c.ValoriPeriodi)
                .FirstOrDefaultAsync(c => c.Id == clienteAttivitaId);

            if (clienteAttivita?.TipoPeriodo == null)
            {
                return Json(new { success = false, error = "Cliente/attività non trovato" });
            }

            // Ordina i periodi dal primo all'ultimo
            var periodi = clienteAttivita.ValoriPeriodi.OrderBy(v => v.NumeroPeriodo).ToList();
            var regoleRiporto = clienteAttivita.TipoPeriodo.Campi
                .SelectMany(c => c.Regole.Where(r => r.TipoRegola == "riporto"))
                .ToList();

            // STEP 1: Calcola il primo periodo (non ha riporti da periodi precedenti)
            if (periodi.Any())
            {
                await RicalcolaCampiAsync(clienteAttivitaId, periodi[0].NumeroPeriodo);
            }

            // STEP 2: Per ogni periodo successivo, PRIMA applica i riporti, POI calcola
            for (int i = 1; i < periodi.Count; i++)
            {
                var periodoPrecedente = periodi[i - 1];
                var periodoCorrente = periodi[i];

                // Ricarica i valori aggiornati del periodo precedente (dopo il calcolo)
                var valoriPrec = JsonSerializer.Deserialize<Dictionary<string, string>>(periodoPrecedente.Valori) 
                                 ?? new Dictionary<string, string>();
                var calcolatiPrec = JsonSerializer.Deserialize<Dictionary<string, string>>(periodoPrecedente.ValoriCalcolati) 
                                    ?? new Dictionary<string, string>();
                var valoriCorr = JsonSerializer.Deserialize<Dictionary<string, string>>(periodoCorrente.Valori) 
                                 ?? new Dictionary<string, string>();

                bool modificato = false;

                foreach (var regola in regoleRiporto)
                {
                    var campoOrigineId = regola.CampoOrigineId ?? regola.CampoPeriodicoId;
                    var campoDestId = regola.CampoDestinazioneId ?? regola.CampoPeriodicoId;

                    var campoOrigine = clienteAttivita.TipoPeriodo.Campi.FirstOrDefault(c => c.Id == campoOrigineId);
                    var campoDest = clienteAttivita.TipoPeriodo.Campi.FirstOrDefault(c => c.Id == campoDestId);

                    if (campoOrigine == null || campoDest == null || campoDest.IsCalculated) continue;

                    // Prendi il valore CALCOLATO dal periodo precedente
                    string? valoreOrigine = null;
                    if (campoOrigine.IsCalculated)
                    {
                        calcolatiPrec.TryGetValue(campoOrigine.Nome, out valoreOrigine);
                    }
                    else
                    {
                        valoriPrec.TryGetValue(campoOrigine.Nome, out valoreOrigine);
                    }

                    // Parse del valore
                    decimal valNum = 0;
                    if (!string.IsNullOrEmpty(valoreOrigine))
                    {
                        decimal.TryParse(valoreOrigine.Replace(",", "."), 
                            System.Globalization.NumberStyles.Any, 
                            System.Globalization.CultureInfo.InvariantCulture, out valNum);
                    }

                    // Parse del valore confronto
                    decimal valConfronto = 0;
                    if (!string.IsNullOrEmpty(regola.ValoreConfronto))
                    {
                        decimal.TryParse(regola.ValoreConfronto.Replace(",", "."), 
                            System.Globalization.NumberStyles.Any, 
                            System.Globalization.CultureInfo.InvariantCulture, out valConfronto);
                    }

                    // Verifica condizione
                    bool applicaRiporto = regola.CondizioneRiporto switch
                    {
                        ">" => valNum > valConfronto,
                        ">=" => valNum >= valConfronto,
                        "<" => valNum < valConfronto,
                        "<=" => valNum <= valConfronto,
                        "=" => valNum == valConfronto,
                        "!=" => valNum != valConfronto,
                        "se_positivo" or "maggiore_zero" => valNum > 0,
                        "se_negativo" or "minore_zero" => valNum < 0,
                        "se_diverso_zero" or "diverso_zero" => valNum != 0,
                        "sempre" => true,
                        _ => true
                    };

                    if (applicaRiporto && valNum != 0)
                    {
                        // Riporta il valore
                        valoriCorr[campoDest.Nome] = valNum.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        modificato = true;
                    }
                    else
                    {
                        // Condizione NON soddisfatta: azzera il campo destinazione
                        valoriCorr[campoDest.Nome] = "0";
                        modificato = true;
                    }
                }

                if (modificato)
                {
                    periodoCorrente.Valori = JsonSerializer.Serialize(valoriCorr);
                    periodoCorrente.DataAggiornamento = DateTime.Now;
                    await _context.SaveChangesAsync();
                }

                // DOPO aver applicato i riporti, calcola i campi calcolati del periodo corrente
                await RicalcolaCampiAsync(clienteAttivitaId, periodoCorrente.NumeroPeriodo);
            }

            return Json(new { success = true, message = "Riporti ricalcolati in sequenza dal periodo 1" });
        }

        // POST: AttivitaPeriodiche/CancellaDati (AJAX)
        // Cancella i dati di un singolo periodo E TUTTI I PERIODI SUCCESSIVI (a cascata)
        // Es: Se cancello il 1° trimestre, cancella anche 2°, 3°, 4° trimestre
        //     Se cancello il 2° trimestre, cancella anche 3°, 4° trimestre (lascia il 1°)
        // DOPO LA CANCELLAZIONE: Ricalcola i riporti dal periodo precedente per riportare i dati corretti
        [HttpPost]
        public async Task<IActionResult> CancellaDati([FromBody] CancellaDatiRequest? request)
        {
            if (request == null || request.ClienteAttivitaId == 0)
            {
                return Json(new { success = false, error = "Richiesta non valida" });
            }

            var clienteAttivita = await _context.ClientiAttivitaPeriodiche
                .Include(c => c.ValoriPeriodi)
                .Include(c => c.TipoPeriodo)
                    .ThenInclude(t => t!.Campi.Where(c => c.IsActive))
                        .ThenInclude(c => c.Regole.Where(r => r.IsActive))
                .FirstOrDefaultAsync(c => c.Id == request.ClienteAttivitaId);

            if (clienteAttivita == null)
            {
                return Json(new { success = false, error = "Cliente non trovato" });
            }

            int periodiCancellati = 0;
            int periodoPrecedente = 0; // Per sapere da dove ricalcolare i riporti

            if (request.NumeroPeriodo == 0)
            {
                // Cancella TUTTI i periodi
                foreach (var periodo in clienteAttivita.ValoriPeriodi)
                {
                    periodo.Valori = "{}";
                    periodo.ValoriCalcolati = "{}";
                    periodo.DataAggiornamento = DateTime.Now;
                    periodiCancellati++;
                }
            }
            else
            {
                // Cancella il periodo specificato E TUTTI I PERIODI SUCCESSIVI (a cascata)
                // Es: Se cancello periodo 2, cancello anche 3, 4, 5... ecc.
                var periodiDaCancellare = clienteAttivita.ValoriPeriodi
                    .Where(v => v.NumeroPeriodo >= request.NumeroPeriodo)
                    .ToList();
                
                foreach (var periodo in periodiDaCancellare)
                {
                    periodo.Valori = "{}";
                    periodo.ValoriCalcolati = "{}";
                    periodo.DataAggiornamento = DateTime.Now;
                    periodiCancellati++;
                }

                // Salva il periodo precedente per il ricalcolo riporti
                periodoPrecedente = request.NumeroPeriodo - 1;
            }

            await _context.SaveChangesAsync();

            // DOPO LA CANCELLAZIONE: Ricalcola i riporti A CASCATA dal periodo precedente fino all'ultimo
            // Es: Se cancello dal periodo 2, ricalcola: P1 → riporta a P2 → calcola P2 → riporta a P3 → calcola P3 → riporta a P4 → calcola P4
            if (periodoPrecedente > 0 && clienteAttivita.TipoPeriodo != null)
            {
                int numeroPeriodi = clienteAttivita.TipoPeriodo.NumeroPeriodi;
                
                // Ricalcola i campi calcolati del periodo precedente (per avere i valori aggiornati)
                await RicalcolaCampiAsync(request.ClienteAttivitaId, periodoPrecedente);

                // Ciclo a cascata: per ogni periodo dal cancellato fino all'ultimo
                for (int p = request.NumeroPeriodo; p <= numeroPeriodi; p++)
                {
                    // Applica i riporti dal periodo precedente al periodo corrente
                    await ApplicaRiportiAsync(request.ClienteAttivitaId, p - 1);
                    
                    // Ricalcola i campi calcolati del periodo corrente
                    await RicalcolaCampiAsync(request.ClienteAttivitaId, p);
                }
            }

            var messaggio = request.NumeroPeriodo == 0 
                ? "Tutti i dati cancellati" 
                : periodiCancellati > 1
                    ? $"Dati dal periodo {request.NumeroPeriodo} in poi cancellati ({periodiCancellati} periodi) - Riporti ricalcolati"
                    : $"Dati del periodo {request.NumeroPeriodo} cancellati - Riporti ricalcolati";

            return Json(new { success = true, message = messaggio });
        }

        // GET: AttivitaPeriodiche/GetRegoleColore (AJAX per la griglia)
        [HttpGet]
        public async Task<IActionResult> GetRegoleColore(int tipoPeriodoId)
        {
            var regole = await _context.RegoleCampi
                .Include(r => r.CampoPeriodico)
                .Where(r => r.CampoPeriodico!.TipoPeriodoId == tipoPeriodoId 
                         && r.IsActive 
                         && r.TipoRegola == "colore")
                .Select(r => new {
                    campoNome = r.CampoPeriodico!.Nome,
                    operatore = r.Operatore,
                    valoreConfronto = r.ValoreConfronto,
                    coloreTesto = r.ColoreTesto,
                    coloreSfondo = r.ColoreSfondo,
                    grassetto = r.Grassetto,
                    icona = r.Icona,
                    applicaA = r.ApplicaA,
                    priorita = r.Priorita
                })
                .OrderBy(r => r.priorita)
                .ToListAsync();

            return Json(regole);
        }

        // ============ VISUALIZZAZIONE DATI ============

        // GET: AttivitaPeriodiche/Dati/5
        public async Task<IActionResult> Dati(int id, int? tipoPeriodoId, int anno = 0, string? search = null, int? clienteId = null)
        {
            // Verifica permessi: accetta sia permesso specifico che generale
            var pageUrlSpecific = $"/AttivitaPeriodiche/Dati/{id}";
            var pageUrlGeneral = "/AttivitaPeriodiche";
            if (!await CanAccessAsync(pageUrlSpecific) && !await CanAccessAsync(pageUrlGeneral))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var attivita = await _context.AttivitaPeriodiche
                .Include(a => a.TipiPeriodo.Where(t => t.IsActive).OrderBy(t => t.DisplayOrder))
                    .ThenInclude(t => t.Campi.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder))
                .FirstOrDefaultAsync(a => a.Id == id);

            if (attivita == null)
            {
                TempData["Error"] = "Sezione non trovata.";
                return RedirectToAction(nameof(Gestione));
            }

            if (!attivita.TipiPeriodo.Any())
            {
                TempData["Error"] = "Configura almeno un tipo periodo prima di inserire dati.";
                return RedirectToAction(nameof(Configura), new { id });
            }

            // Carica le annualità fiscali esistenti
            var annualita = await _context.AnnualitaFiscali
                .Where(a => a.IsActive)
                .OrderByDescending(a => a.Anno)
                .ToListAsync();

            // Anno default: anno corrente se esiste, altrimenti primo disponibile
            if (anno == 0)
            {
                var annoCorrente = annualita.FirstOrDefault(a => a.IsCurrent);
                anno = annoCorrente?.Anno ?? annualita.FirstOrDefault()?.Anno ?? DateTime.Now.Year;
            }

            // Tipo periodo default: il primo disponibile
            var tipoSelezionato = tipoPeriodoId.HasValue
                ? attivita.TipiPeriodo.FirstOrDefault(t => t.Id == tipoPeriodoId.Value)
                : attivita.TipiPeriodo.First();

            if (tipoSelezionato == null)
            {
                TempData["Error"] = "Tipo periodo non trovato.";
                return RedirectToAction(nameof(Configura), new { id });
            }

            // Carica clienti associati a questo tipo periodo per l'anno selezionato
            var clientiAssociatiQuery = _context.ClientiAttivitaPeriodiche
                .Include(c => c.Cliente)
                .Include(c => c.ValoriPeriodi)
                .Where(c => c.AttivitaPeriodicaId == id 
                         && c.TipoPeriodoId == tipoSelezionato.Id 
                         && c.AnnoFiscale == anno
                         && c.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
            {
                clientiAssociatiQuery = clientiAssociatiQuery
                    .Where(c => c.Cliente != null && c.Cliente.RagioneSociale.Contains(search));
            }

            var clientiAssociati = await clientiAssociatiQuery
                .OrderBy(c => c.Cliente!.RagioneSociale)
                .ToListAsync();

            // Carica lista clienti disponibili per aggiunta
            var clientiIds = clientiAssociati.Select(c => c.ClienteId).ToList();
            var clientiDisponibili = await _context.Clienti
                .Where(c => c.IsActive && !clientiIds.Contains(c.Id))
                .OrderBy(c => c.RagioneSociale)
                .Select(c => new { c.Id, c.RagioneSociale })
                .ToListAsync();

            // Parse etichette periodi
            var etichette = JsonSerializer.Deserialize<List<string>>(tipoSelezionato.EtichettePeriodi) ?? new List<string>();

            ViewBag.Attivita = attivita;
            ViewBag.TipoSelezionato = tipoSelezionato;
            ViewBag.Anno = anno;
            ViewBag.Search = search;
            ViewBag.Etichette = etichette;
            ViewBag.ClientiDisponibili = clientiDisponibili;
            ViewBag.Campi = tipoSelezionato.Campi.ToList();
            ViewBag.Annualita = annualita;
            ViewBag.ClienteIdDaAprire = clienteId;

            // Carica i campi che hanno regole di riporto (per renderli readonly dal periodo 2 in poi)
            var campiConRiporto = await _context.RegoleCampi
                .Where(r => r.IsActive && r.TipoRegola == "riporto" && r.CampoDestinazioneId != null)
                .Select(r => r.CampoDestinazioneId!.Value)
                .Distinct()
                .ToListAsync();
            
            // Mappa nome campo -> ha riporto
            var campiConRiportoNomi = await _context.CampiPeriodici
                .Where(c => campiConRiporto.Contains(c.Id))
                .Select(c => c.Nome)
                .ToListAsync();
            ViewBag.CampiConRiporto = campiConRiportoNomi;

            return View(clientiAssociati);
        }

        // POST: AttivitaPeriodiche/AggiungiCliente
        [HttpPost]
        public async Task<IActionResult> AggiungiCliente(int attivitaPeriodicaId, int tipoPeriodoId, int clienteId, int anno)
        {
            // Verifica se esiste già
            var exists = await _context.ClientiAttivitaPeriodiche
                .AnyAsync(c => c.TipoPeriodoId == tipoPeriodoId && c.ClienteId == clienteId && c.AnnoFiscale == anno);

            if (exists)
            {
                TempData["Error"] = "Il cliente è già associato a questa attività per l'anno selezionato.";
                return RedirectToAction(nameof(Dati), new { id = attivitaPeriodicaId, tipoPeriodoId, anno });
            }

            var nuovaAssociazione = new ClienteAttivitaPeriodica
            {
                AttivitaPeriodicaId = attivitaPeriodicaId,
                TipoPeriodoId = tipoPeriodoId,
                ClienteId = clienteId,
                AnnoFiscale = anno,
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.ClientiAttivitaPeriodiche.Add(nuovaAssociazione);
            await _context.SaveChangesAsync();

            // Crea record ValorePeriodo vuoti per tutti i periodi
            var tipo = await _context.TipiPeriodo.FindAsync(tipoPeriodoId);
            if (tipo != null)
            {
                for (int i = 1; i <= tipo.NumeroPeriodi; i++)
                {
                    var valore = new ValorePeriodo
                    {
                        ClienteAttivitaPeriodicaId = nuovaAssociazione.Id,
                        NumeroPeriodo = i,
                        Valori = "{}",
                        ValoriCalcolati = "{}"
                    };
                    _context.ValoriPeriodi.Add(valore);
                }
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Cliente aggiunto con successo.";
            return RedirectToAction(nameof(Dati), new { id = attivitaPeriodicaId, tipoPeriodoId, anno });
        }

        // POST: AttivitaPeriodiche/AggiungiClienti (multipli)
        [HttpPost]
        public async Task<IActionResult> AggiungiClienti(int attivitaPeriodicaId, int tipoPeriodoId, int[] clienteIds, int anno)
        {
            if (clienteIds == null || clienteIds.Length == 0)
            {
                TempData["Error"] = "Seleziona almeno un cliente.";
                return RedirectToAction(nameof(Dati), new { id = attivitaPeriodicaId, tipoPeriodoId, anno });
            }

            var tipo = await _context.TipiPeriodo.FindAsync(tipoPeriodoId);
            int aggiunti = 0;
            int giàPresenti = 0;

            foreach (var clienteId in clienteIds)
            {
                // Verifica se esiste già
                var exists = await _context.ClientiAttivitaPeriodiche
                    .AnyAsync(c => c.TipoPeriodoId == tipoPeriodoId && c.ClienteId == clienteId && c.AnnoFiscale == anno);

                if (exists)
                {
                    giàPresenti++;
                    continue;
                }

                var nuovaAssociazione = new ClienteAttivitaPeriodica
                {
                    AttivitaPeriodicaId = attivitaPeriodicaId,
                    TipoPeriodoId = tipoPeriodoId,
                    ClienteId = clienteId,
                    AnnoFiscale = anno,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.ClientiAttivitaPeriodiche.Add(nuovaAssociazione);
                await _context.SaveChangesAsync();

                // Crea record ValorePeriodo vuoti per tutti i periodi
                if (tipo != null)
                {
                    for (int i = 1; i <= tipo.NumeroPeriodi; i++)
                    {
                        var valore = new ValorePeriodo
                        {
                            ClienteAttivitaPeriodicaId = nuovaAssociazione.Id,
                            NumeroPeriodo = i,
                            Valori = "{}",
                            ValoriCalcolati = "{}"
                        };
                        _context.ValoriPeriodi.Add(valore);
                    }
                    await _context.SaveChangesAsync();
                }

                aggiunti++;
            }

            if (aggiunti > 0)
            {
                var msg = $"{aggiunti} client{(aggiunti == 1 ? "e aggiunto" : "i aggiunti")} con successo.";
                if (giàPresenti > 0)
                {
                    msg += $" ({giàPresenti} già present{(giàPresenti == 1 ? "e" : "i")})";
                }
                TempData["Success"] = msg;
            }
            else if (giàPresenti > 0)
            {
                TempData["Warning"] = $"Tutti i {giàPresenti} clienti selezionati erano già presenti.";
            }

            return RedirectToAction(nameof(Dati), new { id = attivitaPeriodicaId, tipoPeriodoId, anno });
        }

        // POST: AttivitaPeriodiche/AssegnaClienteDaDettaglio
        // Assegna un cliente a più tipi periodo dalla pagina dettagli cliente
        [HttpPost]
        public async Task<IActionResult> AssegnaClienteDaDettaglio(int clienteId, int[] tipiPeriodoIds, int annoFiscale)
        {
            if (tipiPeriodoIds == null || tipiPeriodoIds.Length == 0)
            {
                TempData["Error"] = "Seleziona almeno un tipo di periodo.";
                return RedirectToAction("Details", "Clienti", new { id = clienteId });
            }

            int aggiunti = 0;
            
            foreach (var tipoPeriodoId in tipiPeriodoIds)
            {
                var tipo = await _context.TipiPeriodo
                    .Include(t => t.AttivitaPeriodica)
                    .FirstOrDefaultAsync(t => t.Id == tipoPeriodoId);
                
                if (tipo == null) continue;

                // Verifica se esiste già
                var exists = await _context.ClientiAttivitaPeriodiche
                    .AnyAsync(c => c.TipoPeriodoId == tipoPeriodoId && c.ClienteId == clienteId && c.AnnoFiscale == annoFiscale);

                if (exists) continue;

                var nuovaAssociazione = new ClienteAttivitaPeriodica
                {
                    AttivitaPeriodicaId = tipo.AttivitaPeriodicaId,
                    TipoPeriodoId = tipoPeriodoId,
                    ClienteId = clienteId,
                    AnnoFiscale = annoFiscale,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.ClientiAttivitaPeriodiche.Add(nuovaAssociazione);
                await _context.SaveChangesAsync();

                // Crea record ValorePeriodo vuoti
                for (int i = 1; i <= tipo.NumeroPeriodi; i++)
                {
                    _context.ValoriPeriodi.Add(new ValorePeriodo
                    {
                        ClienteAttivitaPeriodicaId = nuovaAssociazione.Id,
                        NumeroPeriodo = i,
                        Valori = "{}",
                        ValoriCalcolati = "{}"
                    });
                }
                await _context.SaveChangesAsync();

                aggiunti++;
            }

            if (aggiunti > 0)
            {
                TempData["Success"] = $"{aggiunti} attività periodiche assegnate con successo.";
            }
            else
            {
                TempData["Warning"] = "Le attività selezionate erano già assegnate.";
            }

            return RedirectToAction("Details", "Clienti", new { id = clienteId });
        }

        // POST: AttivitaPeriodiche/RimuoviCliente
        [HttpPost]
        public async Task<IActionResult> RimuoviCliente(int id, int attivitaPeriodicaId, int tipoPeriodoId, int anno)
        {
            var associazione = await _context.ClientiAttivitaPeriodiche
                .Include(c => c.ValoriPeriodi)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (associazione != null)
            {
                // Rimuovi tutti i valori periodo
                _context.ValoriPeriodi.RemoveRange(associazione.ValoriPeriodi);
                _context.ClientiAttivitaPeriodiche.Remove(associazione);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Cliente rimosso.";
            }

            return RedirectToAction(nameof(Dati), new { id = attivitaPeriodicaId, tipoPeriodoId, anno });
        }

        // POST: AttivitaPeriodiche/SalvaValore (AJAX)
        [HttpPost]
        public async Task<IActionResult> SalvaValore([FromBody] SalvaValoreRequest request)
        {
            try
            {
                var valorePeriodo = await _context.ValoriPeriodi
                    .FirstOrDefaultAsync(v => v.ClienteAttivitaPeriodicaId == request.ClienteAttivitaId 
                                           && v.NumeroPeriodo == request.NumeroPeriodo);

                if (valorePeriodo == null)
                {
                    // Crea nuovo record
                    valorePeriodo = new ValorePeriodo
                    {
                        ClienteAttivitaPeriodicaId = request.ClienteAttivitaId,
                        NumeroPeriodo = request.NumeroPeriodo,
                        Valori = "{}",
                        ValoriCalcolati = "{}"
                    };
                    _context.ValoriPeriodi.Add(valorePeriodo);
                }

                // Aggiorna il valore del campo specifico
                var valori = JsonSerializer.Deserialize<Dictionary<string, string>>(valorePeriodo.Valori) 
                             ?? new Dictionary<string, string>();
                valori[request.NomeCampo] = request.Valore;
                valorePeriodo.Valori = JsonSerializer.Serialize(valori);
                valorePeriodo.DataAggiornamento = DateTime.Now;

                await _context.SaveChangesAsync();

                // Ricalcola campi calcolati per questo periodo
                var valoriCalcolati = await RicalcolaCampiAsync(request.ClienteAttivitaId, request.NumeroPeriodo);

                // Applica regole di riporto al periodo successivo
                await ApplicaRiportiAsync(request.ClienteAttivitaId, request.NumeroPeriodo);

                return Json(new { success = true, valoriCalcolati });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // Applica le regole di riporto dal periodo corrente al periodo successivo
        private async Task ApplicaRiportiAsync(int clienteAttivitaId, int numeroPeriodoCorrente)
        {
            var clienteAttivita = await _context.ClientiAttivitaPeriodiche
                .Include(c => c.TipoPeriodo)
                    .ThenInclude(t => t!.Campi.Where(c => c.IsActive))
                        .ThenInclude(c => c.Regole.Where(r => r.IsActive && r.TipoRegola == "riporto"))
                .Include(c => c.ValoriPeriodi)
                .FirstOrDefaultAsync(c => c.Id == clienteAttivitaId);

            if (clienteAttivita?.TipoPeriodo == null) return;

            var periodoCorrente = clienteAttivita.ValoriPeriodi.FirstOrDefault(v => v.NumeroPeriodo == numeroPeriodoCorrente);
            var periodoSuccessivo = clienteAttivita.ValoriPeriodi.FirstOrDefault(v => v.NumeroPeriodo == numeroPeriodoCorrente + 1);

            if (periodoCorrente == null || periodoSuccessivo == null) return;

            var valoriCorrente = JsonSerializer.Deserialize<Dictionary<string, string>>(periodoCorrente.Valori) ?? new Dictionary<string, string>();
            var calcolatiCorrente = JsonSerializer.Deserialize<Dictionary<string, string>>(periodoCorrente.ValoriCalcolati) ?? new Dictionary<string, string>();
            var valoriSuccessivo = JsonSerializer.Deserialize<Dictionary<string, string>>(periodoSuccessivo.Valori) ?? new Dictionary<string, string>();

            bool modificato = false;

            // Per ogni campo con regole di riporto
            foreach (var campo in clienteAttivita.TipoPeriodo.Campi)
            {
                foreach (var regola in campo.Regole.Where(r => r.TipoRegola == "riporto" && r.CampoDestinazioneId.HasValue))
                {
                    // Usa CampoOrigineId se impostato, altrimenti fallback a CampoPeriodicoId
                    var origineId = regola.CampoOrigineId ?? regola.CampoPeriodicoId;
                    var campoOrigine = clienteAttivita.TipoPeriodo.Campi.FirstOrDefault(c => c.Id == origineId);
                    var campoDestinazione = clienteAttivita.TipoPeriodo.Campi.FirstOrDefault(c => c.Id == regola.CampoDestinazioneId);

                    if (campoOrigine == null || campoDestinazione == null) continue;

                    // Prendi il valore origine (può essere calcolato o input)
                    string? valoreOrigine = null;
                    if (campoOrigine.IsCalculated)
                    {
                        calcolatiCorrente.TryGetValue(campoOrigine.Nome, out valoreOrigine);
                    }
                    else
                    {
                        valoriCorrente.TryGetValue(campoOrigine.Nome, out valoreOrigine);
                    }

                    // Verifica condizione
                    bool deveRiportare = true;
                    if (!string.IsNullOrEmpty(regola.CondizioneRiporto) && regola.CondizioneRiporto != "sempre")
                    {
                        decimal.TryParse(NormalizzaValore(valoreOrigine), System.Globalization.NumberStyles.Any, 
                            System.Globalization.CultureInfo.InvariantCulture, out decimal valNum);
                        decimal.TryParse(regola.ValoreConfronto ?? "0", System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out decimal valConfronto);
                        
                        deveRiportare = regola.CondizioneRiporto switch
                        {
                            // Nuovi operatori
                            ">" => valNum > valConfronto,
                            ">=" => valNum >= valConfronto,
                            "<" => valNum < valConfronto,
                            "<=" => valNum <= valConfronto,
                            "=" => valNum == valConfronto,
                            "!=" => valNum != valConfronto,
                            // Vecchi operatori (retrocompatibilità)
                            "se_positivo" or "maggiore_zero" => valNum > 0,
                            "se_negativo" or "minore_zero" => valNum < 0,
                            "se_diverso_zero" or "diverso_zero" => valNum != 0,
                            _ => true
                        };
                    }

                    if (deveRiportare && !string.IsNullOrEmpty(valoreOrigine))
                    {
                        valoriSuccessivo[campoDestinazione.Nome] = valoreOrigine;
                        modificato = true;
                    }
                    else
                    {
                        // Se la condizione non è soddisfatta, rimuovi/azzera il valore precedente
                        if (valoriSuccessivo.ContainsKey(campoDestinazione.Nome))
                        {
                            valoriSuccessivo[campoDestinazione.Nome] = "0";
                            modificato = true;
                        }
                    }
                }
            }

            if (modificato)
            {
                periodoSuccessivo.Valori = JsonSerializer.Serialize(valoriSuccessivo);
                await _context.SaveChangesAsync();

                // Ricalcola i campi calcolati del periodo successivo
                await RicalcolaCampiAsync(clienteAttivitaId, numeroPeriodoCorrente + 1);
            }
        }

        // Ricalcola i campi calcolati per un periodo
        private async Task<Dictionary<string, string>> RicalcolaCampiAsync(int clienteAttivitaId, int numeroPeriodo)
        {
            var clienteAttivita = await _context.ClientiAttivitaPeriodiche
                .Include(c => c.TipoPeriodo)
                    .ThenInclude(t => t!.Campi.Where(c => c.IsActive))
                .Include(c => c.ValoriPeriodi)
                .FirstOrDefaultAsync(c => c.Id == clienteAttivitaId);

            if (clienteAttivita?.TipoPeriodo == null) return new Dictionary<string, string>();

            var valorePeriodo = clienteAttivita.ValoriPeriodi.FirstOrDefault(v => v.NumeroPeriodo == numeroPeriodo);
            if (valorePeriodo == null) return new Dictionary<string, string>();

            var valori = JsonSerializer.Deserialize<Dictionary<string, string>>(valorePeriodo.Valori) 
                         ?? new Dictionary<string, string>();
            var valoriCalcolati = new Dictionary<string, string>();

            // Ottieni valori del periodo precedente per riporti (sia normali che calcolati)
            Dictionary<string, string>? valoriPrecedenti = null;
            Dictionary<string, string>? valoriCalcolatiPrecedenti = null;
            if (numeroPeriodo > 1)
            {
                var periodoPrecedente = clienteAttivita.ValoriPeriodi.FirstOrDefault(v => v.NumeroPeriodo == numeroPeriodo - 1);
                if (periodoPrecedente != null)
                {
                    valoriPrecedenti = JsonSerializer.Deserialize<Dictionary<string, string>>(periodoPrecedente.Valori) 
                                       ?? new Dictionary<string, string>();
                    valoriCalcolatiPrecedenti = JsonSerializer.Deserialize<Dictionary<string, string>>(periodoPrecedente.ValoriCalcolati) 
                                                ?? new Dictionary<string, string>();
                }
            }

            // DEBUG: Log valori disponibili
            Console.WriteLine($"[CALC DEBUG] Periodo {numeroPeriodo}, Valori input disponibili: {string.Join(", ", valori.Select(v => $"{v.Key}={v.Value}"))}");

            foreach (var campo in clienteAttivita.TipoPeriodo.Campi.Where(c => c.IsCalculated && !string.IsNullOrEmpty(c.Formula)).OrderBy(c => c.DisplayOrder))
            {
                var formula = campo.Formula!;
                Console.WriteLine($"[CALC] Campo: {campo.Nome}, Formula originale: {formula}");
                try
                {
                    // 1. Sostituisci riferimenti ai campi INPUT del periodo corrente
                    foreach (var campoDato in clienteAttivita.TipoPeriodo.Campi.Where(c => !c.IsCalculated))
                    {
                        valori.TryGetValue(campoDato.Nome, out string? val);
                        var valoreNumerico = NormalizzaValore(val);
                        Console.WriteLine($"[CALC]   Sostituisco [{campoDato.Nome}] con {valoreNumerico} (originale: {val ?? "null"})");
                        formula = formula.Replace($"[{campoDato.Nome}]", valoreNumerico);
                    }

                    // 2. Sostituisci riferimenti ai campi CALCOLATI del periodo corrente (già calcolati)
                    foreach (var campoCalc in clienteAttivita.TipoPeriodo.Campi.Where(c => c.IsCalculated && c.Nome != campo.Nome))
                    {
                        valoriCalcolati.TryGetValue(campoCalc.Nome, out string? valCalc);
                        var valoreNumerico = NormalizzaValore(valCalc);
                        formula = formula.Replace($"[{campoCalc.Nome}]", valoreNumerico);
                    }

                    // 3. Sostituisci riferimenti a campi INPUT del periodo precedente [CAMPO_PREC]
                    if (valoriPrecedenti != null)
                    {
                        foreach (var campoDato in clienteAttivita.TipoPeriodo.Campi.Where(c => !c.IsCalculated))
                        {
                            valoriPrecedenti.TryGetValue(campoDato.Nome, out string? valPrec);
                            var valorePrecNumerico = NormalizzaValore(valPrec);
                            formula = formula.Replace($"[{campoDato.Nome}_PREC]", valorePrecNumerico);
                        }
                    }

                    // 4. Sostituisci riferimenti a campi CALCOLATI del periodo precedente [CAMPO_PREC]
                    if (valoriCalcolatiPrecedenti != null)
                    {
                        foreach (var campoCalc in clienteAttivita.TipoPeriodo.Campi.Where(c => c.IsCalculated))
                        {
                            valoriCalcolatiPrecedenti.TryGetValue(campoCalc.Nome, out string? valCalcPrec);
                            var valorePrecNumerico = NormalizzaValore(valCalcPrec);
                            formula = formula.Replace($"[{campoCalc.Nome}_PREC]", valorePrecNumerico);
                        }
                    }

                    // 5. Sostituisci eventuali placeholder rimasti con 0
                    formula = System.Text.RegularExpressions.Regex.Replace(formula, @"\[[^\]]+\]", "0");

                    // Log formula finale per debug
                    Console.WriteLine($"[CALC] Campo: {campo.Nome}, Formula finale: '{formula}'");

                    // Calcola (semplice eval per operazioni base)
                    var risultato = CalcolaFormula(formula);
                    valoriCalcolati[campo.Nome] = risultato.ToString("0.##");
                }
                catch (Exception ex)
                {
                    // Log per debug
                    Console.WriteLine($"[CALC ERROR] {campo.Nome}: {ex.Message}, Formula: {formula}");
                    valoriCalcolati[campo.Nome] = "ERR";
                }
            }

            // Salva valori calcolati
            valorePeriodo.ValoriCalcolati = JsonSerializer.Serialize(valoriCalcolati);
            await _context.SaveChangesAsync();

            return valoriCalcolati;
        }

        private string NormalizzaValore(string? valore)
        {
            if (string.IsNullOrWhiteSpace(valore) || valore == "ERR")
                return "0";
            
            var valNorm = valore.Replace(",", ".");
            if (decimal.TryParse(valNorm, System.Globalization.NumberStyles.Any, 
                System.Globalization.CultureInfo.InvariantCulture, out _))
            {
                return valNorm;
            }
            return "0";
        }

        private decimal CalcolaFormula(string formula)
        {
            // Usa FormulaCalculatorService che supporta SE()/IF()
            var risultato = FormulaCalculatorService.CalcolaFormulaGenerica(formula, new Dictionary<string, string>());
            
            if (risultato != null && decimal.TryParse(risultato.Replace(",", "."), 
                System.Globalization.NumberStyles.Any, 
                System.Globalization.CultureInfo.InvariantCulture, out var valore))
            {
                return valore;
            }
            
            // Fallback: parser semplice per operazioni +, -, *, /
            formula = formula.Replace(" ", "").Replace(",", ".");
            var dataTable = new System.Data.DataTable();
            var result = dataTable.Compute(formula, null);
            return Convert.ToDecimal(result);
        }

        // POST: AttivitaPeriodiche/CopiaAnno
        [HttpPost]
        public async Task<IActionResult> CopiaAnno(int attivitaPeriodicaId, int tipoPeriodoId, int annoOrigine, int annoDestinazione, bool copiaDati = false, string[]? campiDaCopiare = null)
        {
            if (annoOrigine == annoDestinazione)
            {
                TempData["Error"] = "Anno origine e destinazione devono essere diversi.";
                return RedirectToAction(nameof(Dati), new { id = attivitaPeriodicaId, tipoPeriodoId, anno = annoOrigine });
            }

            var clientiOrigine = await _context.ClientiAttivitaPeriodiche
                .Include(c => c.ValoriPeriodi) // Include valori per copiare anche i dati
                .Where(c => c.AttivitaPeriodicaId == attivitaPeriodicaId 
                         && c.TipoPeriodoId == tipoPeriodoId 
                         && c.AnnoFiscale == annoOrigine)
                .ToListAsync();
            
            // Set di campi da copiare (null significa tutti)
            HashSet<string>? campiFiltro = campiDaCopiare?.Length > 0 ? new HashSet<string>(campiDaCopiare) : null;

            int copiati = 0;
            int datiCopiati = 0;
            
            foreach (var cliente in clientiOrigine)
            {
                // Verifica se esiste già
                var clienteEsistente = await _context.ClientiAttivitaPeriodiche
                    .Include(c => c.ValoriPeriodi)
                    .FirstOrDefaultAsync(c => c.TipoPeriodoId == tipoPeriodoId 
                                && c.ClienteId == cliente.ClienteId 
                                && c.AnnoFiscale == annoDestinazione);

                if (clienteEsistente == null)
                {
                    // Crea nuovo cliente
                    var nuova = new ClienteAttivitaPeriodica
                    {
                        AttivitaPeriodicaId = attivitaPeriodicaId,
                        TipoPeriodoId = tipoPeriodoId,
                        ClienteId = cliente.ClienteId,
                        AnnoFiscale = annoDestinazione,
                        CodCoge = cliente.CodCoge,
                        PercentualeInteressi = cliente.PercentualeInteressi,
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                    _context.ClientiAttivitaPeriodiche.Add(nuova);
                    await _context.SaveChangesAsync(); // Salva per ottenere l'Id

                    // Copia valori periodi se richiesto
                    if (copiaDati && cliente.ValoriPeriodi.Any())
                    {
                        foreach (var valore in cliente.ValoriPeriodi)
                        {
                            var valoriFiltrati = FiltraValoriJson(valore.Valori, campiFiltro);
                            _context.ValoriPeriodi.Add(new ValorePeriodo
                            {
                                ClienteAttivitaPeriodicaId = nuova.Id,
                                NumeroPeriodo = valore.NumeroPeriodo,
                                Valori = valoriFiltrati,  // Copia solo i campi selezionati
                                ValoriCalcolati = "{}", // Reset calcolati (verranno ricalcolati)
                                DataAggiornamento = DateTime.Now
                            });
                            datiCopiati++;
                        }
                    }
                    copiati++;
                }
                else if (copiaDati && cliente.ValoriPeriodi.Any())
                {
                    // Cliente esiste già, ma copiamo i dati se richiesto
                    foreach (var valoreOrigine in cliente.ValoriPeriodi)
                    {
                        var valoriFiltrati = FiltraValoriJson(valoreOrigine.Valori, campiFiltro);
                        var valoreDestinazione = clienteEsistente.ValoriPeriodi
                            .FirstOrDefault(v => v.NumeroPeriodo == valoreOrigine.NumeroPeriodo);
                        
                        if (valoreDestinazione != null)
                        {
                            // Aggiorna solo se i dati destinazione sono vuoti
                            if (valoreDestinazione.Valori == "{}" || string.IsNullOrEmpty(valoreDestinazione.Valori))
                            {
                                valoreDestinazione.Valori = valoriFiltrati;
                                valoreDestinazione.DataAggiornamento = DateTime.Now;
                                datiCopiati++;
                            }
                        }
                        else
                        {
                            // Crea nuovo periodo
                            _context.ValoriPeriodi.Add(new ValorePeriodo
                            {
                                ClienteAttivitaPeriodicaId = clienteEsistente.Id,
                                NumeroPeriodo = valoreOrigine.NumeroPeriodo,
                                Valori = valoriFiltrati,
                                ValoriCalcolati = "{}",
                                DataAggiornamento = DateTime.Now
                            });
                            datiCopiati++;
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();

            // Crea record ValorePeriodo vuoti per i nuovi clienti
            var nuoviClienti = await _context.ClientiAttivitaPeriodiche
                .Where(c => c.TipoPeriodoId == tipoPeriodoId && c.AnnoFiscale == annoDestinazione)
                .ToListAsync();

            var tipo = await _context.TipiPeriodo.FindAsync(tipoPeriodoId);
            if (tipo != null)
            {
                foreach (var nc in nuoviClienti)
                {
                    var hasValori = await _context.ValoriPeriodi.AnyAsync(v => v.ClienteAttivitaPeriodicaId == nc.Id);
                    if (!hasValori)
                    {
                        for (int i = 1; i <= tipo.NumeroPeriodi; i++)
                        {
                            _context.ValoriPeriodi.Add(new ValorePeriodo
                            {
                                ClienteAttivitaPeriodicaId = nc.Id,
                                NumeroPeriodo = i,
                                Valori = "{}",
                                ValoriCalcolati = "{}"
                            });
                        }
                    }
                }
                await _context.SaveChangesAsync();
            }

            var messaggioDati = copiaDati && datiCopiati > 0 ? $" ({datiCopiati} periodi con dati copiati)" : "";
            TempData["Success"] = $"{copiati} clienti copiati da {annoOrigine} a {annoDestinazione}.{messaggioDati}";
            return RedirectToAction(nameof(Dati), new { id = attivitaPeriodicaId, tipoPeriodoId, anno = annoDestinazione });
        }

        // ============ HELPERS ============
        
        /// <summary>
        /// Filtra i valori JSON mantenendo solo i campi specificati nel filtro.
        /// Se il filtro è null, restituisce il JSON originale.
        /// </summary>
        private string FiltraValoriJson(string valoriJson, HashSet<string>? campiFiltro)
        {
            if (campiFiltro == null || string.IsNullOrEmpty(valoriJson) || valoriJson == "{}")
                return valoriJson;
            
            try
            {
                var valoriOriginali = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(valoriJson) 
                                      ?? new Dictionary<string, string>();
                
                var valoriFiltrati = new Dictionary<string, string>();
                foreach (var kvp in valoriOriginali)
                {
                    if (campiFiltro.Contains(kvp.Key))
                    {
                        valoriFiltrati[kvp.Key] = kvp.Value;
                    }
                }
                
                return System.Text.Json.JsonSerializer.Serialize(valoriFiltrati);
            }
            catch
            {
                return "{}";
            }
        }

        private (List<string> etichette, List<string> dateInizio, List<string> dateFine) GeneraPeriodiDefault(int numeroPeriodi)
        {
            var etichette = new List<string>();
            var dateInizio = new List<string>();
            var dateFine = new List<string>();

            switch (numeroPeriodi)
            {
                case 12: // Mensile
                    var mesi = new[] { "Gennaio", "Febbraio", "Marzo", "Aprile", "Maggio", "Giugno",
                                       "Luglio", "Agosto", "Settembre", "Ottobre", "Novembre", "Dicembre" };
                    var giorniFine = new[] { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
                    for (int i = 0; i < 12; i++)
                    {
                        etichette.Add(mesi[i]);
                        dateInizio.Add($"01/{(i + 1):D2}");
                        dateFine.Add($"{giorniFine[i]:D2}/{(i + 1):D2}");
                    }
                    break;

                case 4: // Trimestrale
                    etichette.AddRange(new[] { "1° Trimestre", "2° Trimestre", "3° Trimestre", "4° Trimestre" });
                    dateInizio.AddRange(new[] { "01/01", "01/04", "01/07", "01/10" });
                    dateFine.AddRange(new[] { "31/03", "30/06", "30/09", "31/12" });
                    break;

                case 2: // Semestrale
                    etichette.AddRange(new[] { "1° Semestre", "2° Semestre" });
                    dateInizio.AddRange(new[] { "01/01", "01/07" });
                    dateFine.AddRange(new[] { "30/06", "31/12" });
                    break;

                case 1: // Annuale
                    etichette.Add("Anno");
                    dateInizio.Add("01/01");
                    dateFine.Add("31/12");
                    break;

                default:
                    // Custom: genera periodi generici
                    for (int i = 1; i <= numeroPeriodi; i++)
                    {
                        etichette.Add($"Periodo {i}");
                        dateInizio.Add("");
                        dateFine.Add("");
                    }
                    break;
            }

            return (etichette, dateInizio, dateFine);
        }
    }

    /// <summary>
    /// Request per salvataggio valore cella via AJAX
    /// </summary>
    public class SalvaValoreRequest
    {
        public int ClienteAttivitaId { get; set; }
        public int NumeroPeriodo { get; set; }
        public string NomeCampo { get; set; } = string.Empty;
        public string Valore { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request per ricalcolo riporti via AJAX
    /// </summary>
    public class RicalcolaRiportiRequest
    {
        public int ClienteAttivitaId { get; set; }
    }

    /// <summary>
    /// Request per cancellazione dati via AJAX
    /// </summary>
    public class CancellaDatiRequest
    {
        public int ClienteAttivitaId { get; set; }
        public int NumeroPeriodo { get; set; } // 0 = tutti i periodi
    }
}

