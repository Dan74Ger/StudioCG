## Opzione B — Template HTML + editor “tipo Word” (WYSIWYG) + campi configurabili

### Obiettivo
Creare un **sistema unico di template** per documenti (Mandato, Privacy, Consegna clienti, Antiriciclaggio, e **altri template futuri**) con:
- testo e layout modificabili in app (editor “tipo Word” nel browser)
- **campi** inseribili (dati cliente, indirizzo, città, CF/P.IVA, legale rappresentante, soci, importi mandato, ecc.)
- anteprima con dati reali
- generazione documento finale (consigliato: PDF; opzionale DOCX in fase successiva)

---

## 1) Libreria Template (generica)
Una sezione/pagina “**Template Documenti**” con:
- elenco template
- **Nuovo template**
- **Duplica template**
- modifica template in editor WYSIWYG
- **Anteprima/Test** con un contesto reale (cliente/mandato/scadenza)

Ogni template salva:
- **Nome/Titolo**
- **Categoria/Tipo** (es. Mandati, Privacy, Comunicazioni, Antiriciclaggio, ecc.)
- **Contesto richiesto** (es. Solo Cliente / Cliente+Mandato / Cliente+Mandato+Scadenza)
- **ContenutoHtml** (il contenuto editato, con placeholder)
- opzionale: **HeaderHtml**, **FooterHtml**
- opzionale: versione, timestamps, attivo/disattivo

---

## 2) Catalogo Campi (field catalog) + inserimento sicuro
Invece di scrivere i tag a mano, l’app offre un pannello laterale con:
- **gruppi** (Cliente, Indirizzo, LegaleRapp, Soci, Mandato, Studio/Professionista, Varie)
- **ricerca** (typeahead: “piva”, “cf”, “pec”, ecc.)
- click su un campo → inserisce un token nel punto del cursore

Formato token consigliato:
- `{{Cliente.RagioneSociale}}`
- `{{Cliente.CF}}`
- `{{Cliente.PIVA}}`
- `{{Cliente.Indirizzo}}`, `{{Cliente.CAP}}`, `{{Cliente.Citta}}`, `{{Cliente.Provincia}}`
- `{{LegaleRapp.Nome}}`, `{{LegaleRapp.Cognome}}`, `{{LegaleRapp.CF}}`, ecc.
- `{{Mandato.ImportoMandato}}`, `{{Mandato.Anno}}`, `{{Mandato.Oggetto}}`, ecc.
- `{{Studio.RagioneSociale}}`, `{{Studio.PIVA}}`, `{{Studio.PEC}}`, ecc.

---

## 3) Liste ripetute (Soci / righe)
Per dati multipli (es. Soci), serve un blocco ripetibile (loop) nel template.

Esempio:
- `{{#Soci}} ... {{/Soci}}`
  - dentro: `{{Nome}} {{Cognome}} - {{Percentuale}}% - CF {{CF}}`

Nell’editor si vede la struttura; in anteprima si espande con tutte le righe reali.

---

## 4) Contesto dati (da dove arrivano i valori)
Quando fai anteprima/generi, scegli un contesto (a seconda del template):
- ClienteId
- MandatoId (se richiesto)
- ScadenzaId (se richiesto)
- altri metadati (Data odierna, Operatore, ecc.)

Il motore compone un “view model” unico e sostituisce i token nel contenuto HTML.

---

## 5) Anteprima e Output
Anteprima:
- render dell’HTML con sostituzione token con dati reali (preview in pagina).

Output:
- **PDF (consigliato)**: stabile e uniforme.
- **DOCX (opzionale fase 2)**: possibile ma più delicato (conversioni/impaginazione).

---

## 6) Creazione di nuovi template oltre ai predefiniti
Sì: il sistema è **generico**, quindi puoi creare **qualsiasi nuovo template**:
- “Nuovo template” → scrivi testo/layout → inserisci campi → salva → usa.

Opzionale:
- “Campi preferiti per template” (per mostrare prima quelli più usati per quel documento)
- “Campi personalizzati per template” (input manuali richiesti in fase di generazione: testo libero/checkbox)


