using Microsoft.EntityFrameworkCore;
using StudioCG.Web.Data;
using StudioCG.Web.Models;
using StudioCG.Web.Models.Documenti;
using StudioCG.Web.Models.Fatturazione;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;

namespace StudioCG.Web.Services
{
    /// <summary>
    /// Servizio per la generazione di documenti da template
    /// </summary>
    public class DocumentoGeneratorService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DocumentoGeneratorService> _logger;
        private readonly CultureInfo _italianCulture = new CultureInfo("it-IT");

        public DocumentoGeneratorService(ApplicationDbContext context, ILogger<DocumentoGeneratorService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Genera un documento sostituendo i campi dinamici
        /// </summary>
        public async Task<DocumentoGenerato> GeneraDocumentoAsync(
            int templateId,
            int clienteId,
            int? mandatoId,
            TipoOutputDocumento tipoOutput,
            int userId)
        {
            // Carica template
            var template = await _context.TemplateDocumenti.FindAsync(templateId);
            if (template == null)
                throw new ArgumentException("Template non trovato");

            // Carica cliente con soggetti
            var cliente = await _context.Clienti
                .Include(c => c.Soggetti)
                .FirstOrDefaultAsync(c => c.Id == clienteId);
            if (cliente == null)
                throw new ArgumentException("Cliente non trovato");

            // Carica mandato se richiesto, altrimenti prende l'ultimo mandato attivo del cliente
            MandatoCliente? mandato = null;
            if (mandatoId.HasValue)
            {
                mandato = await _context.MandatiClienti.FindAsync(mandatoId.Value);
            }
            else
            {
                // Se non è specificato un mandato, prende l'ultimo mandato attivo del cliente
                mandato = await _context.MandatiClienti
                    .Where(m => m.ClienteId == clienteId && m.IsActive)
                    .OrderByDescending(m => m.Anno)
                    .FirstOrDefaultAsync();
            }

            // Carica configurazione studio
            var studio = await _context.ConfigurazioniStudio.FirstOrDefaultAsync();

            // Sostituisci i campi in tutte le sezioni
            var intestazioneCompilata = !string.IsNullOrEmpty(template.Intestazione) 
                ? SostituisciCampi(template.Intestazione, studio, cliente, mandato) 
                : null;
            var contenutoCompilato = SostituisciCampi(template.Contenuto, studio, cliente, mandato);
            var piePaginaCompilato = !string.IsNullOrEmpty(template.PiePagina) 
                ? SostituisciCampi(template.PiePagina, studio, cliente, mandato) 
                : null;

            // Genera il file
            byte[] fileBytes;
            string contentType;
            string estensione;

            if (tipoOutput == TipoOutputDocumento.PDF)
            {
                fileBytes = GeneraPdf(intestazioneCompilata, contenutoCompilato, piePaginaCompilato, studio);
                contentType = "application/pdf";
                estensione = ".pdf";
            }
            else
            {
                fileBytes = GeneraWord(intestazioneCompilata, contenutoCompilato, piePaginaCompilato, studio);
                contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                estensione = ".docx";
            }

            // Crea nome file
            var nomeFile = $"{template.Nome}_{cliente.RagioneSociale}_{DateTime.Now:yyyyMMdd_HHmmss}{estensione}";
            nomeFile = SanitizeFileName(nomeFile);

            // Salva in archivio
            var documento = new DocumentoGenerato
            {
                TemplateDocumentoId = templateId,
                ClienteId = clienteId,
                MandatoClienteId = mandatoId,
                NomeFile = nomeFile,
                Contenuto = fileBytes,
                ContentType = contentType,
                TipoOutput = tipoOutput,
                GeneratoDaUserId = userId,
                GeneratoIl = DateTime.Now
            };

            _context.DocumentiGenerati.Add(documento);
            await _context.SaveChangesAsync();

            return documento;
        }

        /// <summary>
        /// Sostituisce tutti i campi dinamici nel contenuto
        /// </summary>
        private string SostituisciCampi(string contenuto, ConfigurazioneStudio? studio, Cliente cliente, MandatoCliente? mandato)
        {
            var result = contenuto;

            // Rimuovi tag span campo-dinamico (lasciati da TinyMCE)
            result = Regex.Replace(result, @"<span class=""campo-dinamico"">(.*?)</span>", "$1");

            // ===== CAMPI STUDIO =====
            if (studio != null)
            {
                result = result.Replace("{{Studio.Nome}}", studio.NomeStudio ?? "");
                result = result.Replace("{{Studio.Indirizzo}}", studio.Indirizzo ?? "");
                result = result.Replace("{{Studio.Citta}}", studio.Citta ?? "");
                result = result.Replace("{{Studio.CAP}}", studio.CAP ?? "");
                result = result.Replace("{{Studio.Provincia}}", studio.Provincia ?? "");
                result = result.Replace("{{Studio.PIVA}}", studio.PIVA ?? "");
                result = result.Replace("{{Studio.CF}}", studio.CF ?? "");
                result = result.Replace("{{Studio.Email}}", studio.Email ?? "");
                result = result.Replace("{{Studio.PEC}}", studio.PEC ?? "");
                result = result.Replace("{{Studio.Telefono}}", studio.Telefono ?? "");
                
                // Logo e Firma - placeholder per immagini
                if (studio.Logo != null)
                {
                    var logoBase64 = Convert.ToBase64String(studio.Logo);
                    result = result.Replace("{{Studio.Logo}}", $"<img src=\"data:{studio.LogoContentType};base64,{logoBase64}\" style=\"max-height: 80px;\" />");
                }
                else
                {
                    result = result.Replace("{{Studio.Logo}}", "");
                }

                if (studio.Firma != null)
                {
                    var firmaBase64 = Convert.ToBase64String(studio.Firma);
                    result = result.Replace("{{Studio.Firma}}", $"<img src=\"data:{studio.FirmaContentType};base64,{firmaBase64}\" style=\"max-height: 50px;\" />");
                }
                else
                {
                    result = result.Replace("{{Studio.Firma}}", "");
                }

                // Indirizzo completo
                var indirizzoCompleto = new StringBuilder();
                if (!string.IsNullOrEmpty(studio.Indirizzo)) indirizzoCompleto.Append(studio.Indirizzo);
                if (!string.IsNullOrEmpty(studio.CAP)) indirizzoCompleto.Append($" - {studio.CAP}");
                if (!string.IsNullOrEmpty(studio.Citta)) indirizzoCompleto.Append($" {studio.Citta}");
                if (!string.IsNullOrEmpty(studio.Provincia)) indirizzoCompleto.Append($" ({studio.Provincia})");
                result = result.Replace("{{Studio.IndirizzoCompleto}}", indirizzoCompleto.ToString());
            }
            else
            {
                // Rimuovi tutti i campi studio se non configurato
                result = Regex.Replace(result, @"\{\{Studio\.[^}]+\}\}", "");
            }

            // ===== CAMPI CLIENTE =====
            result = result.Replace("{{Cliente.RagioneSociale}}", cliente.RagioneSociale ?? "");
            result = result.Replace("{{Cliente.Indirizzo}}", cliente.Indirizzo ?? "");
            result = result.Replace("{{Cliente.Citta}}", cliente.Citta ?? "");
            result = result.Replace("{{Cliente.CAP}}", cliente.CAP ?? "");
            result = result.Replace("{{Cliente.Provincia}}", cliente.Provincia ?? "");
            result = result.Replace("{{Cliente.CF}}", cliente.CodiceFiscale ?? "");
            result = result.Replace("{{Cliente.PIVA}}", cliente.PartitaIVA ?? "");
            result = result.Replace("{{Cliente.Email}}", cliente.Email ?? "");
            result = result.Replace("{{Cliente.PEC}}", cliente.PEC ?? "");
            result = result.Replace("{{Cliente.Telefono}}", cliente.Telefono ?? "");
            result = result.Replace("{{Cliente.CodiceAteco}}", cliente.CodiceAteco ?? "");
            result = result.Replace("{{Cliente.TipoSoggetto}}", cliente.TipoSoggetto?.ToString() ?? "");

            // Importo annuo dalla fatturazione (dal mandato)
            if (mandato != null)
            {
                result = result.Replace("{{Cliente.ImportoAnnuo}}", mandato.ImportoAnnuo.ToString("N2", _italianCulture) + " €");
                result = result.Replace("{{Cliente.ImportoAnnuoLettere}}", NumeroInLettere(mandato.ImportoAnnuo));
            }
            else
            {
                result = result.Replace("{{Cliente.ImportoAnnuo}}", "");
                result = result.Replace("{{Cliente.ImportoAnnuoLettere}}", "");
            }

            // ===== CAMPI SOGGETTI =====
            var soggetti = cliente.Soggetti?.ToList() ?? new List<ClienteSoggetto>();
            
            // ---- LEGALE RAPPRESENTANTE ----
            var legaleRapp = soggetti.FirstOrDefault(s => s.TipoSoggetto == TipoSoggetto.LegaleRappresentante);
            if (legaleRapp != null)
            {
                result = result.Replace("{{LegaleRapp.NomeCompleto}}", $"{legaleRapp.Cognome} {legaleRapp.Nome}");
                result = result.Replace("{{LegaleRapp.Nome}}", legaleRapp.Nome ?? "");
                result = result.Replace("{{LegaleRapp.Cognome}}", legaleRapp.Cognome ?? "");
                result = result.Replace("{{LegaleRapp.CF}}", legaleRapp.CodiceFiscale ?? "");
                result = result.Replace("{{LegaleRapp.Indirizzo}}", legaleRapp.Indirizzo ?? "");
                result = result.Replace("{{LegaleRapp.Citta}}", legaleRapp.Citta ?? "");
                result = result.Replace("{{LegaleRapp.Provincia}}", legaleRapp.Provincia ?? "");
                result = result.Replace("{{LegaleRapp.CAP}}", legaleRapp.CAP ?? "");
                result = result.Replace("{{LegaleRapp.Email}}", legaleRapp.Email ?? "");
                result = result.Replace("{{LegaleRapp.Telefono}}", legaleRapp.Telefono ?? "");
                // Documento di identità
                result = result.Replace("{{LegaleRapp.DocumentoNumero}}", legaleRapp.DocumentoNumero ?? "");
                result = result.Replace("{{LegaleRapp.DocumentoDataRilascio}}", legaleRapp.DocumentoDataRilascio?.ToString("dd/MM/yyyy") ?? "");
                result = result.Replace("{{LegaleRapp.DocumentoRilasciatoDa}}", legaleRapp.DocumentoRilasciatoDa ?? "");
                result = result.Replace("{{LegaleRapp.DocumentoScadenza}}", legaleRapp.DocumentoScadenza?.ToString("dd/MM/yyyy") ?? "");
                var indirizzoCompleto = legaleRapp.Indirizzo ?? "";
                if (!string.IsNullOrEmpty(legaleRapp.CAP)) indirizzoCompleto += $" - {legaleRapp.CAP}";
                if (!string.IsNullOrEmpty(legaleRapp.Citta)) indirizzoCompleto += $" {legaleRapp.Citta}";
                if (!string.IsNullOrEmpty(legaleRapp.Provincia)) indirizzoCompleto += $" ({legaleRapp.Provincia})";
                result = result.Replace("{{LegaleRapp.IndirizzoCompleto}}", indirizzoCompleto);
                // Backward compatibility
                result = result.Replace("{{Soggetti.LegaleRapp}}", $"{legaleRapp.Cognome} {legaleRapp.Nome} (CF: {legaleRapp.CodiceFiscale})");
            }
            else
            {
                result = result.Replace("{{LegaleRapp.NomeCompleto}}", "");
                result = result.Replace("{{LegaleRapp.Nome}}", "");
                result = result.Replace("{{LegaleRapp.Cognome}}", "");
                result = result.Replace("{{LegaleRapp.CF}}", "");
                result = result.Replace("{{LegaleRapp.Indirizzo}}", "");
                result = result.Replace("{{LegaleRapp.Citta}}", "");
                result = result.Replace("{{LegaleRapp.Provincia}}", "");
                result = result.Replace("{{LegaleRapp.CAP}}", "");
                result = result.Replace("{{LegaleRapp.Email}}", "");
                result = result.Replace("{{LegaleRapp.Telefono}}", "");
                result = result.Replace("{{LegaleRapp.DocumentoNumero}}", "");
                result = result.Replace("{{LegaleRapp.DocumentoDataRilascio}}", "");
                result = result.Replace("{{LegaleRapp.DocumentoRilasciatoDa}}", "");
                result = result.Replace("{{LegaleRapp.DocumentoScadenza}}", "");
                result = result.Replace("{{LegaleRapp.IndirizzoCompleto}}", "");
                result = result.Replace("{{Soggetti.LegaleRapp}}", "");
            }

            // ---- CONSIGLIERI ----
            var consiglieri = soggetti.Where(s => s.TipoSoggetto == TipoSoggetto.Consigliere).OrderBy(s => s.DisplayOrder).ToList();
            if (consiglieri.Any())
            {
                var elencoConsiglieri = string.Join("<br/>", consiglieri.Select(c => $"{c.Cognome} {c.Nome}"));
                result = result.Replace("{{Consiglieri.Elenco}}", elencoConsiglieri);
                
                // Elenco VERAMENTE completo con TUTTI i campi dell'anagrafica
                var elencoConsiglieriCompleto = string.Join("<br/><br/>", consiglieri.Select((c, idx) => {
                    var sb = new StringBuilder();
                    sb.Append($"<strong>CONSIGLIERE {idx + 1}: {c.Cognome} {c.Nome}</strong><br/>");
                    sb.Append($"Codice Fiscale: {c.CodiceFiscale ?? "N/A"}<br/>");
                    // Indirizzo completo
                    if (!string.IsNullOrEmpty(c.Indirizzo) || !string.IsNullOrEmpty(c.Citta) || !string.IsNullOrEmpty(c.CAP) || !string.IsNullOrEmpty(c.Provincia))
                    {
                        sb.Append("Indirizzo: ");
                        if (!string.IsNullOrEmpty(c.Indirizzo)) sb.Append(c.Indirizzo);
                        if (!string.IsNullOrEmpty(c.CAP)) sb.Append($" - {c.CAP}");
                        if (!string.IsNullOrEmpty(c.Citta)) sb.Append($" {c.Citta}");
                        if (!string.IsNullOrEmpty(c.Provincia)) sb.Append($" ({c.Provincia})");
                        sb.Append("<br/>");
                    }
                    // Contatti
                    if (!string.IsNullOrEmpty(c.Email))
                        sb.Append($"Email: {c.Email}<br/>");
                    if (!string.IsNullOrEmpty(c.Telefono))
                        sb.Append($"Telefono: {c.Telefono}<br/>");
                    // Documento identità
                    if (!string.IsNullOrEmpty(c.DocumentoNumero) || c.DocumentoDataRilascio.HasValue || !string.IsNullOrEmpty(c.DocumentoRilasciatoDa) || c.DocumentoScadenza.HasValue)
                    {
                        sb.Append("Documento: ");
                        if (!string.IsNullOrEmpty(c.DocumentoNumero)) sb.Append($"N° {c.DocumentoNumero}");
                        if (c.DocumentoDataRilascio.HasValue) sb.Append($" del {c.DocumentoDataRilascio.Value:dd/MM/yyyy}");
                        if (!string.IsNullOrEmpty(c.DocumentoRilasciatoDa)) sb.Append($" rilasciato da {c.DocumentoRilasciatoDa}");
                        if (c.DocumentoScadenza.HasValue) sb.Append($" - Scadenza: {c.DocumentoScadenza.Value:dd/MM/yyyy}");
                    }
                    return sb.ToString();
                }));
                result = result.Replace("{{Consiglieri.ElencoCompleto}}", elencoConsiglieriCompleto);
                
                // Numero consiglieri
                result = result.Replace("{{Consiglieri.Numero}}", consiglieri.Count.ToString());
            }
            else
            {
                result = result.Replace("{{Consiglieri.Elenco}}", "");
                result = result.Replace("{{Consiglieri.ElencoCompleto}}", "");
                result = result.Replace("{{Consiglieri.Numero}}", "0");
            }
            
            // Campi individuali per ogni Consigliere (fino a 10)
            for (int i = 1; i <= 10; i++)
            {
                var prefix = $"{{{{Consigliere{i}.";
                if (i <= consiglieri.Count)
                {
                    var c = consiglieri[i - 1];
                    result = result.Replace($"{prefix}NomeCompleto}}}}", $"{c.Cognome} {c.Nome}");
                    result = result.Replace($"{prefix}Nome}}}}", c.Nome ?? "");
                    result = result.Replace($"{prefix}Cognome}}}}", c.Cognome ?? "");
                    result = result.Replace($"{prefix}CF}}}}", c.CodiceFiscale ?? "");
                    result = result.Replace($"{prefix}Indirizzo}}}}", c.Indirizzo ?? "");
                    result = result.Replace($"{prefix}Citta}}}}", c.Citta ?? "");
                    result = result.Replace($"{prefix}Provincia}}}}", c.Provincia ?? "");
                    result = result.Replace($"{prefix}CAP}}}}", c.CAP ?? "");
                    result = result.Replace($"{prefix}Email}}}}", c.Email ?? "");
                    result = result.Replace($"{prefix}Telefono}}}}", c.Telefono ?? "");
                    result = result.Replace($"{prefix}DocumentoNumero}}}}", c.DocumentoNumero ?? "");
                    result = result.Replace($"{prefix}DocumentoDataRilascio}}}}", c.DocumentoDataRilascio?.ToString("dd/MM/yyyy") ?? "");
                    result = result.Replace($"{prefix}DocumentoRilasciatoDa}}}}", c.DocumentoRilasciatoDa ?? "");
                    result = result.Replace($"{prefix}DocumentoScadenza}}}}", c.DocumentoScadenza?.ToString("dd/MM/yyyy") ?? "");
                    // Indirizzo completo
                    var indirizzoCompletoC = c.Indirizzo ?? "";
                    if (!string.IsNullOrEmpty(c.CAP)) indirizzoCompletoC += $" - {c.CAP}";
                    if (!string.IsNullOrEmpty(c.Citta)) indirizzoCompletoC += $" {c.Citta}";
                    if (!string.IsNullOrEmpty(c.Provincia)) indirizzoCompletoC += $" ({c.Provincia})";
                    result = result.Replace($"{prefix}IndirizzoCompleto}}}}", indirizzoCompletoC);
                }
                else
                {
                    // Rimuovi tutti i campi per consiglieri non esistenti
                    result = result.Replace($"{prefix}NomeCompleto}}}}", "");
                    result = result.Replace($"{prefix}Nome}}}}", "");
                    result = result.Replace($"{prefix}Cognome}}}}", "");
                    result = result.Replace($"{prefix}CF}}}}", "");
                    result = result.Replace($"{prefix}Indirizzo}}}}", "");
                    result = result.Replace($"{prefix}Citta}}}}", "");
                    result = result.Replace($"{prefix}Provincia}}}}", "");
                    result = result.Replace($"{prefix}CAP}}}}", "");
                    result = result.Replace($"{prefix}Email}}}}", "");
                    result = result.Replace($"{prefix}Telefono}}}}", "");
                    result = result.Replace($"{prefix}DocumentoNumero}}}}", "");
                    result = result.Replace($"{prefix}DocumentoDataRilascio}}}}", "");
                    result = result.Replace($"{prefix}DocumentoRilasciatoDa}}}}", "");
                    result = result.Replace($"{prefix}DocumentoScadenza}}}}", "");
                    result = result.Replace($"{prefix}IndirizzoCompleto}}}}", "");
                }
            }

            // ---- SOCI ----
            var soci = soggetti.Where(s => s.TipoSoggetto == TipoSoggetto.Socio).OrderBy(s => s.DisplayOrder).ToList();
            if (soci.Any())
            {
                var elencoSoci = string.Join("<br/>", soci.Select(s => $"{s.Cognome} {s.Nome}"));
                result = result.Replace("{{Soci.Elenco}}", elencoSoci);
                
                var elencoSociConQuote = string.Join("<br/>", soci.Select(s => 
                    $"{s.Cognome} {s.Nome}" + 
                    (s.QuotaPercentuale.HasValue ? $" - Quota: {s.QuotaPercentuale.Value.ToString("N2", _italianCulture)}%" : "")));
                result = result.Replace("{{Soci.ElencoConQuote}}", elencoSociConQuote);
                
                // Elenco VERAMENTE completo con TUTTI i campi dell'anagrafica
                var elencoSociCompleto = string.Join("<br/><br/>", soci.Select((s, idx) => {
                    var sb = new StringBuilder();
                    sb.Append($"<strong>SOCIO {idx + 1}: {s.Cognome} {s.Nome}</strong><br/>");
                    sb.Append($"Codice Fiscale: {s.CodiceFiscale ?? "N/A"}");
                    if (s.QuotaPercentuale.HasValue)
                        sb.Append($" - Quota: {s.QuotaPercentuale.Value.ToString("N2", _italianCulture)}%");
                    sb.Append("<br/>");
                    // Indirizzo completo
                    if (!string.IsNullOrEmpty(s.Indirizzo) || !string.IsNullOrEmpty(s.Citta) || !string.IsNullOrEmpty(s.CAP) || !string.IsNullOrEmpty(s.Provincia))
                    {
                        sb.Append("Indirizzo: ");
                        if (!string.IsNullOrEmpty(s.Indirizzo)) sb.Append(s.Indirizzo);
                        if (!string.IsNullOrEmpty(s.CAP)) sb.Append($" - {s.CAP}");
                        if (!string.IsNullOrEmpty(s.Citta)) sb.Append($" {s.Citta}");
                        if (!string.IsNullOrEmpty(s.Provincia)) sb.Append($" ({s.Provincia})");
                        sb.Append("<br/>");
                    }
                    // Contatti
                    if (!string.IsNullOrEmpty(s.Email))
                        sb.Append($"Email: {s.Email}<br/>");
                    if (!string.IsNullOrEmpty(s.Telefono))
                        sb.Append($"Telefono: {s.Telefono}<br/>");
                    // Documento identità
                    if (!string.IsNullOrEmpty(s.DocumentoNumero) || s.DocumentoDataRilascio.HasValue || !string.IsNullOrEmpty(s.DocumentoRilasciatoDa) || s.DocumentoScadenza.HasValue)
                    {
                        sb.Append("Documento: ");
                        if (!string.IsNullOrEmpty(s.DocumentoNumero)) sb.Append($"N° {s.DocumentoNumero}");
                        if (s.DocumentoDataRilascio.HasValue) sb.Append($" del {s.DocumentoDataRilascio.Value:dd/MM/yyyy}");
                        if (!string.IsNullOrEmpty(s.DocumentoRilasciatoDa)) sb.Append($" rilasciato da {s.DocumentoRilasciatoDa}");
                        if (s.DocumentoScadenza.HasValue) sb.Append($" - Scadenza: {s.DocumentoScadenza.Value:dd/MM/yyyy}");
                    }
                    return sb.ToString();
                }));
                result = result.Replace("{{Soci.ElencoCompleto}}", elencoSociCompleto);
                
                var totaleQuote = soci.Where(s => s.QuotaPercentuale.HasValue).Sum(s => s.QuotaPercentuale ?? 0);
                result = result.Replace("{{Soci.TotaleQuote}}", $"{totaleQuote.ToString("N2", _italianCulture)}%");
                
                // Numero soci
                result = result.Replace("{{Soci.Numero}}", soci.Count.ToString());
            }
            else
            {
                result = result.Replace("{{Soci.Elenco}}", "");
                result = result.Replace("{{Soci.ElencoConQuote}}", "");
                result = result.Replace("{{Soci.ElencoCompleto}}", "");
                result = result.Replace("{{Soci.TotaleQuote}}", "");
                result = result.Replace("{{Soci.Numero}}", "0");
            }
            
            // Campi individuali per ogni Socio (fino a 10)
            for (int i = 1; i <= 10; i++)
            {
                var prefix = $"{{{{Socio{i}.";
                if (i <= soci.Count)
                {
                    var s = soci[i - 1];
                    result = result.Replace($"{prefix}NomeCompleto}}}}", $"{s.Cognome} {s.Nome}");
                    result = result.Replace($"{prefix}Nome}}}}", s.Nome ?? "");
                    result = result.Replace($"{prefix}Cognome}}}}", s.Cognome ?? "");
                    result = result.Replace($"{prefix}CF}}}}", s.CodiceFiscale ?? "");
                    result = result.Replace($"{prefix}Indirizzo}}}}", s.Indirizzo ?? "");
                    result = result.Replace($"{prefix}Citta}}}}", s.Citta ?? "");
                    result = result.Replace($"{prefix}Provincia}}}}", s.Provincia ?? "");
                    result = result.Replace($"{prefix}CAP}}}}", s.CAP ?? "");
                    result = result.Replace($"{prefix}Email}}}}", s.Email ?? "");
                    result = result.Replace($"{prefix}Telefono}}}}", s.Telefono ?? "");
                    result = result.Replace($"{prefix}DocumentoNumero}}}}", s.DocumentoNumero ?? "");
                    result = result.Replace($"{prefix}DocumentoDataRilascio}}}}", s.DocumentoDataRilascio?.ToString("dd/MM/yyyy") ?? "");
                    result = result.Replace($"{prefix}DocumentoRilasciatoDa}}}}", s.DocumentoRilasciatoDa ?? "");
                    result = result.Replace($"{prefix}DocumentoScadenza}}}}", s.DocumentoScadenza?.ToString("dd/MM/yyyy") ?? "");
                    result = result.Replace($"{prefix}Quota}}}}", s.QuotaPercentuale.HasValue ? $"{s.QuotaPercentuale.Value.ToString("N2", _italianCulture)}%" : "");
                    result = result.Replace($"{prefix}QuotaNumero}}}}", s.QuotaPercentuale.HasValue ? s.QuotaPercentuale.Value.ToString("N2", _italianCulture) : "");
                    // Indirizzo completo
                    var indirizzoCompletoS = s.Indirizzo ?? "";
                    if (!string.IsNullOrEmpty(s.CAP)) indirizzoCompletoS += $" - {s.CAP}";
                    if (!string.IsNullOrEmpty(s.Citta)) indirizzoCompletoS += $" {s.Citta}";
                    if (!string.IsNullOrEmpty(s.Provincia)) indirizzoCompletoS += $" ({s.Provincia})";
                    result = result.Replace($"{prefix}IndirizzoCompleto}}}}", indirizzoCompletoS);
                }
                else
                {
                    // Rimuovi tutti i campi per soci non esistenti
                    result = result.Replace($"{prefix}NomeCompleto}}}}", "");
                    result = result.Replace($"{prefix}Nome}}}}", "");
                    result = result.Replace($"{prefix}Cognome}}}}", "");
                    result = result.Replace($"{prefix}CF}}}}", "");
                    result = result.Replace($"{prefix}Indirizzo}}}}", "");
                    result = result.Replace($"{prefix}Citta}}}}", "");
                    result = result.Replace($"{prefix}Provincia}}}}", "");
                    result = result.Replace($"{prefix}CAP}}}}", "");
                    result = result.Replace($"{prefix}Email}}}}", "");
                    result = result.Replace($"{prefix}Telefono}}}}", "");
                    result = result.Replace($"{prefix}DocumentoNumero}}}}", "");
                    result = result.Replace($"{prefix}DocumentoDataRilascio}}}}", "");
                    result = result.Replace($"{prefix}DocumentoRilasciatoDa}}}}", "");
                    result = result.Replace($"{prefix}DocumentoScadenza}}}}", "");
                    result = result.Replace($"{prefix}Quota}}}}", "");
                    result = result.Replace($"{prefix}QuotaNumero}}}}", "");
                    result = result.Replace($"{prefix}IndirizzoCompleto}}}}", "");
                }
            }

            // ---- TUTTI I SOGGETTI ----
            if (soggetti.Any())
            {
                var tuttiElenco = string.Join("<br/>", soggetti.Select(s => 
                    $"{s.Cognome} {s.Nome} ({GetTipoSoggettoLabel(s.TipoSoggetto)})" + 
                    (s.QuotaPercentuale.HasValue ? $" - {s.QuotaPercentuale.Value.ToString("N2", _italianCulture)}%" : "")));
                result = result.Replace("{{Soggetti.TuttiElenco}}", tuttiElenco);
                result = result.Replace("{{Soggetti.Elenco}}", tuttiElenco); // Backward compatibility
                
                // Tabella completa HTML
                var tabella = "<table style='width:100%; border-collapse:collapse;'>" +
                    "<tr style='background:#f0f0f0;'><th style='border:1px solid #ccc;padding:5px;'>Tipo</th>" +
                    "<th style='border:1px solid #ccc;padding:5px;'>Nome</th>" +
                    "<th style='border:1px solid #ccc;padding:5px;'>Codice Fiscale</th>" +
                    "<th style='border:1px solid #ccc;padding:5px;'>Quota %</th></tr>";
                foreach (var s in soggetti)
                {
                    tabella += $"<tr><td style='border:1px solid #ccc;padding:5px;'>{GetTipoSoggettoLabel(s.TipoSoggetto)}</td>" +
                        $"<td style='border:1px solid #ccc;padding:5px;'>{s.Cognome} {s.Nome}</td>" +
                        $"<td style='border:1px solid #ccc;padding:5px;'>{s.CodiceFiscale ?? "-"}</td>" +
                        $"<td style='border:1px solid #ccc;padding:5px;'>{(s.QuotaPercentuale.HasValue ? s.QuotaPercentuale.Value.ToString("N2", _italianCulture) + "%" : "-")}</td></tr>";
                }
                tabella += "</table>";
                result = result.Replace("{{Soggetti.Tabella}}", tabella);
            }
            else
            {
                result = result.Replace("{{Soggetti.TuttiElenco}}", "");
                result = result.Replace("{{Soggetti.Elenco}}", "");
                result = result.Replace("{{Soggetti.Tabella}}", "");
            }

            // Titolare (usa Legale Rappresentante se esiste, altrimenti primo socio)
            var titolare = legaleRapp ?? soggetti.FirstOrDefault();
            if (titolare != null)
            {
                result = result.Replace("{{Soggetti.Titolare}}", $"{titolare.Cognome} {titolare.Nome} (CF: {titolare.CodiceFiscale})");
            }
            else
            {
                result = result.Replace("{{Soggetti.Titolare}}", "");
            }

            // ===== CAMPI MANDATO =====
            if (mandato != null)
            {
                result = result.Replace("{{Mandato.Oggetto}}", mandato.Note ?? $"Mandato {mandato.Anno}");
                result = result.Replace("{{Mandato.ImportoAnnuo}}", mandato.ImportoAnnuo.ToString("N2", _italianCulture) + " €");
                result = result.Replace("{{Mandato.ImportoLettere}}", NumeroInLettere(mandato.ImportoAnnuo));
                result = result.Replace("{{Mandato.TipoScadenza}}", mandato.TipoScadenza.ToString());
                result = result.Replace("{{Mandato.Anno}}", mandato.Anno.ToString());
                result = result.Replace("{{Mandato.ImportoRata}}", mandato.ImportoRata.ToString("N2", _italianCulture) + " €");
                result = result.Replace("{{Mandato.NumeroRate}}", mandato.NumeroRate.ToString());
                result = result.Replace("{{Mandato.RimborsoSpese}}", mandato.RimborsoSpese.ToString("N2", _italianCulture) + " €");
                // DataInizio/DataFine non esistono nel modello, rimuoviamo
                result = result.Replace("{{Mandato.DataInizio}}", "");
                result = result.Replace("{{Mandato.DataFine}}", "");
            }
            else
            {
                result = Regex.Replace(result, @"\{\{Mandato\.[^}]+\}\}", "");
            }

            // ===== CAMPI DATA/VARIE =====
            result = result.Replace("{{Oggi}}", DateTime.Today.ToString("dd/MM/yyyy"));
            result = result.Replace("{{OggiEstesa}}", DateTime.Today.ToString("dddd d MMMM yyyy", _italianCulture));
            result = result.Replace("{{AnnoCorrente}}", DateTime.Today.Year.ToString());
            
            // Luogo e data
            var luogo = studio?.Citta ?? "";
            result = result.Replace("{{LuogoData}}", $"{luogo}, {DateTime.Today.ToString("dd/MM/yyyy")}");

            return result;
        }

        /// <summary>
        /// Genera PDF dal contenuto HTML usando QuestPDF
        /// </summary>
        private byte[] GeneraPdf(string? intestazione, string htmlContent, string? piePagina, ConfigurazioneStudio? studio)
        {
            // Configura licenza QuestPDF (Community = gratuita)
            QuestPDF.Settings.License = LicenseType.Community;

            var intestazionePlain = !string.IsNullOrEmpty(intestazione) ? RimuoviTagHtml(intestazione) : null;
            var footerPlain = !string.IsNullOrEmpty(piePagina) ? RimuoviTagHtml(piePagina) : null;
            
            // Estrai le parti del contenuto (testo e tabelle)
            var contentParts = ParseHtmlContent(htmlContent);

            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                    // Header - usa intestazione personalizzata se presente, altrimenti dati studio
                    page.Header().Column(col =>
                    {
                        if (!string.IsNullOrEmpty(intestazionePlain))
                        {
                            // Intestazione personalizzata dal template
                            if (studio?.Logo != null)
                            {
                                col.Item().MaxHeight(40).AlignCenter().Image(studio.Logo).FitHeight();
                                col.Item().Height(3);
                            }
                            foreach (var line in intestazionePlain.Split('\n').Take(3)) // Max 3 righe
                            {
                                if (!string.IsNullOrWhiteSpace(line))
                                    col.Item().AlignCenter().Text(line.Trim()).FontSize(9);
                            }
                        }
                        else
                        {
                            // Intestazione default da ConfigurazioneStudio
                            if (studio?.Logo != null)
                            {
                                col.Item().MaxHeight(45).AlignCenter().Image(studio.Logo).FitHeight();
                                col.Item().Height(5);
                            }
                            
                            if (!string.IsNullOrEmpty(studio?.NomeStudio))
                            {
                                col.Item().AlignCenter().Text(studio.NomeStudio).Bold().FontSize(12);
                            }
                            
                            if (!string.IsNullOrEmpty(studio?.Indirizzo))
                            {
                                var indirizzo = $"{studio.Indirizzo}";
                                if (!string.IsNullOrEmpty(studio.CAP)) indirizzo += $" - {studio.CAP}";
                                if (!string.IsNullOrEmpty(studio.Citta)) indirizzo += $" {studio.Citta}";
                                col.Item().AlignCenter().Text(indirizzo).FontSize(8);
                            }
                        }
                        
                        col.Item().Height(3);
                        col.Item().LineHorizontal(0.5f);
                        col.Item().Height(5);
                    });

                    // Contenuto - Testo con formattazione mantenuta
                    page.Content().Column(col =>
                    {
                        foreach (var part in contentParts)
                        {
                            if (part.IsTable)
                            {
                                // Renderizza tabella
                                RenderTableToPdf(col, part.TableRows, part.TableHeaders);
                                col.Item().Height(10);
                            }
                            else if (part.Paragraphs.Count > 0)
                            {
                                // Renderizza paragrafi formattati
                                foreach (var paragraph in part.Paragraphs)
                                {
                                    RenderFormattedParagraphToPdf(col, paragraph);
                                }
                            }
                            else if (!string.IsNullOrWhiteSpace(part.Text))
                            {
                                // Fallback: testo semplice
                                col.Item().Text(text => 
                                {
                                    text.Justify();
                                    text.DefaultTextStyle(x => x.LineHeight(1.5f));
                                    text.Span(part.Text);
                                });
                                col.Item().Height(8);
                            }
                        }
                    });

                    // Footer - usa piè di pagina personalizzato se presente (max 2 righe)
                    page.Footer().Column(col =>
                    {
                        if (!string.IsNullOrEmpty(footerPlain))
                        {
                            col.Item().LineHorizontal(0.5f);
                            col.Item().Height(3);
                            foreach (var line in footerPlain.Split('\n').Take(2))
                            {
                                if (!string.IsNullOrWhiteSpace(line))
                                {
                                    var lineText = line.Trim();
                                    // Gestisci tag numerazione pagine
                                    if (lineText.Contains("{{PaginaDi}}"))
                                    {
                                        // Tag completo "Pagina X di Y"
                                        var parts = lineText.Split(new[] { "{{PaginaDi}}" }, StringSplitOptions.None);
                                        col.Item().AlignCenter().Text(text =>
                                        {
                                            text.DefaultTextStyle(x => x.FontSize(9));
                                            if (parts.Length > 0 && !string.IsNullOrEmpty(parts[0]))
                                                text.Span(parts[0]);
                                            text.Span("Pagina ");
                                            text.CurrentPageNumber();
                                            text.Span(" di ");
                                            text.TotalPages();
                                            if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1]))
                                                text.Span(parts[1]);
                                        });
                                    }
                                    else if (lineText.Contains("{{Pagina}}") || lineText.Contains("{{TotalePagine}}"))
                                    {
                                        col.Item().AlignCenter().Text(text =>
                                        {
                                            text.DefaultTextStyle(x => x.FontSize(9));
                                            var remaining = lineText;
                                            while (remaining.Length > 0)
                                            {
                                                var paginaIdx = remaining.IndexOf("{{Pagina}}");
                                                var totaleIdx = remaining.IndexOf("{{TotalePagine}}");
                                                
                                                if (paginaIdx < 0 && totaleIdx < 0)
                                                {
                                                    text.Span(remaining);
                                                    break;
                                                }
                                                
                                                var nextIdx = paginaIdx >= 0 && (totaleIdx < 0 || paginaIdx < totaleIdx) ? paginaIdx : totaleIdx;
                                                var isPagina = nextIdx == paginaIdx && paginaIdx >= 0;
                                                
                                                if (nextIdx > 0)
                                                    text.Span(remaining.Substring(0, nextIdx));
                                                
                                                if (isPagina)
                                                {
                                                    text.CurrentPageNumber();
                                                    remaining = remaining.Substring(nextIdx + "{{Pagina}}".Length);
                                                }
                                                else
                                                {
                                                    text.TotalPages();
                                                    remaining = remaining.Substring(nextIdx + "{{TotalePagine}}".Length);
                                                }
                                            }
                                        });
                                    }
                                    else
                                    {
                                        col.Item().AlignCenter().Text(lineText).FontSize(9);
                                    }
                                }
                            }
                            col.Item().Height(5);
                        }
                        else
                        {
                            // Numero pagina default se non c'è footer personalizzato
                            col.Item().AlignCenter().Text(text =>
                            {
                                text.Span("Pagina ");
                                text.CurrentPageNumber();
                                text.Span(" di ");
                                text.TotalPages();
                            });
                        }
                    });
                });
            });

            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            return stream.ToArray();
        }

        /// <summary>
        /// Genera Word DOCX usando DocumentFormat.OpenXml
        /// </summary>
        private byte[] GeneraWord(string? intestazione, string htmlContent, string? piePagina, ConfigurazioneStudio? studio)
        {
            var intestazionePlain = !string.IsNullOrEmpty(intestazione) ? RimuoviTagHtml(intestazione) : null;
            var footerPlain = !string.IsNullOrEmpty(piePagina) ? RimuoviTagHtml(piePagina) : null;
            
            // Estrai le parti del contenuto (testo e tabelle) - stesso metodo usato per PDF
            var contentParts = ParseHtmlContent(htmlContent);

            using var stream = new MemoryStream();
            
            using (var wordDocument = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
            {
                // Aggiungi parti principali
                var mainPart = wordDocument.AddMainDocumentPart();
                mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
                var body = mainPart.Document.AppendChild(new Body());

                // ===== CREA HEADER PART (Intestazione vera di Word) =====
                string? headerPartId = null;
                if (!string.IsNullOrEmpty(intestazionePlain) || studio?.Logo != null || !string.IsNullOrEmpty(studio?.NomeStudio))
                {
                    var headerPart = mainPart.AddNewPart<HeaderPart>();
                    headerPartId = mainPart.GetIdOfPart(headerPart);
                    var header = new Header();

                    // Aggiungi logo se presente
                    if (studio?.Logo != null)
                    {
                        var logoPara = CreateImageParagraphForPart(headerPart, studio.Logo, studio.LogoContentType ?? "image/png", 1500000, 600000);
                        header.AppendChild(logoPara);
                    }

                    // Aggiungi intestazione personalizzata o nome studio
                    if (!string.IsNullOrEmpty(intestazionePlain))
                    {
                        foreach (var line in intestazionePlain.Split('\n'))
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                var headerPara = new Paragraph();
                                var headerRun = new Run();
                                var headerProps = new RunProperties();
                                headerProps.AppendChild(new RunFonts { Ascii = "Arial", HighAnsi = "Arial" });
                                headerProps.AppendChild(new FontSize { Val = "20" }); // 10pt
                                headerProps.AppendChild(new Bold());
                                headerRun.AppendChild(headerProps);
                                headerRun.AppendChild(new Text(line.Trim()) { Space = SpaceProcessingModeValues.Preserve });
                                headerPara.AppendChild(headerRun);
                                var paraProps = new ParagraphProperties();
                                paraProps.AppendChild(new Justification { Val = JustificationValues.Center });
                                headerPara.PrependChild(paraProps);
                                header.AppendChild(headerPara);
                            }
                        }
                    }
                    else if (studio != null && !string.IsNullOrEmpty(studio.NomeStudio))
                    {
                        // Nome studio
                        var headerPara = new Paragraph();
                        var headerRun = new Run();
                        var headerProps = new RunProperties();
                        headerProps.AppendChild(new RunFonts { Ascii = "Arial", HighAnsi = "Arial" });
                        headerProps.AppendChild(new FontSize { Val = "24" }); // 12pt
                        headerProps.AppendChild(new Bold());
                        headerRun.AppendChild(headerProps);
                        headerRun.AppendChild(new Text(studio.NomeStudio) { Space = SpaceProcessingModeValues.Preserve });
                        headerPara.AppendChild(headerRun);
                        var paraProps = new ParagraphProperties();
                        paraProps.AppendChild(new Justification { Val = JustificationValues.Center });
                        headerPara.PrependChild(paraProps);
                        header.AppendChild(headerPara);

                        // Aggiungi indirizzo
                        if (!string.IsNullOrEmpty(studio.Indirizzo))
                        {
                            var indirizzo = studio.Indirizzo;
                            if (!string.IsNullOrEmpty(studio.CAP)) indirizzo += $" - {studio.CAP}";
                            if (!string.IsNullOrEmpty(studio.Citta)) indirizzo += $" {studio.Citta}";

                            var addrPara = new Paragraph();
                            var addrRun = new Run();
                            var addrProps = new RunProperties();
                            addrProps.AppendChild(new RunFonts { Ascii = "Arial", HighAnsi = "Arial" });
                            addrProps.AppendChild(new FontSize { Val = "18" }); // 9pt
                            addrRun.AppendChild(addrProps);
                            addrRun.AppendChild(new Text(indirizzo) { Space = SpaceProcessingModeValues.Preserve });
                            addrPara.AppendChild(addrRun);
                            var addrParaProps = new ParagraphProperties();
                            addrParaProps.AppendChild(new Justification { Val = JustificationValues.Center });
                            addrPara.PrependChild(addrParaProps);
                            header.AppendChild(addrPara);
                        }
                    }

                    headerPart.Header = header;
                    headerPart.Header.Save();
                }

                // ===== CREA FOOTER PART (Piè di pagina vero di Word) =====
                string? footerPartId = null;
                if (!string.IsNullOrEmpty(footerPlain))
                {
                    var footerPart = mainPart.AddNewPart<FooterPart>();
                    footerPartId = mainPart.GetIdOfPart(footerPart);
                    var footer = new Footer();

                    // Linea orizzontale sopra il footer
                    var linePara = new Paragraph();
                    var lineParaProps = new ParagraphProperties();
                    var bottomBorder = new ParagraphBorders();
                    bottomBorder.AppendChild(new TopBorder { Val = BorderValues.Single, Size = 4, Color = "000000" });
                    lineParaProps.AppendChild(bottomBorder);
                    linePara.AppendChild(lineParaProps);
                    footer.AppendChild(linePara);

                    foreach (var line in footerPlain.Split('\n').Take(2)) // Max 2 righe
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            var footerPara = new Paragraph();
                            var footerRun = new Run();
                            var footerProps = new RunProperties();
                            footerProps.AppendChild(new RunFonts { Ascii = "Arial", HighAnsi = "Arial" });
                            footerProps.AppendChild(new FontSize { Val = "18" }); // 9pt
                            footerRun.AppendChild(footerProps);
                            
                            // Gestisci numerazione pagine
                            var lineText = line.Trim();
                            if (lineText.Contains("{{PaginaDi}}") || lineText.Contains("{{Pagina}}"))
                            {
                                // Sostituisci con campi numerazione Word
                                lineText = lineText.Replace("{{PaginaDi}}", "");
                                lineText = lineText.Replace("{{Pagina}}", "");
                                lineText = lineText.Replace("{{TotalePagine}}", "");
                                footerRun.AppendChild(new Text("Pagina ") { Space = SpaceProcessingModeValues.Preserve });
                                footerRun.AppendChild(new FieldChar { FieldCharType = FieldCharValues.Begin });
                                footerRun.AppendChild(new FieldCode(" PAGE ") { Space = SpaceProcessingModeValues.Preserve });
                                footerRun.AppendChild(new FieldChar { FieldCharType = FieldCharValues.Separate });
                                footerRun.AppendChild(new Text("1"));
                                footerRun.AppendChild(new FieldChar { FieldCharType = FieldCharValues.End });
                                footerRun.AppendChild(new Text(" di ") { Space = SpaceProcessingModeValues.Preserve });
                                footerRun.AppendChild(new FieldChar { FieldCharType = FieldCharValues.Begin });
                                footerRun.AppendChild(new FieldCode(" NUMPAGES ") { Space = SpaceProcessingModeValues.Preserve });
                                footerRun.AppendChild(new FieldChar { FieldCharType = FieldCharValues.Separate });
                                footerRun.AppendChild(new Text("1"));
                                footerRun.AppendChild(new FieldChar { FieldCharType = FieldCharValues.End });
                            }
                            else
                            {
                                footerRun.AppendChild(new Text(lineText) { Space = SpaceProcessingModeValues.Preserve });
                            }
                            
                            footerPara.AppendChild(footerRun);
                            var paraProps = new ParagraphProperties();
                            paraProps.AppendChild(new Justification { Val = JustificationValues.Center });
                            footerPara.PrependChild(paraProps);
                            footer.AppendChild(footerPara);
                        }
                    }

                    footerPart.Footer = footer;
                    footerPart.Footer.Save();
                }

                // ===== AGGIUNGI CONTENUTO (solo il body) =====
                foreach (var part in contentParts)
                {
                    if (part.IsTable)
                    {
                        // Crea tabella Word
                        var wordTable = CreateWordTable(part.TableHeaders, part.TableRows);
                        body.AppendChild(wordTable);
                        body.AppendChild(new Paragraph()); // Spazio dopo tabella
                    }
                    else if (part.Paragraphs.Count > 0)
                    {
                        // Aggiungi paragrafi formattati
                        foreach (var formattedPara in part.Paragraphs)
                        {
                            var wordPara = CreateFormattedWordParagraph(formattedPara);
                            body.AppendChild(wordPara);
                        }
                        body.AppendChild(new Paragraph()); // Spazio tra blocchi
                    }
                    else if (!string.IsNullOrWhiteSpace(part.Text))
                    {
                        // Fallback: aggiungi testo normale
                        var paragraph = new Paragraph();
                        var run = new Run();
                        
                        // Imposta font
                        var runProperties = new RunProperties();
                        runProperties.AppendChild(new RunFonts { Ascii = "Arial", HighAnsi = "Arial" });
                        runProperties.AppendChild(new FontSize { Val = "22" }); // 11pt = 22 half-points
                        run.AppendChild(runProperties);
                        
                        run.AppendChild(new Text(part.Text) { Space = SpaceProcessingModeValues.Preserve });
                        paragraph.AppendChild(run);
                        
                        // Giustifica il testo
                        var paraProperties = new ParagraphProperties();
                        paraProperties.AppendChild(new Justification { Val = JustificationValues.Both });
                        paragraph.PrependChild(paraProperties);
                        
                        body.AppendChild(paragraph);
                        body.AppendChild(new Paragraph()); // Spazio tra paragrafi
                    }
                }

                // ===== IMPOSTA SECTION PROPERTIES con riferimenti a Header/Footer =====
                var sectionProperties = new SectionProperties();
                
                // Margini pagina
                var pageMargin = new PageMargin
                {
                    Top = 1440,    // 1 inch = 1440 twips
                    Right = 1440,
                    Bottom = 1440,
                    Left = 1440,
                    Header = 720,  // 0.5 inch per header
                    Footer = 720   // 0.5 inch per footer
                };
                sectionProperties.AppendChild(pageMargin);

                // Riferimento all'header
                if (headerPartId != null)
                {
                    sectionProperties.AppendChild(new HeaderReference 
                    { 
                        Type = HeaderFooterValues.Default, 
                        Id = headerPartId 
                    });
                }

                // Riferimento al footer
                if (footerPartId != null)
                {
                    sectionProperties.AppendChild(new FooterReference 
                    { 
                        Type = HeaderFooterValues.Default, 
                        Id = footerPartId 
                    });
                }

                body.AppendChild(sectionProperties);

                mainPart.Document.Save();
            }

            return stream.ToArray();
        }

        /// <summary>
        /// Crea un paragrafo con immagine per HeaderPart
        /// </summary>
        private Paragraph CreateImageParagraphForPart(HeaderPart headerPart, byte[] imageData, string contentType, long maxWidth, long maxHeight)
        {
            var (actualWidth, actualHeight) = GetImageDimensions(imageData, contentType);
            
            // Calcola dimensioni proporzionali
            long cx = maxWidth;
            long cy = maxHeight;
            
            if (actualWidth > 0 && actualHeight > 0)
            {
                double aspectRatio = (double)actualWidth / actualHeight;
                double maxAspect = (double)maxWidth / maxHeight;
                
                if (aspectRatio > maxAspect)
                {
                    cx = maxWidth;
                    cy = (long)(maxWidth / aspectRatio);
                }
                else
                {
                    cy = maxHeight;
                    cx = (long)(maxHeight * aspectRatio);
                }
            }

            // Determina il tipo di immagine e content type per ImagePart
            var imageContentType = contentType.ToLower() switch
            {
                "image/png" => "image/png",
                "image/gif" => "image/gif",
                "image/bmp" => "image/bmp",
                _ => "image/jpeg"
            };

            // Aggiungi immagine alla HeaderPart usando AddNewPart
            var imagePart = headerPart.AddNewPart<ImagePart>(imageContentType, "rIdImage1");
            using (var imageStream = new MemoryStream(imageData))
            {
                imagePart.FeedData(imageStream);
            }

            // Ottieni relationship ID
            string relationshipId = headerPart.GetIdOfPart(imagePart);

            // Crea elemento immagine
            var element = new Drawing(
                new DW.Inline(
                    new DW.Extent() { Cx = cx, Cy = cy },
                    new DW.EffectExtent() { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                    new DW.DocProperties() { Id = 1U, Name = "Logo" },
                    new DW.NonVisualGraphicFrameDrawingProperties(
                        new A.GraphicFrameLocks() { NoChangeAspect = true }),
                    new A.Graphic(
                        new A.GraphicData(
                            new PIC.Picture(
                                new PIC.NonVisualPictureProperties(
                                    new PIC.NonVisualDrawingProperties() { Id = 0U, Name = "Image.png" },
                                    new PIC.NonVisualPictureDrawingProperties()),
                                new PIC.BlipFill(
                                    new A.Blip() { Embed = relationshipId },
                                    new A.Stretch(new A.FillRectangle())),
                                new PIC.ShapeProperties(
                                    new A.Transform2D(
                                        new A.Offset() { X = 0L, Y = 0L },
                                        new A.Extents() { Cx = cx, Cy = cy }),
                                    new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle }))
                        ) { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" })
                ) { DistanceFromTop = 0U, DistanceFromBottom = 0U, DistanceFromLeft = 0U, DistanceFromRight = 0U }
            );

            // Centra l'immagine
            var paragraph = new Paragraph();
            var paragraphProperties = new ParagraphProperties();
            paragraphProperties.AppendChild(new Justification { Val = JustificationValues.Center });
            paragraph.AppendChild(paragraphProperties);
            paragraph.AppendChild(new Run(element));

            return paragraph;
        }

        /// <summary>
        /// Crea una tabella Word da righe e headers
        /// </summary>
        private DocumentFormat.OpenXml.Wordprocessing.Table CreateWordTable(List<string> headers, List<List<string>> rows)
        {
            var table = new DocumentFormat.OpenXml.Wordprocessing.Table();

            // Proprietà della tabella
            var tableProperties = new TableProperties();
            
            // Bordi della tabella
            var tableBorders = new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
                new BottomBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
                new LeftBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
                new RightBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4, Color = "000000" }
            );
            tableProperties.AppendChild(tableBorders);
            
            // Larghezza tabella al 100% della pagina (rispetta margini)
            tableProperties.AppendChild(new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct });
            
            // Layout fisso per rispettare le proporzioni
            tableProperties.AppendChild(new TableLayout { Type = TableLayoutValues.Fixed });
            
            table.AppendChild(tableProperties);

            // Determina il numero di colonne
            int columnCount = headers.Count;
            foreach (var row in rows)
            {
                if (row.Count > columnCount) columnCount = row.Count;
            }
            if (columnCount == 0) return table;

            // Calcola larghezze colonne in twips (1 inch = 1440 twips)
            // Larghezza utile pagina A4 con margini 2.5cm: circa 6.5 inches = 9360 twips
            int totalWidth = 9360;
            var columnWidths = new List<int>();
            
            if (columnCount == 3)
            {
                // Tabella tipo antiriciclaggio: Controllo | Sì | No
                columnWidths.Add((int)(totalWidth * 0.80)); // 80%
                columnWidths.Add((int)(totalWidth * 0.10)); // 10%
                columnWidths.Add((int)(totalWidth * 0.10)); // 10%
            }
            else if (columnCount == 4)
            {
                // Tabella 4 colonne: (X) | Documentazione | Osservazioni | Annotazioni
                columnWidths.Add((int)(totalWidth * 0.05)); // 5% - spunta
                columnWidths.Add((int)(totalWidth * 0.30)); // 30% - documentazione
                columnWidths.Add((int)(totalWidth * 0.45)); // 45% - osservazioni
                columnWidths.Add((int)(totalWidth * 0.20)); // 20% - annotazioni
            }
            else
            {
                // Distribuzione equa
                int colWidth = totalWidth / columnCount;
                for (int i = 0; i < columnCount; i++)
                    columnWidths.Add(colWidth);
            }

            // Definizione griglia colonne
            var tableGrid = new TableGrid();
            foreach (var width in columnWidths)
            {
                tableGrid.AppendChild(new GridColumn { Width = width.ToString() });
            }
            table.AppendChild(tableGrid);

            // Riga header
            if (headers.Count > 0)
            {
                var headerRow = new TableRow();
                for (int i = 0; i < columnCount; i++)
                {
                    var cellText = i < headers.Count ? headers[i] : "";
                    var width = i < columnWidths.Count ? columnWidths[i] : 1000;
                    // Per tabelle a 4 colonne, prima colonna centrata, resto a sinistra
                    var align = columnCount == 4 
                        ? (i == 0 ? JustificationValues.Center : JustificationValues.Left)
                        : (i == 0 ? JustificationValues.Left : JustificationValues.Center);
                    var cell = CreateWordTableCell(cellText, true, align, width);
                    headerRow.AppendChild(cell);
                }
                table.AppendChild(headerRow);
            }

            // Righe dati
            foreach (var row in rows)
            {
                var tableRow = new TableRow();
                for (int i = 0; i < columnCount; i++)
                {
                    var cellText = i < row.Count ? row[i] : "";
                    var width = i < columnWidths.Count ? columnWidths[i] : 1000;
                    // Per tabelle a 4 colonne, prima colonna centrata, resto a sinistra
                    var align = columnCount == 4 
                        ? (i == 0 ? JustificationValues.Center : JustificationValues.Left)
                        : (i == 0 ? JustificationValues.Left : JustificationValues.Center);
                    var cell = CreateWordTableCell(cellText, false, align, width);
                    tableRow.AppendChild(cell);
                }
                table.AppendChild(tableRow);
            }

            return table;
        }

        /// <summary>
        /// Crea una cella di tabella Word
        /// </summary>
        private TableCell CreateWordTableCell(string text, bool isHeader, JustificationValues alignment, int widthTwips)
        {
            var cell = new TableCell();
            
            // Proprietà cella
            var cellProperties = new TableCellProperties();
            
            // Larghezza cella
            cellProperties.AppendChild(new TableCellWidth { Width = widthTwips.ToString(), Type = TableWidthUnitValues.Dxa });
            
            cellProperties.AppendChild(new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center });
            
            // Bordi cella
            var cellBorders = new TableCellBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
                new BottomBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
                new LeftBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
                new RightBorder { Val = BorderValues.Single, Size = 4, Color = "000000" }
            );
            cellProperties.AppendChild(cellBorders);
            
            // Padding cella ridotto per risparmiare spazio
            cellProperties.AppendChild(new TableCellMargin(
                new TopMargin { Width = "30", Type = TableWidthUnitValues.Dxa },
                new BottomMargin { Width = "30", Type = TableWidthUnitValues.Dxa },
                new LeftMargin { Width = "50", Type = TableWidthUnitValues.Dxa },
                new RightMargin { Width = "50", Type = TableWidthUnitValues.Dxa }
            ));
            
            cell.AppendChild(cellProperties);

            // Paragrafo con testo
            var paragraph = new Paragraph();
            var paragraphProperties = new ParagraphProperties();
            paragraphProperties.AppendChild(new Justification { Val = alignment });
            // Riduci spaziatura paragrafo
            paragraphProperties.AppendChild(new SpacingBetweenLines { After = "0", Before = "0" });
            paragraph.AppendChild(paragraphProperties);

            var run = new Run();
            var runProperties = new RunProperties();
            runProperties.AppendChild(new RunFonts { Ascii = "Arial", HighAnsi = "Arial" });
            runProperties.AppendChild(new FontSize { Val = "18" }); // 9pt per risparmiare spazio
            if (isHeader)
            {
                runProperties.AppendChild(new Bold());
            }
            run.AppendChild(runProperties);
            run.AppendChild(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
            
            paragraph.AppendChild(run);
            cell.AppendChild(paragraph);

            return cell;
        }

        /// <summary>
        /// Classe per rappresentare uno span di testo con formattazione
        /// </summary>
        private class FormattedSpan
        {
            public string Text { get; set; } = "";
            public bool IsBold { get; set; }
            public bool IsItalic { get; set; }
            public bool IsUnderline { get; set; }
            public int? FontSizePt { get; set; } // null = default
        }

        /// <summary>
        /// Classe per rappresentare un paragrafo formattato
        /// </summary>
        private class FormattedParagraph
        {
            public List<FormattedSpan> Spans { get; set; } = new();
            public string Alignment { get; set; } = "justify"; // left, center, right, justify
            public bool IsHeading { get; set; }
            public int HeadingLevel { get; set; } = 0; // 1-6 for h1-h6
            public float LineHeight { get; set; } = 1.5f; // Interlinea (1.0, 1.15, 1.5, 2.0, etc.)
            public int? SpacingBeforePt { get; set; } // Spaziatura prima del paragrafo in pt
            public int? SpacingAfterPt { get; set; } // Spaziatura dopo il paragrafo in pt
            public int? FontSizePt { get; set; } // Dimensione font del paragrafo (se impostata)
            public bool IsBulletPoint { get; set; } // Se è un elemento di lista (li)
        }

        /// <summary>
        /// Classe per rappresentare una parte del contenuto (testo o tabella)
        /// </summary>
        private class ContentPart
        {
            public bool IsTable { get; set; }
            public string Text { get; set; } = ""; // Per retrocompatibilità
            public string RawHtml { get; set; } = ""; // HTML originale del blocco
            public List<FormattedParagraph> Paragraphs { get; set; } = new();
            public List<string> TableHeaders { get; set; } = new();
            public List<List<string>> TableRows { get; set; } = new();
        }

        /// <summary>
        /// Parsa il contenuto HTML e separa testo e tabelle, mantenendo la formattazione
        /// </summary>
        private List<ContentPart> ParseHtmlContent(string html)
        {
            var parts = new List<ContentPart>();
            if (string.IsNullOrEmpty(html)) return parts;

            // Pattern per trovare le tabelle
            var tablePattern = @"<table[^>]*>(.*?)</table>";
            var matches = Regex.Matches(html, tablePattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

            int lastIndex = 0;
            foreach (Match match in matches)
            {
                // Aggiungi testo prima della tabella
                if (match.Index > lastIndex)
                {
                    var textBefore = html.Substring(lastIndex, match.Index - lastIndex);
                    var contentPart = ParseHtmlToFormattedContent(textBefore);
                    if (contentPart.Paragraphs.Count > 0)
                    {
                        parts.Add(contentPart);
                    }
                }

                // Parsa la tabella
                var tableHtml = match.Value;
                var tablePart = ParseHtmlTable(tableHtml);
                if (tablePart.TableRows.Count > 0 || tablePart.TableHeaders.Count > 0)
                {
                    parts.Add(tablePart);
                }

                lastIndex = match.Index + match.Length;
            }

            // Aggiungi testo dopo l'ultima tabella (solo se c'erano tabelle)
            if (matches.Count > 0 && lastIndex < html.Length)
            {
                var textAfter = html.Substring(lastIndex);
                var contentPart = ParseHtmlToFormattedContent(textAfter);
                if (contentPart.Paragraphs.Count > 0)
                {
                    parts.Add(contentPart);
                }
            }

            // Se non ci sono tabelle, tratta tutto come contenuto formattato
            if (matches.Count == 0)
            {
                var contentPart = ParseHtmlToFormattedContent(html);
                if (contentPart.Paragraphs.Count > 0)
                {
                    parts.Add(contentPart);
                }
            }

            return parts;
        }

        /// <summary>
        /// Parsa HTML in paragrafi formattati
        /// </summary>
        private ContentPart ParseHtmlToFormattedContent(string html)
        {
            var part = new ContentPart { IsTable = false, RawHtml = html };
            
            if (string.IsNullOrEmpty(html)) return part;
            
            // Prima estrai tutti i list items <li>...</li>
            var listItemPattern = @"<li[^>]*>(.*?)</li>";
            var listItemMatches = Regex.Matches(html, listItemPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            
            // Sostituisci temporaneamente i list items con placeholder per processarli separatamente
            var listItems = new List<(string content, string outerHtml)>();
            foreach (Match match in listItemMatches)
            {
                listItems.Add((match.Groups[1].Value, match.Value));
            }
            
            // Rimuovi liste e list items dal HTML principale
            var htmlWithoutLists = Regex.Replace(html, @"<ul[^>]*>.*?</ul>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            htmlWithoutLists = Regex.Replace(htmlWithoutLists, @"<ol[^>]*>.*?</ol>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            
            // Processa i paragrafi normali (p, div, h1-h6)
            var blockPattern = @"<(p|div|h[1-6])[^>]*>(.*?)</\1>";
            var blockMatches = Regex.Matches(htmlWithoutLists, blockPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

            foreach (Match blockMatch in blockMatches)
            {
                var tagName = blockMatch.Groups[1].Value.ToLower();
                var innerHtml = blockMatch.Groups[2].Value;
                
                // Salta blocchi vuoti
                if (string.IsNullOrWhiteSpace(RimuoviTagHtml(innerHtml)))
                    continue;

                var paragraph = new FormattedParagraph();
                
                // Determina allineamento dal tag o style
                paragraph.Alignment = ExtractAlignment(blockMatch.Value);
                
                // Estrai line-height
                paragraph.LineHeight = ExtractLineHeight(blockMatch.Value);
                
                // Estrai spaziatura (margin-top, margin-bottom)
                var (spacingBefore, spacingAfter) = ExtractSpacing(blockMatch.Value);
                paragraph.SpacingBeforePt = spacingBefore;
                paragraph.SpacingAfterPt = spacingAfter;
                
                // Estrai font-size dal paragrafo
                paragraph.FontSizePt = ExtractFontSize(blockMatch.Value);
                
                // Verifica se è un heading
                if (tagName.StartsWith("h") && tagName.Length == 2 && char.IsDigit(tagName[1]))
                {
                    paragraph.IsHeading = true;
                    paragraph.HeadingLevel = int.Parse(tagName[1].ToString());
                }

                // Parsa gli span formattati nel contenuto (passando il font-size base del paragrafo)
                paragraph.Spans = ParseFormattedSpans(innerHtml, paragraph.FontSizePt);

                if (paragraph.Spans.Count > 0)
                {
                    part.Paragraphs.Add(paragraph);
                }
            }
            
            // Processa i list items separatamente
            foreach (var (content, outerHtml) in listItems)
            {
                if (string.IsNullOrWhiteSpace(RimuoviTagHtml(content)))
                    continue;
                    
                var paragraph = new FormattedParagraph();
                paragraph.IsBulletPoint = true;
                paragraph.Alignment = ExtractAlignment(outerHtml);
                paragraph.LineHeight = ExtractLineHeight(outerHtml);
                paragraph.FontSizePt = ExtractFontSize(outerHtml);
                
                var (spacingBefore, spacingAfter) = ExtractSpacing(outerHtml);
                paragraph.SpacingBeforePt = spacingBefore;
                paragraph.SpacingAfterPt = spacingAfter;
                
                paragraph.Spans = ParseFormattedSpans(content, paragraph.FontSizePt);
                
                if (paragraph.Spans.Count > 0)
                {
                    part.Paragraphs.Add(paragraph);
                }
            }

            // Se non abbiamo trovato blocchi strutturati, prova testo semplice
            if (part.Paragraphs.Count == 0)
            {
                var plainText = RimuoviTagHtml(html).Trim();
                if (!string.IsNullOrEmpty(plainText))
                {
                    var lines = plainText.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            var para = new FormattedParagraph();
                            para.Spans.Add(new FormattedSpan { Text = line.Trim() });
                            part.Paragraphs.Add(para);
                        }
                    }
                }
            }

            // Fallback per testo semplice (retrocompatibilità)
            part.Text = RimuoviTagHtml(html).Trim();

            return part;
        }

        /// <summary>
        /// Estrae l'allineamento da un tag HTML
        /// </summary>
        private string ExtractAlignment(string tagHtml)
        {
            // Cerca align="..." o style="...text-align:..."
            var alignMatch = Regex.Match(tagHtml, @"align\s*=\s*[""']?(left|center|right|justify)[""']?", RegexOptions.IgnoreCase);
            if (alignMatch.Success)
                return alignMatch.Groups[1].Value.ToLower();

            var styleMatch = Regex.Match(tagHtml, @"text-align\s*:\s*(left|center|right|justify)", RegexOptions.IgnoreCase);
            if (styleMatch.Success)
                return styleMatch.Groups[1].Value.ToLower();

            return "justify"; // Default
        }

        /// <summary>
        /// Estrae line-height da un tag HTML
        /// </summary>
        private float ExtractLineHeight(string tagHtml)
        {
            // Cerca line-height: X o line-height: Xpx/em/%
            var match = Regex.Match(tagHtml, @"line-height\s*:\s*(\d+\.?\d*)(px|em|%)?", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                if (float.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Float, 
                    System.Globalization.CultureInfo.InvariantCulture, out float value))
                {
                    var unit = match.Groups[2].Value.ToLower();
                    
                    // Se è in px, converti approssimativamente
                    if (unit == "px")
                        return value / 16f; // Assume 16px = 1em base
                    
                    // Se è in percentuale, converti
                    if (unit == "%")
                        return value / 100f;
                    
                    // Altrimenti è già un moltiplicatore (es: 1.5, 2)
                    return value;
                }
            }
            
            return 1.5f; // Default
        }

        /// <summary>
        /// Estrae font-size da un tag HTML
        /// </summary>
        private int? ExtractFontSize(string tagHtml)
        {
            // Cerca font-size: Xpt, Xpx, X (supporta decimali)
            var match = Regex.Match(tagHtml, @"font-size\s*:\s*(\d+\.?\d*)(pt|px)?", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                if (float.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float value))
                {
                    var unit = match.Groups[2].Value.ToLower();
                    
                    // Se è in px, converti in pt (1pt = 1.333px circa, quindi px * 0.75 = pt)
                    if (unit == "px")
                        value = value * 0.75f;
                    
                    return (int)Math.Round(value);
                }
            }
            
            return null; // Non specificato
        }

        /// <summary>
        /// Estrae spaziatura (margin) da un tag HTML
        /// </summary>
        private (int? before, int? after) ExtractSpacing(string tagHtml)
        {
            int? before = null;
            int? after = null;
            
            // Cerca margin-top
            var marginTopMatch = Regex.Match(tagHtml, @"margin-top\s*:\s*(\d+)(px|pt)?", RegexOptions.IgnoreCase);
            if (marginTopMatch.Success)
            {
                if (int.TryParse(marginTopMatch.Groups[1].Value, out int value))
                {
                    // Converti px in pt approssimativamente
                    if (marginTopMatch.Groups[2].Value.ToLower() == "px")
                        value = (int)(value * 0.75);
                    before = value;
                }
            }
            
            // Cerca margin-bottom
            var marginBottomMatch = Regex.Match(tagHtml, @"margin-bottom\s*:\s*(\d+)(px|pt)?", RegexOptions.IgnoreCase);
            if (marginBottomMatch.Success)
            {
                if (int.TryParse(marginBottomMatch.Groups[1].Value, out int value))
                {
                    if (marginBottomMatch.Groups[2].Value.ToLower() == "px")
                        value = (int)(value * 0.75);
                    after = value;
                }
            }
            
            return (before, after);
        }

        /// <summary>
        /// Parsa il contenuto HTML in span formattati (bold, italic, underline)
        /// </summary>
        /// <param name="html">HTML da parsare</param>
        /// <param name="baseFontSize">Font-size base del paragrafo (opzionale)</param>
        private List<FormattedSpan> ParseFormattedSpans(string html, int? baseFontSize = null)
        {
            var spans = new List<FormattedSpan>();
            if (string.IsNullOrEmpty(html)) return spans;

            // Pattern per trovare tag di formattazione
            var pattern = @"<(strong|b|em|i|u|span)[^>]*>(.*?)</\1>|([^<]+)|<[^>]+>";
            var matches = Regex.Matches(html, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

            foreach (Match match in matches)
            {
                var tagName = match.Groups[1].Value.ToLower();
                var innerContent = match.Groups[2].Success ? match.Groups[2].Value : "";
                var plainText = match.Groups[3].Success ? match.Groups[3].Value : "";

                if (!string.IsNullOrEmpty(plainText))
                {
                    // Testo semplice senza formattazione - applica font-size base
                    var text = System.Web.HttpUtility.HtmlDecode(plainText);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        spans.Add(new FormattedSpan { Text = text, FontSizePt = baseFontSize });
                    }
                }
                else if (!string.IsNullOrEmpty(tagName))
                {
                    // Tag di formattazione
                    var innerText = RimuoviTagHtml(innerContent);
                    if (!string.IsNullOrWhiteSpace(innerText))
                    {
                        var span = new FormattedSpan { Text = innerText };
                        
                        // Imposta font-size base (può essere sovrascritto dallo span)
                        span.FontSizePt = baseFontSize;

                        // Determina formattazione
                        if (tagName == "strong" || tagName == "b")
                            span.IsBold = true;
                        else if (tagName == "em" || tagName == "i")
                            span.IsItalic = true;
                        else if (tagName == "u")
                            span.IsUnderline = true;
                        else if (tagName == "span")
                        {
                            // Controlla stili inline
                            if (Regex.IsMatch(match.Value, @"font-weight\s*:\s*bold", RegexOptions.IgnoreCase))
                                span.IsBold = true;
                            if (Regex.IsMatch(match.Value, @"font-style\s*:\s*italic", RegexOptions.IgnoreCase))
                                span.IsItalic = true;
                            if (Regex.IsMatch(match.Value, @"text-decoration[^;]*underline", RegexOptions.IgnoreCase))
                                span.IsUnderline = true;

                            // Controlla font-size (supporta decimali: 8pt, 10.6667px, etc.)
                            var sizeMatch = Regex.Match(match.Value, @"font-size\s*:\s*(\d+\.?\d*)(pt|px)?", RegexOptions.IgnoreCase);
                            if (sizeMatch.Success)
                            {
                                if (float.TryParse(sizeMatch.Groups[1].Value, 
                                    System.Globalization.NumberStyles.Float,
                                    System.Globalization.CultureInfo.InvariantCulture, 
                                    out float size))
                                {
                                    // Converti px in pt approssimativamente
                                    if (sizeMatch.Groups[2].Value.ToLower() == "px")
                                        size = size * 0.75f;
                                    span.FontSizePt = (int)Math.Round(size);
                                }
                            }
                        }

                        spans.Add(span);
                    }
                }
            }

            return spans;
        }

        /// <summary>
        /// Parsa una tabella HTML ed estrae righe e celle
        /// </summary>
        private ContentPart ParseHtmlTable(string tableHtml)
        {
            var part = new ContentPart { IsTable = true };

            // Estrai righe (tr)
            var rowPattern = @"<tr[^>]*>(.*?)</tr>";
            var rowMatches = Regex.Matches(tableHtml, rowPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

            bool isFirstRow = true;
            foreach (Match rowMatch in rowMatches)
            {
                var rowContent = rowMatch.Groups[1].Value;
                var cells = new List<string>();

                // Cerca header (th) o celle (td)
                var cellPattern = @"<t[hd][^>]*>(.*?)</t[hd]>";
                var cellMatches = Regex.Matches(rowContent, cellPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

                foreach (Match cellMatch in cellMatches)
                {
                    var cellText = RimuoviTagHtml(cellMatch.Groups[1].Value).Trim();
                    cells.Add(cellText);
                }

                if (cells.Count > 0)
                {
                    // La prima riga con th è l'header
                    if (isFirstRow && rowContent.Contains("<th", StringComparison.OrdinalIgnoreCase))
                    {
                        part.TableHeaders = cells;
                    }
                    else
                    {
                        part.TableRows.Add(cells);
                    }
                    isFirstRow = false;
                }
            }

            return part;
        }

        /// <summary>
        /// Renderizza un paragrafo formattato in PDF
        /// </summary>
        private void RenderFormattedParagraphToPdf(QuestPDF.Fluent.ColumnDescriptor col, FormattedParagraph paragraph)
        {
            // Spaziatura prima del paragrafo
            if (paragraph.SpacingBeforePt.HasValue && paragraph.SpacingBeforePt > 0)
            {
                col.Item().Height(paragraph.SpacingBeforePt.Value);
            }

            col.Item().Text(text =>
            {
                // Imposta allineamento
                switch (paragraph.Alignment)
                {
                    case "left":
                        text.AlignLeft();
                        break;
                    case "center":
                        text.AlignCenter();
                        break;
                    case "right":
                        text.AlignRight();
                        break;
                    default:
                        text.Justify();
                        break;
                }

                // Applica line-height (interlinea)
                text.DefaultTextStyle(x => x.LineHeight(paragraph.LineHeight).FontFamily("Arial"));

                // Determina dimensione base
                int baseFontSize = paragraph.FontSizePt ?? 11; // Usa font-size del paragrafo o default 11
                bool isHeadingBold = false;
                if (paragraph.IsHeading)
                {
                    isHeadingBold = true;
                    // Per heading, sovrascrive solo se non c'è un font-size esplicito
                    if (!paragraph.FontSizePt.HasValue)
                    {
                        baseFontSize = paragraph.HeadingLevel switch
                        {
                            1 => 18,
                            2 => 16,
                            3 => 14,
                            4 => 13,
                            5 => 12,
                            6 => 11,
                            _ => 11
                        };
                    }
                }

                // Aggiungi bullet point se è un elemento di lista
                if (paragraph.IsBulletPoint)
                {
                    text.Span("• ").FontSize(baseFontSize);
                }

                // Renderizza ogni span con la sua formattazione
                foreach (var span in paragraph.Spans)
                {
                    var fontSize = span.FontSizePt ?? baseFontSize;
                    var isBold = span.IsBold || isHeadingBold;
                    var isItalic = span.IsItalic;
                    var isUnderline = span.IsUnderline;

                    // QuestPDF: applica stili in modo condizionale
                    var textSpan = text.Span(span.Text).FontSize(fontSize);
                    
                    if (isBold)
                        textSpan = textSpan.Bold();
                    if (isItalic)
                        textSpan = textSpan.Italic();
                    if (isUnderline)
                        textSpan = textSpan.Underline();
                }
            });

            // Spaziatura dopo il paragrafo
            var spacingAfter = paragraph.SpacingAfterPt ?? (paragraph.IsHeading ? 10 : 6);
            col.Item().Height(spacingAfter);
        }

        /// <summary>
        /// Renderizza una tabella in PDF usando QuestPDF
        /// </summary>
        private void RenderTableToPdf(QuestPDF.Fluent.ColumnDescriptor col, List<List<string>> rows, List<string> headers)
        {
            // Determina il numero di colonne dalla riga con più celle
            int columnCount = headers.Count;
            foreach (var row in rows)
            {
                if (row.Count > columnCount) columnCount = row.Count;
            }
            if (columnCount == 0) return;

            col.Item().Table(table =>
            {
                // Definisci le colonne con proporzioni intelligenti per formato A4
                table.ColumnsDefinition(columns =>
                {
                    if (columnCount == 3)
                    {
                        // Tabella tipo: Controllo | Sì | No
                        columns.RelativeColumn(8); // Controllo (80%)
                        columns.RelativeColumn(1); // Sì (10%)
                        columns.RelativeColumn(1); // No (10%)
                    }
                    else if (columnCount == 4)
                    {
                        // Tabella tipo: (X) | Documentazione | Osservazioni | Annotazioni
                        columns.RelativeColumn(0.5f); // 5% - spunta
                        columns.RelativeColumn(3);    // 30% - documentazione
                        columns.RelativeColumn(4.5f); // 45% - osservazioni  
                        columns.RelativeColumn(2);    // 20% - annotazioni
                    }
                    else if (columnCount == 2)
                    {
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                    }
                    else
                    {
                        // Distribuzione equa
                        for (int i = 0; i < columnCount; i++)
                        {
                            columns.RelativeColumn(1);
                        }
                    }
                });

                // Header
                if (headers.Count > 0)
                {
                    for (int i = 0; i < columnCount; i++)
                    {
                        var headerText = i < headers.Count ? headers[i] : "";
                        var headerCell = table.Cell()
                            .Border(1)
                            .BorderColor(Colors.Black)
                            .Background(Colors.Grey.Lighten3)
                            .Padding(3);
                        
                        // Allineamento header in base al tipo di tabella
                        if (columnCount == 4)
                        {
                            // Prima colonna (spunta) centrata, resto a sinistra
                            if (i == 0)
                            {
                                headerCell.AlignCenter()
                                    .AlignMiddle()
                                    .Text(headerText)
                                    .Bold()
                                    .FontSize(9);
                            }
                            else
                            {
                                headerCell.AlignLeft()
                                    .AlignMiddle()
                                    .Text(headerText)
                                    .Bold()
                                    .FontSize(9);
                            }
                        }
                        else
                        {
                            headerCell.AlignCenter()
                                .AlignMiddle()
                                .Text(headerText)
                                .Bold()
                                .FontSize(9);
                        }
                    }
                }

                // Righe dati
                foreach (var row in rows)
                {
                    for (int i = 0; i < columnCount; i++)
                    {
                        var cellText = i < row.Count ? row[i] : "";
                        var cell = table.Cell()
                            .Border(1)
                            .BorderColor(Colors.Black)
                            .Padding(3); // Padding ridotto per risparmiare spazio
                        
                        // Allineamento in base al tipo di tabella
                        if (columnCount == 4)
                        {
                            // Tabella 4 colonne: prima centrata (spunta), resto a sinistra
                            if (i == 0)
                            {
                                cell.AlignCenter()
                                    .AlignMiddle()
                                    .Text(cellText)
                                    .FontSize(9);
                            }
                            else
                            {
                                cell.AlignLeft()
                                    .Text(cellText)
                                    .FontSize(9);
                            }
                        }
                        else
                        {
                            // Tabella 3 colonne: prima a sinistra, resto centrato
                            if (i == 0)
                            {
                                cell.AlignLeft()
                                    .Text(cellText)
                                    .FontSize(9);
                            }
                            else
                            {
                                cell.AlignCenter()
                                    .AlignMiddle()
                                    .Text(cellText)
                                    .FontSize(9);
                            }
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Rimuove i tag HTML e restituisce testo pulito
        /// </summary>
        private string RimuoviTagHtml(string html)
        {
            if (string.IsNullOrEmpty(html)) return "";

            // Sostituisci <br> e </p> con newline
            var result = Regex.Replace(html, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"</p>", "\n\n", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"</div>", "\n", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"</li>", "\n", RegexOptions.IgnoreCase);
            
            // Rimuovi tutti i tag HTML
            result = Regex.Replace(result, @"<[^>]+>", "");
            
            // Decodifica entità HTML comuni
            result = result.Replace("&nbsp;", " ");
            result = result.Replace("&amp;", "&");
            result = result.Replace("&lt;", "<");
            result = result.Replace("&gt;", ">");
            result = result.Replace("&quot;", "\"");
            result = result.Replace("&#39;", "'");
            result = result.Replace("&euro;", "€");
            
            // Rimuovi spazi multipli
            result = Regex.Replace(result, @"[ \t]+", " ");
            
            // Rimuovi righe vuote multiple
            result = Regex.Replace(result, @"\n{3,}", "\n\n");
            
            return result.Trim();
        }

        /// <summary>
        /// Restituisce il label del tipo soggetto
        /// </summary>
        private string GetTipoSoggettoLabel(TipoSoggetto tipo)
        {
            return tipo switch
            {
                TipoSoggetto.LegaleRappresentante => "Legale Rappresentante",
                TipoSoggetto.Consigliere => "Consigliere",
                TipoSoggetto.Socio => "Socio",
                _ => tipo.ToString()
            };
        }

        /// <summary>
        /// Converte un numero in lettere (italiano)
        /// </summary>
        private string NumeroInLettere(decimal numero)
        {
            // Implementazione semplificata
            var intera = (long)Math.Floor(numero);
            var decimale = (int)((numero - intera) * 100);

            var parteIntera = NumeroInteroInLettere(intera);
            
            if (decimale > 0)
            {
                return $"{parteIntera} euro e {decimale:00}/100";
            }
            return $"{parteIntera} euro";
        }

        private string NumeroInteroInLettere(long n)
        {
            if (n == 0) return "zero";
            if (n < 0) return "meno " + NumeroInteroInLettere(-n);

            string[] unita = { "", "uno", "due", "tre", "quattro", "cinque", "sei", "sette", "otto", "nove", "dieci",
                              "undici", "dodici", "tredici", "quattordici", "quindici", "sedici", "diciassette", "diciotto", "diciannove" };
            string[] decine = { "", "", "venti", "trenta", "quaranta", "cinquanta", "sessanta", "settanta", "ottanta", "novanta" };

            if (n < 20) return unita[n];
            if (n < 100)
            {
                var d = decine[n / 10];
                var u = n % 10;
                if (u == 1 || u == 8) d = d.Substring(0, d.Length - 1); // vent'uno, ventotto
                return d + unita[u];
            }
            if (n < 1000)
            {
                var c = n / 100;
                var resto = n % 100;
                var cento = c == 1 ? "cento" : unita[c] + "cento";
                return cento + (resto > 0 ? NumeroInteroInLettere(resto) : "");
            }
            if (n < 1000000)
            {
                var m = n / 1000;
                var resto = n % 1000;
                var mille = m == 1 ? "mille" : NumeroInteroInLettere(m) + "mila";
                return mille + (resto > 0 ? NumeroInteroInLettere(resto) : "");
            }
            if (n < 1000000000)
            {
                var m = n / 1000000;
                var resto = n % 1000000;
                var milione = m == 1 ? "un milione" : NumeroInteroInLettere(m) + " milioni";
                return milione + (resto > 0 ? " " + NumeroInteroInLettere(resto) : "");
            }

            return n.ToString("N0", _italianCulture);
        }

        /// <summary>
        /// Rimuove caratteri non validi dal nome file
        /// </summary>
        private string SanitizeFileName(string fileName)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return string.Join("_", fileName.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
        }

        /// <summary>
        /// Legge le dimensioni di un'immagine direttamente dai byte (supporta PNG e JPEG)
        /// </summary>
        private (int width, int height) GetImageDimensions(byte[] imageBytes, string contentType)
        {
            try
            {
                if (contentType.Contains("png", StringComparison.OrdinalIgnoreCase))
                {
                    // PNG: le dimensioni sono ai byte 16-23 (dopo l'header IHDR)
                    // Formato: 8 byte signature + 4 byte length + 4 byte "IHDR" + 4 byte width + 4 byte height
                    if (imageBytes.Length > 24)
                    {
                        int width = (imageBytes[16] << 24) | (imageBytes[17] << 16) | (imageBytes[18] << 8) | imageBytes[19];
                        int height = (imageBytes[20] << 24) | (imageBytes[21] << 16) | (imageBytes[22] << 8) | imageBytes[23];
                        return (width, height);
                    }
                }
                else if (contentType.Contains("jpeg", StringComparison.OrdinalIgnoreCase) || 
                         contentType.Contains("jpg", StringComparison.OrdinalIgnoreCase))
                {
                    // JPEG: cerca il marker SOF0 (0xFFC0) o SOF2 (0xFFC2)
                    for (int i = 0; i < imageBytes.Length - 9; i++)
                    {
                        if (imageBytes[i] == 0xFF)
                        {
                            byte marker = imageBytes[i + 1];
                            // SOF0, SOF1, SOF2 markers
                            if (marker == 0xC0 || marker == 0xC1 || marker == 0xC2)
                            {
                                int height = (imageBytes[i + 5] << 8) | imageBytes[i + 6];
                                int width = (imageBytes[i + 7] << 8) | imageBytes[i + 8];
                                return (width, height);
                            }
                        }
                    }
                }
                else if (contentType.Contains("gif", StringComparison.OrdinalIgnoreCase))
                {
                    // GIF: dimensioni ai byte 6-9 (little endian)
                    if (imageBytes.Length > 10)
                    {
                        int width = imageBytes[6] | (imageBytes[7] << 8);
                        int height = imageBytes[8] | (imageBytes[9] << 8);
                        return (width, height);
                    }
                }
                else if (contentType.Contains("bmp", StringComparison.OrdinalIgnoreCase))
                {
                    // BMP: dimensioni ai byte 18-25 (little endian, signed)
                    if (imageBytes.Length > 26)
                    {
                        int width = imageBytes[18] | (imageBytes[19] << 8) | (imageBytes[20] << 16) | (imageBytes[21] << 24);
                        int height = imageBytes[22] | (imageBytes[23] << 8) | (imageBytes[24] << 16) | (imageBytes[25] << 24);
                        return (width, Math.Abs(height)); // height può essere negativo
                    }
                }
            }
            catch
            {
                // In caso di errore, ritorna 0,0 per usare dimensioni di default
            }
            
            return (0, 0);
        }

        /// <summary>
        /// Crea un paragrafo Word con formattazione
        /// </summary>
        private Paragraph CreateFormattedWordParagraph(FormattedParagraph formattedPara)
        {
            var paragraph = new Paragraph();
            
            // Imposta proprietà paragrafo (allineamento)
            var paraProperties = new ParagraphProperties();
            var justification = formattedPara.Alignment switch
            {
                "left" => JustificationValues.Left,
                "center" => JustificationValues.Center,
                "right" => JustificationValues.Right,
                _ => JustificationValues.Both
            };
            paraProperties.AppendChild(new Justification { Val = justification });
            
            // Spaziatura paragrafo (before/after in twips, 1pt = 20 twips)
            var spacingBefore = formattedPara.SpacingBeforePt.HasValue 
                ? (formattedPara.SpacingBeforePt.Value * 20).ToString() 
                : "0";
            var spacingAfter = formattedPara.SpacingAfterPt.HasValue 
                ? (formattedPara.SpacingAfterPt.Value * 20).ToString() 
                : "120";
            
            // Line-height: in Word usa il valore moltiplicato per 240 (240 = single line)
            // Es: 1.5 = 360, 2.0 = 480
            var lineSpacing = (int)(formattedPara.LineHeight * 240);
            
            paraProperties.AppendChild(new SpacingBetweenLines 
            { 
                After = spacingAfter, 
                Before = spacingBefore,
                Line = lineSpacing.ToString(),
                LineRule = LineSpacingRuleValues.Auto
            });
            
            paragraph.AppendChild(paraProperties);

            // Determina dimensione base
            int baseFontSizePt = formattedPara.FontSizePt ?? 11; // Usa font-size del paragrafo o default 11
            if (formattedPara.IsHeading && !formattedPara.FontSizePt.HasValue)
            {
                // Per heading, sovrascrive solo se non c'è un font-size esplicito
                baseFontSizePt = formattedPara.HeadingLevel switch
                {
                    1 => 18,
                    2 => 16,
                    3 => 14,
                    4 => 13,
                    5 => 12,
                    6 => 11,
                    _ => 11
                };
            }
            int baseFontSizeHalfPt = baseFontSizePt * 2; // Converti in half-points

            // Aggiungi bullet point se è un elemento di lista
            if (formattedPara.IsBulletPoint)
            {
                var bulletRun = new Run();
                var bulletProps = new RunProperties();
                bulletProps.AppendChild(new RunFonts { Ascii = "Arial", HighAnsi = "Arial" });
                bulletProps.AppendChild(new FontSize { Val = baseFontSizeHalfPt.ToString() });
                bulletRun.AppendChild(bulletProps);
                bulletRun.AppendChild(new Text("• ") { Space = SpaceProcessingModeValues.Preserve });
                paragraph.AppendChild(bulletRun);
            }

            // Aggiungi ogni span formattato
            foreach (var span in formattedPara.Spans)
            {
                var run = new Run();
                var runProperties = new RunProperties();
                
                // Font
                runProperties.AppendChild(new RunFonts { Ascii = "Arial", HighAnsi = "Arial" });
                
                // Dimensione font
                var fontSize = span.FontSizePt.HasValue 
                    ? span.FontSizePt.Value * 2  // pt to half-points
                    : baseFontSizeHalfPt;
                runProperties.AppendChild(new FontSize { Val = fontSize.ToString() });
                
                // Grassetto
                if (span.IsBold || formattedPara.IsHeading)
                {
                    runProperties.AppendChild(new Bold());
                }
                
                // Corsivo
                if (span.IsItalic)
                {
                    runProperties.AppendChild(new Italic());
                }
                
                // Sottolineato
                if (span.IsUnderline)
                {
                    runProperties.AppendChild(new Underline { Val = UnderlineValues.Single });
                }
                
                run.AppendChild(runProperties);
                run.AppendChild(new Text(span.Text) { Space = SpaceProcessingModeValues.Preserve });
                
                paragraph.AppendChild(run);
            }

            return paragraph;
        }

        /// <summary>
        /// Crea un paragrafo con immagine per Word
        /// </summary>
        private Paragraph CreateImageParagraph(MainDocumentPart mainPart, byte[] imageBytes, string contentType, long widthEmu, long heightEmu)
        {
            // Determina il content type per PartTypeInfo
            string partContentType = contentType.ToLower() switch
            {
                "image/png" => "image/png",
                "image/gif" => "image/gif",
                "image/bmp" => "image/bmp",
                _ => "image/jpeg"
            };

            // Aggiungi l'immagine come parte del documento
            var imagePart = mainPart.AddImagePart(partContentType);
            using (var imageStream = new MemoryStream(imageBytes))
            {
                imagePart.FeedData(imageStream);
            }

            var relationshipId = mainPart.GetIdOfPart(imagePart);

            // Leggi le dimensioni reali dell'immagine per mantenere le proporzioni
            var (imgWidth, imgHeight) = GetImageDimensions(imageBytes, contentType);
            
            // 1 cm = 360000 EMU (English Metric Units)
            // Dimensione massima: larghezza 5cm, altezza 2cm
            long maxWidthEmu = 1800000;  // 5cm
            long maxHeightEmu = 720000;  // 2cm
            
            if (imgWidth > 0 && imgHeight > 0)
            {
                double aspectRatio = (double)imgWidth / imgHeight;
                
                // Calcola dimensioni mantenendo proporzioni
                widthEmu = maxWidthEmu;
                heightEmu = (long)(maxWidthEmu / aspectRatio);
                
                // Se l'altezza supera il massimo, ricalcola dalla altezza
                if (heightEmu > maxHeightEmu)
                {
                    heightEmu = maxHeightEmu;
                    widthEmu = (long)(maxHeightEmu * aspectRatio);
                }
            }
            else
            {
                // Fallback a dimensioni di default se non riesce a leggere
                widthEmu = 1440000;  // ~4cm
                heightEmu = 540000;  // ~1.5cm
            }

            // Crea l'elemento Drawing per l'immagine
            var element = new Drawing(
                new DW.Inline(
                    new DW.Extent { Cx = widthEmu, Cy = heightEmu },
                    new DW.EffectExtent
                    {
                        LeftEdge = 0L,
                        TopEdge = 0L,
                        RightEdge = 0L,
                        BottomEdge = 0L
                    },
                    new DW.DocProperties
                    {
                        Id = 1U,
                        Name = "Logo"
                    },
                    new DW.NonVisualGraphicFrameDrawingProperties(
                        new A.GraphicFrameLocks { NoChangeAspect = true }),
                    new A.Graphic(
                        new A.GraphicData(
                            new PIC.Picture(
                                new PIC.NonVisualPictureProperties(
                                    new PIC.NonVisualDrawingProperties
                                    {
                                        Id = 0U,
                                        Name = "Logo.png"
                                    },
                                    new PIC.NonVisualPictureDrawingProperties()),
                                new PIC.BlipFill(
                                    new A.Blip
                                    {
                                        Embed = relationshipId,
                                        CompressionState = A.BlipCompressionValues.Print
                                    },
                                    new A.Stretch(new A.FillRectangle())),
                                new PIC.ShapeProperties(
                                    new A.Transform2D(
                                        new A.Offset { X = 0L, Y = 0L },
                                        new A.Extents { Cx = widthEmu, Cy = heightEmu }),
                                    new A.PresetGeometry(new A.AdjustValueList())
                                    { Preset = A.ShapeTypeValues.Rectangle })))
                        { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" })
                )
                {
                    DistanceFromTop = 0U,
                    DistanceFromBottom = 0U,
                    DistanceFromLeft = 0U,
                    DistanceFromRight = 0U
                });

            // Crea paragrafo centrato con l'immagine
            var paragraph = new Paragraph();
            var paragraphProperties = new ParagraphProperties();
            paragraphProperties.AppendChild(new Justification { Val = JustificationValues.Center });
            paragraph.AppendChild(paragraphProperties);
            
            var run = new Run();
            run.AppendChild(element);
            paragraph.AppendChild(run);

            return paragraph;
        }

        /// <summary>
        /// Anteprima del documento (senza salvare) - formato A4 con intestazione, contenuto e piè di pagina
        /// </summary>
        public async Task<string> GeneraAnteprimaAsync(int templateId, int clienteId, int? mandatoId)
        {
            var template = await _context.TemplateDocumenti.FindAsync(templateId);
            if (template == null) return "<p>Template non trovato</p>";

            var cliente = await _context.Clienti
                .Include(c => c.Soggetti)
                .FirstOrDefaultAsync(c => c.Id == clienteId);
            if (cliente == null) return "<p>Cliente non trovato</p>";

            // Carica mandato se richiesto, altrimenti prende l'ultimo mandato attivo del cliente
            MandatoCliente? mandato = null;
            if (mandatoId.HasValue)
            {
                mandato = await _context.MandatiClienti.FindAsync(mandatoId.Value);
            }
            else
            {
                // Se non è specificato un mandato, prende l'ultimo mandato attivo del cliente
                mandato = await _context.MandatiClienti
                    .Where(m => m.ClienteId == clienteId && m.IsActive)
                    .OrderByDescending(m => m.Anno)
                    .FirstOrDefaultAsync();
            }

            var studio = await _context.ConfigurazioniStudio.FirstOrDefaultAsync();

            // Compila tutte le sezioni
            var intestazione = !string.IsNullOrEmpty(template.Intestazione) 
                ? SostituisciCampi(template.Intestazione, studio, cliente, mandato) 
                : "";
            var contenuto = SostituisciCampi(template.Contenuto, studio, cliente, mandato);
            var piePagina = !string.IsNullOrEmpty(template.PiePagina) 
                ? SostituisciCampi(template.PiePagina, studio, cliente, mandato) 
                : "";

            // Costruisci HTML con formato A4 giustificato
            var html = new StringBuilder();
            html.AppendLine(@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        * { box-sizing: border-box; margin: 0; padding: 0; }
        body { 
            font-family: Arial, sans-serif; 
            font-size: 11pt; 
            line-height: 1.5;
            background: #e0e0e0;
            padding: 20px;
        }
        .page {
            width: 210mm;
            min-height: 297mm;
            margin: 0 auto 20px auto;
            padding: 20mm;
            background: white;
            box-shadow: 0 4px 12px rgba(0,0,0,0.15);
            position: relative;
        }
        .header {
            text-align: center;
            padding-bottom: 10px;
            border-bottom: 1px solid #333;
            margin-bottom: 15px;
        }
        .header img { max-height: 60px; margin-bottom: 10px; }
        .content {
            text-align: justify;
            min-height: 200mm;
        }
        .content p { margin-bottom: 10px; text-align: justify; }
        .footer {
            text-align: center;
            padding-top: 10px;
            border-top: 1px solid #333;
            margin-top: 15px;
            font-size: 9pt;
            color: #666;
        }
        @media print {
            body { background: white; padding: 0; }
            .page { 
                box-shadow: none; 
                margin: 0; 
                page-break-after: always;
            }
        }
    </style>
</head>
<body>");

            html.AppendLine("<div class='page'>");

            // Header
            if (!string.IsNullOrEmpty(intestazione))
            {
                html.AppendLine($"<div class='header'>{intestazione}</div>");
            }
            else if (studio != null)
            {
                html.AppendLine("<div class='header'>");
                if (studio.Logo != null)
                {
                    var logoBase64 = Convert.ToBase64String(studio.Logo);
                    html.AppendLine($"<img src='data:{studio.LogoContentType};base64,{logoBase64}' /><br/>");
                }
                if (!string.IsNullOrEmpty(studio.NomeStudio))
                {
                    html.AppendLine($"<strong style='font-size:14pt;'>{studio.NomeStudio}</strong><br/>");
                }
                if (!string.IsNullOrEmpty(studio.Indirizzo))
                {
                    var indirizzo = studio.Indirizzo;
                    if (!string.IsNullOrEmpty(studio.CAP)) indirizzo += $" - {studio.CAP}";
                    if (!string.IsNullOrEmpty(studio.Citta)) indirizzo += $" {studio.Citta}";
                    html.AppendLine($"<span style='font-size:9pt;'>{indirizzo}</span>");
                }
                html.AppendLine("</div>");
            }

            // Content
            html.AppendLine($"<div class='content'>{contenuto}</div>");

            // Footer
            if (!string.IsNullOrEmpty(piePagina))
            {
                // Sostituisci tag pagina per anteprima
                var footerHtml = piePagina
                    .Replace("{{Pagina}}", "1")
                    .Replace("{{TotalePagine}}", "1")
                    .Replace("{{PaginaDi}}", "Pagina 1 di 1");
                html.AppendLine($"<div class='footer'>{footerHtml}</div>");
            }

            html.AppendLine("</div>");
            html.AppendLine("</body></html>");

            return html.ToString();
        }
    }
}

