using System.Data;
using System.Text.RegularExpressions;
using StudioCG.Web.Models;
using StudioCG.Web.Models.Entita;

namespace StudioCG.Web.Services
{
    /// <summary>
    /// Servizio per calcolare formule dei campi calcolati
    /// </summary>
    public class FormulaCalculatorService
    {
        /// <summary>
        /// Calcola il valore di un campo calcolato basandosi sui valori degli altri campi
        /// </summary>
        /// <param name="formula">La formula da calcolare (es: "[LIQ4TRIM] * 0.88")</param>
        /// <param name="campi">Lista dei campi dell'attività</param>
        /// <param name="valori">Dizionario NomeCampo -> Valore</param>
        /// <returns>Risultato calcolato formattato con 2 decimali, o null se errore</returns>
        public static string? CalcolaFormula(string? formula, IEnumerable<AttivitaCampo> campi, Dictionary<string, string> valori)
        {
            if (string.IsNullOrWhiteSpace(formula))
                return null;

            try
            {
                // Sostituisci i riferimenti ai campi [NomeCampo] con i loro valori
                var formulaCalcolata = Regex.Replace(formula, @"\[([^\]]+)\]", match =>
                {
                    var nomeCampo = match.Groups[1].Value;
                    
                    // Cerca il campo per Name (case-insensitive)
                    var campo = campi.FirstOrDefault(c => 
                        c.Name.Equals(nomeCampo, StringComparison.OrdinalIgnoreCase));
                    
                    if (campo == null)
                    {
                        // Prova anche a cercare direttamente nel dizionario valori
                        foreach (var kvp in valori)
                        {
                            if (kvp.Key.Equals(nomeCampo, StringComparison.OrdinalIgnoreCase))
                            {
                                var val = kvp.Value.Replace(",", ".");
                                if (decimal.TryParse(val, System.Globalization.NumberStyles.Any, 
                                    System.Globalization.CultureInfo.InvariantCulture, out _))
                                {
                                    return val;
                                }
                            }
                        }
                        return "0";
                    }

                    // Ottieni il valore (case-insensitive lookup)
                    string? valore = null;
                    foreach (var kvp in valori)
                    {
                        if (kvp.Key.Equals(campo.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            valore = kvp.Value;
                            break;
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(valore))
                    {
                        // Converti in formato numerico (gestisce sia virgola che punto)
                        var valoreNormalizzato = valore.Replace(",", ".");
                        if (decimal.TryParse(valoreNormalizzato, System.Globalization.NumberStyles.Any, 
                            System.Globalization.CultureInfo.InvariantCulture, out _))
                        {
                            return valoreNormalizzato;
                        }
                    }
                    
                    return "0";
                });

                // Normalizza le virgole in punti per il calcolo (supporto formato italiano)
                formulaCalcolata = formulaCalcolata.Replace(",", ".");
                
                // Valida che la formula contenga solo caratteri sicuri
                if (!IsFormulaSafe(formulaCalcolata))
                    return null;

                // Calcola l'espressione
                var risultato = EvaluateExpression(formulaCalcolata);
                
                // Formatta con 2 decimali
                return risultato.ToString("N2", System.Globalization.CultureInfo.GetCultureInfo("it-IT"));
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Calcola il valore di un campo calcolato per un ClienteAttivita
        /// </summary>
        public static string? CalcolaPerClienteAttivita(AttivitaCampo campoCalcolato, ClienteAttivita clienteAttivita, IEnumerable<AttivitaCampo> tuttiCampi)
        {
            if (!campoCalcolato.IsCalculated || string.IsNullOrEmpty(campoCalcolato.Formula))
                return null;

            // Costruisci dizionario dei valori (case-insensitive)
            var valori = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var campo in tuttiCampi.Where(c => !c.IsCalculated))
            {
                var valore = clienteAttivita.Valori?.FirstOrDefault(v => v.AttivitaCampoId == campo.Id)?.Valore ?? "";
                valori[campo.Name] = valore;
            }

            return CalcolaFormula(campoCalcolato.Formula, tuttiCampi, valori);
        }

        /// <summary>
        /// Verifica che la formula sia sicura (solo numeri, operatori e parentesi)
        /// </summary>
        private static bool IsFormulaSafe(string formula)
        {
            // Permetti solo: numeri, punto decimale, operatori matematici, parentesi, spazi
            // Le funzioni SE/IF vengono elaborate prima, quindi qui la formula è già numerica
            return Regex.IsMatch(formula, @"^[\d\.\+\-\*\/\(\)\s]+$");
        }

        /// <summary>
        /// Valuta un'espressione matematica semplice
        /// </summary>
        private static decimal EvaluateExpression(string expression)
        {
            // Usa DataTable.Compute per calcolare espressioni matematiche semplici
            var dt = new DataTable();
            var result = dt.Compute(expression, "");
            return Convert.ToDecimal(result);
        }

        /// <summary>
        /// Valida una formula, restituendo eventuali errori
        /// </summary>
        /// <param name="formula">La formula da validare</param>
        /// <param name="campiDisponibili">Lista dei campi disponibili per la referenza</param>
        /// <returns>Null se valida, altrimenti messaggio di errore</returns>
        public static string? ValidaFormula(string? formula, IEnumerable<AttivitaCampo> campiDisponibili)
        {
            if (string.IsNullOrWhiteSpace(formula))
                return "La formula è obbligatoria per i campi calcolati.";

            // Estrai tutti i riferimenti ai campi
            var riferimenti = Regex.Matches(formula, @"\[([^\]]+)\]")
                .Cast<Match>()
                .Select(m => m.Groups[1].Value)
                .ToList();

            if (!riferimenti.Any())
                return "La formula deve contenere almeno un riferimento a un campo (es: [NomeCampo]).";

            // Verifica che tutti i campi referenziati esistano e non siano calcolati
            var campiNonCalcolati = campiDisponibili.Where(c => !c.IsCalculated).ToList();
            foreach (var riferimento in riferimenti)
            {
                var campo = campiDisponibili.FirstOrDefault(c => 
                    c.Name.Equals(riferimento, StringComparison.OrdinalIgnoreCase));
                
                if (campo == null)
                {
                    var nomiDisponibili = string.Join(", ", campiNonCalcolati.Select(c => $"[{c.Name}]"));
                    return $"Il campo [{riferimento}] non esiste. Campi disponibili: {nomiDisponibili}";
                }
                
                if (campo.IsCalculated)
                    return $"Il campo [{riferimento}] è calcolato e non può essere referenziato.";
            }

            // Prova a calcolare con valori di test
            try
            {
                var valoriTest = campiDisponibili
                    .Where(c => !c.IsCalculated)
                    .ToDictionary(c => c.Name, c => "1");
                
                CalcolaFormula(formula, campiDisponibili, valoriTest);
            }
            catch (Exception ex)
            {
                return $"Errore nella formula: {ex.Message}";
            }

            return null; // Valida
        }

        /// <summary>
        /// Estrae i nomi dei campi referenziati in una formula
        /// </summary>
        public static List<string> EstraiRiferimentiCampi(string? formula)
        {
            if (string.IsNullOrWhiteSpace(formula))
                return new List<string>();

            return Regex.Matches(formula, @"\[([^\]]+)\]")
                .Cast<Match>()
                .Select(m => m.Groups[1].Value)
                .Distinct()
                .ToList();
        }

        // ==================== METODI PER ENTITA DINAMICHE ====================

        /// <summary>
        /// Calcola formula generica usando dizionario di valori
        /// Supporta funzioni condizionali: SE(condizione; valore_se_vero; valore_se_falso)
        /// Esempio: SE([LIQUIDAZIONE]<0; [LIQUIDAZIONE]*0.10; 0)
        /// </summary>
        public static string? CalcolaFormulaGenerica(string? formula, Dictionary<string, string> valori)
        {
            if (string.IsNullOrWhiteSpace(formula))
                return null;

            try
            {
                // Prima sostituisci i riferimenti ai campi [NomeCampo] con i loro valori
                var formulaConValori = SostituisciCampiConValori(formula, valori);
                
                // Poi elabora le funzioni SE/IF
                var formulaCalcolata = ElaboraFunzioniCondizionali(formulaConValori);

                // Normalizza le virgole in punti per il calcolo
                formulaCalcolata = formulaCalcolata.Replace(",", ".");
                
                // Valida che la formula contenga solo caratteri sicuri
                if (!IsFormulaSafe(formulaCalcolata))
                    return null;

                // Calcola l'espressione
                var risultato = EvaluateExpression(formulaCalcolata);
                
                // Formatta con 2 decimali
                return risultato.ToString("N2", System.Globalization.CultureInfo.GetCultureInfo("it-IT"));
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Sostituisce i riferimenti ai campi [NomeCampo] con i loro valori numerici
        /// </summary>
        private static string SostituisciCampiConValori(string formula, Dictionary<string, string> valori)
        {
            return Regex.Replace(formula, @"\[([^\]]+)\]", match =>
            {
                var nomeCampo = match.Groups[1].Value;
                
                // Cerca nel dizionario (case-insensitive)
                foreach (var kvp in valori)
                {
                    if (kvp.Key.Equals(nomeCampo, StringComparison.OrdinalIgnoreCase))
                    {
                        var val = kvp.Value?.Replace(",", ".") ?? "0";
                        if (decimal.TryParse(val, System.Globalization.NumberStyles.Any, 
                            System.Globalization.CultureInfo.InvariantCulture, out _))
                        {
                            return val;
                        }
                    }
                }
                return "0";
            });
        }

        /// <summary>
        /// Elabora funzioni condizionali SE/IF nella formula
        /// Sintassi: SE(condizione; valore_se_vero; valore_se_falso)
        /// Condizioni supportate: <, <=, >, >=, =, !=
        /// Supporta formato italiano con virgola decimale: 1,5 viene convertito in 1.5
        /// Esempio: SE(100<0; 100*0,10; 0) → 0
        /// Esempio: SE(-50<0; -50*1,5/100; 0) → -0.75
        /// </summary>
        private static string ElaboraFunzioniCondizionali(string formula)
        {
            // Converte le virgole decimali italiane in punti (es: 1,5 → 1.5)
            // Ma preserva il ; come separatore di argomenti
            formula = ConvertVirgoleDecimaliInPunti(formula);
            
            // Pattern per SE(...) o IF(...)
            // Nota: usa ; come separatore per evitare conflitti con la virgola decimale
            var pattern = @"(?:SE|IF)\s*\(\s*(.+?)\s*;\s*(.+?)\s*;\s*(.+?)\s*\)";
            
            while (Regex.IsMatch(formula, pattern, RegexOptions.IgnoreCase))
            {
                formula = Regex.Replace(formula, pattern, match =>
                {
                    var condizione = match.Groups[1].Value.Trim();
                    var seVero = match.Groups[2].Value.Trim();
                    var seFalso = match.Groups[3].Value.Trim();

                    // Valuta la condizione
                    var risultatoCondizione = ValutaCondizione(condizione);

                    // Ritorna il valore appropriato
                    return risultatoCondizione ? seVero : seFalso;
                }, RegexOptions.IgnoreCase);
            }

            return formula;
        }

        /// <summary>
        /// Valuta una condizione di confronto (es: "100<0", "-50>=0")
        /// </summary>
        private static bool ValutaCondizione(string condizione)
        {
            // Pattern per estrarre: valore1 operatore valore2
            var operatori = new[] { "<=", ">=", "!=", "<>", "<", ">", "=" };
            
            foreach (var op in operatori)
            {
                var parti = condizione.Split(new[] { op }, 2, StringSplitOptions.None);
                if (parti.Length == 2)
                {
                    var sinistra = parti[0].Trim().Replace(",", ".");
                    var destra = parti[1].Trim().Replace(",", ".");

                    // Prova a calcolare le espressioni se contengono operatori matematici
                    var valSinistra = CalcolaEspressioneSemplice(sinistra);
                    var valDestra = CalcolaEspressioneSemplice(destra);

                    return op switch
                    {
                        "<" => valSinistra < valDestra,
                        "<=" => valSinistra <= valDestra,
                        ">" => valSinistra > valDestra,
                        ">=" => valSinistra >= valDestra,
                        "=" => valSinistra == valDestra,
                        "!=" or "<>" => valSinistra != valDestra,
                        _ => false
                    };
                }
            }

            return false;
        }

        /// <summary>
        /// Calcola un'espressione semplice, restituendo 0 in caso di errore
        /// </summary>
        private static decimal CalcolaEspressioneSemplice(string espressione)
        {
            try
            {
                // Converti virgole italiane in punti
                espressione = espressione.Replace(",", ".");
                
                if (decimal.TryParse(espressione, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var numero))
                {
                    return numero;
                }

                // Prova a calcolare come espressione
                var dt = new DataTable();
                var result = dt.Compute(espressione, "");
                return Convert.ToDecimal(result);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Converte le virgole decimali italiane in punti
        /// Riconosce pattern come: numero,decimale (es: 1,5 → 1.5, 10,25 → 10.25)
        /// Non converte il ; che è il separatore di argomenti SE()
        /// </summary>
        private static string ConvertVirgoleDecimaliInPunti(string formula)
        {
            // Pattern: cifra + virgola + cifra (es: 1,5 o 100,25)
            return Regex.Replace(formula, @"(\d),(\d)", "$1.$2");
        }

        /// <summary>
        /// Calcola il valore di un campo calcolato per un RecordEntita
        /// </summary>
        public static string? CalcolaPerRecordEntita(CampoEntita campoCalcolato, RecordEntita record, IEnumerable<CampoEntita> tuttiCampi)
        {
            if (!campoCalcolato.IsCalculated || string.IsNullOrEmpty(campoCalcolato.Formula))
                return null;

            // Costruisci dizionario dei valori (case-insensitive)
            var valori = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var campo in tuttiCampi.Where(c => !c.IsCalculated))
            {
                var valore = record.Valori?.FirstOrDefault(v => v.CampoEntitaId == campo.Id)?.Valore ?? "";
                valori[campo.Nome] = valore;
            }

            return CalcolaFormulaGenerica(campoCalcolato.Formula, valori);
        }

        /// <summary>
        /// Valida una formula per entità, restituendo eventuali errori
        /// </summary>
        public static string? ValidaFormulaEntita(string? formula, IEnumerable<CampoEntita> campiDisponibili)
        {
            if (string.IsNullOrWhiteSpace(formula))
                return "La formula è obbligatoria per i campi calcolati.";

            // Estrai tutti i riferimenti ai campi
            var riferimenti = Regex.Matches(formula, @"\[([^\]]+)\]")
                .Cast<Match>()
                .Select(m => m.Groups[1].Value)
                .ToList();

            if (!riferimenti.Any())
                return "La formula deve contenere almeno un riferimento a un campo (es: [NomeCampo]).";

            // Verifica che tutti i campi referenziati esistano e non siano calcolati
            var campiNonCalcolati = campiDisponibili.Where(c => !c.IsCalculated).ToList();
            foreach (var riferimento in riferimenti)
            {
                var campo = campiDisponibili.FirstOrDefault(c => 
                    c.Nome.Equals(riferimento, StringComparison.OrdinalIgnoreCase));
                
                if (campo == null)
                {
                    var nomiDisponibili = string.Join(", ", campiNonCalcolati.Select(c => $"[{c.Nome}]"));
                    return $"Il campo [{riferimento}] non esiste. Campi disponibili: {nomiDisponibili}";
                }
                
                if (campo.IsCalculated)
                    return $"Il campo [{riferimento}] è calcolato e non può essere referenziato.";
            }

            // Prova a calcolare con valori di test
            try
            {
                var valoriTest = campiDisponibili
                    .Where(c => !c.IsCalculated)
                    .ToDictionary(c => c.Nome, c => "1");
                
                CalcolaFormulaGenerica(formula, valoriTest);
            }
            catch (Exception ex)
            {
                return $"Errore nella formula: {ex.Message}";
            }

            return null; // Valida
        }
    }
}
