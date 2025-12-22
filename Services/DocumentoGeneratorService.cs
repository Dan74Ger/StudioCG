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

            // Carica mandato se richiesto
            MandatoCliente? mandato = null;
            if (mandatoId.HasValue)
            {
                mandato = await _context.MandatiClienti.FindAsync(mandatoId.Value);
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
                fileBytes = GeneraWord(intestazioneCompilata, contenutoCompilato, piePaginaCompilato);
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
                result = result.Replace("{{LegaleRapp.IndirizzoCompleto}}", "");
                result = result.Replace("{{Soggetti.LegaleRapp}}", "");
            }

            // ---- CONSIGLIERI ----
            var consiglieri = soggetti.Where(s => s.TipoSoggetto == TipoSoggetto.Consigliere).ToList();
            if (consiglieri.Any())
            {
                var elencoConsiglieri = string.Join("<br/>", consiglieri.Select(c => $"{c.Cognome} {c.Nome}"));
                result = result.Replace("{{Consiglieri.Elenco}}", elencoConsiglieri);
                
                var elencoConsiglieriCompleto = string.Join("<br/>", consiglieri.Select(c => 
                    $"{c.Cognome} {c.Nome} - CF: {c.CodiceFiscale ?? "N/A"}" +
                    (!string.IsNullOrEmpty(c.Indirizzo) ? $" - {c.Indirizzo}" : "") +
                    (!string.IsNullOrEmpty(c.Citta) ? $", {c.Citta}" : "")));
                result = result.Replace("{{Consiglieri.ElencoCompleto}}", elencoConsiglieriCompleto);
            }
            else
            {
                result = result.Replace("{{Consiglieri.Elenco}}", "");
                result = result.Replace("{{Consiglieri.ElencoCompleto}}", "");
            }

            // ---- SOCI ----
            var soci = soggetti.Where(s => s.TipoSoggetto == TipoSoggetto.Socio).ToList();
            if (soci.Any())
            {
                var elencoSoci = string.Join("<br/>", soci.Select(s => $"{s.Cognome} {s.Nome}"));
                result = result.Replace("{{Soci.Elenco}}", elencoSoci);
                
                var elencoSociConQuote = string.Join("<br/>", soci.Select(s => 
                    $"{s.Cognome} {s.Nome}" + 
                    (s.QuotaPercentuale.HasValue ? $" - Quota: {s.QuotaPercentuale.Value.ToString("N2", _italianCulture)}%" : "")));
                result = result.Replace("{{Soci.ElencoConQuote}}", elencoSociConQuote);
                
                var elencoSociCompleto = string.Join("<br/>", soci.Select(s => 
                    $"{s.Cognome} {s.Nome} - CF: {s.CodiceFiscale ?? "N/A"}" +
                    (s.QuotaPercentuale.HasValue ? $" - Quota: {s.QuotaPercentuale.Value.ToString("N2", _italianCulture)}%" : "") +
                    (!string.IsNullOrEmpty(s.Indirizzo) ? $" - {s.Indirizzo}" : "") +
                    (!string.IsNullOrEmpty(s.Citta) ? $", {s.Citta}" : "")));
                result = result.Replace("{{Soci.ElencoCompleto}}", elencoSociCompleto);
                
                var totaleQuote = soci.Where(s => s.QuotaPercentuale.HasValue).Sum(s => s.QuotaPercentuale ?? 0);
                result = result.Replace("{{Soci.TotaleQuote}}", $"{totaleQuote.ToString("N2", _italianCulture)}%");
            }
            else
            {
                result = result.Replace("{{Soci.Elenco}}", "");
                result = result.Replace("{{Soci.ElencoConQuote}}", "");
                result = result.Replace("{{Soci.ElencoCompleto}}", "");
                result = result.Replace("{{Soci.TotaleQuote}}", "");
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

            // Rimuovi tag HTML per ottenere testo pulito per QuestPDF
            var testoPlain = RimuoviTagHtml(htmlContent);
            var paragrafi = testoPlain.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            
            var intestazionePlain = !string.IsNullOrEmpty(intestazione) ? RimuoviTagHtml(intestazione) : null;
            var footerPlain = !string.IsNullOrEmpty(piePagina) ? RimuoviTagHtml(piePagina) : null;

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
                                col.Item().Height(50).AlignCenter().Image(studio.Logo);
                                col.Item().Height(5);
                            }
                            foreach (var line in intestazionePlain.Split('\n'))
                            {
                                if (!string.IsNullOrWhiteSpace(line))
                                    col.Item().AlignCenter().Text(line.Trim()).FontSize(10);
                            }
                        }
                        else
                        {
                            // Intestazione default da ConfigurazioneStudio
                            if (studio?.Logo != null)
                            {
                                col.Item().Height(60).AlignCenter().Image(studio.Logo);
                                col.Item().Height(10);
                            }
                            
                            if (!string.IsNullOrEmpty(studio?.NomeStudio))
                            {
                                col.Item().AlignCenter().Text(studio.NomeStudio).Bold().FontSize(14);
                            }
                            
                            if (!string.IsNullOrEmpty(studio?.Indirizzo))
                            {
                                var indirizzo = $"{studio.Indirizzo}";
                                if (!string.IsNullOrEmpty(studio.CAP)) indirizzo += $" - {studio.CAP}";
                                if (!string.IsNullOrEmpty(studio.Citta)) indirizzo += $" {studio.Citta}";
                                col.Item().AlignCenter().Text(indirizzo).FontSize(9);
                            }
                        }
                        
                        col.Item().Height(5);
                        col.Item().LineHorizontal(0.5f);
                        col.Item().Height(10);
                    });

                    // Contenuto
                    page.Content().Column(col =>
                    {
                        foreach (var para in paragrafi)
                        {
                            var testo = para.Trim();
                            if (string.IsNullOrWhiteSpace(testo)) continue;
                            
                            col.Item().Text(testo).LineHeight(1.5f);
                            col.Item().Height(8);
                        }
                    });

                    // Footer - usa piè di pagina personalizzato se presente
                    page.Footer().Column(col =>
                    {
                        if (!string.IsNullOrEmpty(footerPlain))
                        {
                            col.Item().LineHorizontal(0.5f);
                            col.Item().Height(5);
                            foreach (var line in footerPlain.Split('\n'))
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
        private byte[] GeneraWord(string? intestazione, string htmlContent, string? piePagina)
        {
            // Rimuovi tag HTML per ottenere testo pulito
            var testoPlain = RimuoviTagHtml(htmlContent);
            var paragrafi = testoPlain.Split(new[] { "\n\n", "\r\n\r\n", "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            
            var intestazionePlain = !string.IsNullOrEmpty(intestazione) ? RimuoviTagHtml(intestazione) : null;
            var footerPlain = !string.IsNullOrEmpty(piePagina) ? RimuoviTagHtml(piePagina) : null;

            using var stream = new MemoryStream();
            
            using (var wordDocument = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
            {
                // Aggiungi parti principali
                var mainPart = wordDocument.AddMainDocumentPart();
                mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
                var body = mainPart.Document.AppendChild(new Body());

                // Aggiungi intestazione se presente
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
                            // Centra il paragrafo
                            var paraProps = new ParagraphProperties();
                            paraProps.AppendChild(new Justification { Val = JustificationValues.Center });
                            headerPara.PrependChild(paraProps);
                            body.AppendChild(headerPara);
                        }
                    }
                    // Linea separatore
                    body.AppendChild(new Paragraph());
                }

                // Aggiungi ogni paragrafo del contenuto
                foreach (var para in paragrafi)
                {
                    var testo = para.Trim();
                    if (string.IsNullOrWhiteSpace(testo)) continue;

                    var paragraph = new Paragraph();
                    var run = new Run();
                    
                    // Imposta font
                    var runProperties = new RunProperties();
                    runProperties.AppendChild(new RunFonts { Ascii = "Arial", HighAnsi = "Arial" });
                    runProperties.AppendChild(new FontSize { Val = "22" }); // 11pt = 22 half-points
                    run.AppendChild(runProperties);
                    
                    run.AppendChild(new Text(testo) { Space = SpaceProcessingModeValues.Preserve });
                    paragraph.AppendChild(run);
                    
                    // Giustifica il testo
                    var paraProperties = new ParagraphProperties();
                    paraProperties.AppendChild(new Justification { Val = JustificationValues.Both });
                    paragraph.PrependChild(paraProperties);
                    
                    body.AppendChild(paragraph);
                    
                    // Aggiungi spazio tra paragrafi
                    body.AppendChild(new Paragraph());
                }

                // Aggiungi piè di pagina se presente
                if (!string.IsNullOrEmpty(footerPlain))
                {
                    body.AppendChild(new Paragraph()); // Spazio
                    foreach (var line in footerPlain.Split('\n'))
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            var footerPara = new Paragraph();
                            var footerRun = new Run();
                            var footerProps = new RunProperties();
                            footerProps.AppendChild(new RunFonts { Ascii = "Arial", HighAnsi = "Arial" });
                            footerProps.AppendChild(new FontSize { Val = "18" }); // 9pt
                            footerRun.AppendChild(footerProps);
                            footerRun.AppendChild(new Text(line.Trim()) { Space = SpaceProcessingModeValues.Preserve });
                            footerPara.AppendChild(footerRun);
                            // Centra il paragrafo
                            var paraProps = new ParagraphProperties();
                            paraProps.AppendChild(new Justification { Val = JustificationValues.Center });
                            footerPara.PrependChild(paraProps);
                            body.AppendChild(footerPara);
                        }
                    }
                }

                // Imposta margini pagina
                var sectionProperties = new SectionProperties();
                var pageMargin = new PageMargin
                {
                    Top = 1440,    // 1 inch = 1440 twips
                    Right = 1440,
                    Bottom = 1440,
                    Left = 1440
                };
                sectionProperties.AppendChild(pageMargin);
                body.AppendChild(sectionProperties);

                mainPart.Document.Save();
            }

            return stream.ToArray();
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
        /// Anteprima del documento (senza salvare)
        /// </summary>
        public async Task<string> GeneraAnteprimaAsync(int templateId, int clienteId, int? mandatoId)
        {
            var template = await _context.TemplateDocumenti.FindAsync(templateId);
            if (template == null) return "<p>Template non trovato</p>";

            var cliente = await _context.Clienti
                .Include(c => c.Soggetti)
                .FirstOrDefaultAsync(c => c.Id == clienteId);
            if (cliente == null) return "<p>Cliente non trovato</p>";

            MandatoCliente? mandato = null;
            if (mandatoId.HasValue)
            {
                mandato = await _context.MandatiClienti.FindAsync(mandatoId.Value);
            }

            var studio = await _context.ConfigurazioniStudio.FirstOrDefaultAsync();

            return SostituisciCampi(template.Contenuto, studio, cliente, mandato);
        }
    }
}

