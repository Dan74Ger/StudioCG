## Budget Studio — Specifica (riepilogo)

### Obiettivo
Creare una nuova voce di menu **Budget Studio** con una pagina che consente:
- gestire l’**anagrafica voci di spesa**
- compilare un **prospetto mensile Gen–Dic** (righe = voci, colonne = mesi, + totale)
- duplicare/importare facilmente gli importi (tutto anno / intervallo / mesi selezionati)
- gestire per ogni mese lo stato **Pagata SI/NO**
- di default mostrare **solo NON pagate**, con filtro: **Non pagate / Pagate / Tutte**
- poter scegliere **quali mesi visualizzare** (filtro mesi)

---

## Modello dati

### 1) VociSpesa (anagrafica)
Campi:
- **CodiceSpesa** (string, univoco)
- **Descrizione** (string)
- **MetodoPagamentoDefault** (enum: Bonifico, RiBa, Contanti, Carta, SDD, Altro)
- **NoteDefault** (string opzionale)
- (opzionale) **Categoria** (string o enum)
- (opzionale) **IsActive**

### 2) BudgetSpese (righe mensili)
Una riga per: **VoceSpesa + Anno + Mese**

Campi:
- **VoceSpesaId** (FK)
- **Anno** (int)
- **Mese** (int 1..12)
- **Importo** (decimal)
- **Pagata** (bool)
- **MetodoPagamento** (override opzionale)
- **Note** (override opzionale)

Vincolo:
- unique index su (**VoceSpesaId**, **Anno**, **Mese**)

---

## UI/UX (semplice e veloce)

### Pagina “Budget Studio” con 2 tab
1) **Tab Anagrafica Voci**
- tabella CRUD (Codice, Descrizione, Metodo default, attiva/disattiva)
- ricerca + “Nuova voce”

2) **Tab Prospetto Budget (Anno selezionabile)**
- tabella stile Excel: **Gen…Dic + Totale riga**
- totali per mese in fondo + totale annuo

### Filtri (Tab Prospetto)
In alto:
- **Anno**
- **Filtro stato**: **Non pagate** (default) / **Pagate** / **Tutte**
- **Filtro mesi visibili**:
  - modalità semplice: **Da mese → A mese**
  - modalità completa: **checkbox mesi** (scegli esattamente quali colonne mostrare)

### Filtro Pagate
In alto:
- **Non pagate** (default)
- **Pagate**
- **Tutte**

Comportamento:
- per default le righe/celle **Pagate** non vengono mostrate (in base al filtro).

### Comportamento click su cella (importo)
La griglia deve essere velocissima:
- **Click singolo**: edit inline dell’**importo** (scrivi → invio salva, ESC annulla)
- **Doppio click** (o icona “…”): apre “Dettaglio cella” (modale/pannello laterale)

### Editing importi (operatività)
- **Edit inline** delle celle importo (scrivi → invio)
- “Dettaglio cella” (icona o doppio click) per:
  - toggle **Pagata SI/NO**
  - override Metodo pagamento
  - Note (non mostrate nel prospetto; gestite solo nel dettaglio)

### Azioni rapide (per voce e per cella)
Per riga (voce):
- **Compila tutto l’anno** (un importo → Gen–Dic)
- **Compila intervallo** (es. Apr–Dic)
- **Copia su mesi selezionati**
- **Azzera anno per voce**

Per cella:
- **Copia su mesi selezionati**
- **Segna pagata/non pagata**

### Contenuti del “Dettaglio cella”
Mostra e permette modifica di:
- **Voce** (Codice + Descrizione), **Anno**, **Mese**
- **Importo**
- **Pagata SI/NO**
- **Metodo pagamento** (default della voce, con override per mese)
- **Note** (opzionali, non in tabella)
Azioni rapide dentro il dettaglio:
- copia importo su mesi selezionati
- compila intervallo (da mese a mese)
- (opzionale) azzera mese


