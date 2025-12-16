# 📋 SEZIONE AMMINISTRAZIONE - LOGICA FATTURAZIONE + INCASSI

## 🗂️ PAGINE DA CREARE (SEPARATE)

| # | Pagina | Descrizione |
|---|--------|-------------|
| 1 | **Dashboard Fatturazione** | Riepilogo generale + gestione anno |
| 2 | **Mandati Clienti** | CRUD mandati + genera scadenze |
| 3 | **Scadenze Fatturazione** | Proforma/fattura + gestione spese |
| 4 | **Spese Pratiche** | CRUD spese |
| 5 | **Accessi Clienti** | CRUD accessi (ore, tariffa) |
| 6 | **Fatture in Cloud** | Importo + scadenza configurabili (default 31/10) |
| 7 | **Bilanci CEE** | Solo SC, importo + scadenza configurabili (default 31/03) |
| 8 | **💰 INCASSI** | **PAGINA SEPARATA** - gestione incassi (anche parziali) |
| 9 | **📊 Report Professionisti** | Fatturato/incassato per professionista/mese |

**❌ NESSUN CESTINO** - Cancellazione definitiva con conferma

---

## 🔗 FLUSSO TRA PAGINE

```
PAGINA                    PAGINA                    PAGINA
FATTURAZIONE              INCASSI                   REPORT
     │                         │                        │
     │  Emetto Fattura         │                        │
     │ ─────────────────────→  │  Fattura appare        │
     │                         │  come "Da incassare"   │
     │                         │                        │
     │                         │  Registro incasso      │
     │                         │  + suddivisione        │
     │                         │ ─────────────────────→ │
     │                         │                        │  Vedo totali
     │                         │                        │  per profess.
```

---

## 📋 STATI PER PAGINA

### In PAGINA FATTURAZIONE:
```
APERTA → PROFORMA → FATTURATA (esce da qui, va in Incassi)
   ↓
   └───────────→ FATTURATA (esce da qui, va in Incassi)
```

### In PAGINA INCASSI:
```
DA INCASSARE → PARZIALMENTE INCASSATA → INCASSATA ✅
```

---

## ✏️ AZIONI PER PAGINA

### PAGINA FATTURAZIONE:

| Azione | APERTA | PROFORMA | FATTURATA |
|--------|:------:|:--------:|:---------:|
| Modifica scadenza | ✅ | ⛔ | ⛔ |
| Cancella scadenza | ✅ | ⛔ | ⛔ |
| Modifica spese | ✅ | ⚠️ Avviso | ⛔ |
| Cancella spese | ✅ | ⚠️ Avviso | ⛔ |
| Aggiungi spese | ✅ | ⚠️ Avviso | ⛔ |
| Assegna Proforma | ✅ | ⛔ | ⛔ |
| Assegna Fattura | ✅ | ✅ | ⛔ |
| Annulla Proforma | - | ✅ | ⛔ |
| Annulla Fattura | - | - | ✅* |

*Solo ultima fattura, e solo se NON ha incassi registrati

### PAGINA INCASSI:

| Azione | DA INCASSARE | PARZIALE | INCASSATA |
|--------|:------------:|:--------:|:---------:|
| Registra incasso | ✅ | ✅ | ⛔ |
| Modifica incasso | - | ✅** | ✅** |
| Cancella incasso | - | ✅** | ✅** |

**Solo ultimo incasso registrato

---

## 🗑️ CANCELLAZIONE (SENZA CESTINO)

| Cancello da | Effetto |
|-------------|---------|
| **Spese Pratiche** | Sparisce anche da **Fatturazione** |
| **Fatturazione** | Sparisce anche da **Spese Pratiche** |
| **Accessi Clienti** | Sparisce anche da **Fatturazione** |
| **Incasso** | Importo torna "da incassare", fattura torna stato precedente |
| **Fattura (senza incassi)** | Torna in Fatturazione, scadenza torna "aperta" o "proforma" |

**Sempre popup conferma → Eliminazione DEFINITIVA → Per recuperare: reinserire manualmente**

---

## 💰 CALCOLO IMPORTO RESIDUO MANDATO

```
Importo Residuo = Totale Annuo - (Fatturato + Proformato)
```

| Evento | Effetto su Residuo |
|--------|-------------------|
| Emetto **Fattura** | Sottrae (definitivo) |
| Emetto **Proforma** | Sottrae (impegnato) |
| Annullo **Proforma** | Libera importo |
| Annullo **Fattura** (ultima, senza incassi) | Libera importo |

---

## 💰 INCASSI PARZIALI

Una fattura può essere incassata in più tranche:

```
Fattura n.16 - € 1.500,00

Incassi registrati:
├── 10/05/2025 - € 500,00 - Mario 50%, Giuseppe 50%
├── 25/05/2025 - € 700,00 - Mario 60%, Anna 40%
├─────────────────────────────────────────────────
│   INCASSATO:  € 1.200,00
│   RESIDUO:    €   300,00
│   Stato: PARZIALMENTE INCASSATA
└── [➕ Aggiungi Incasso] per restanti € 300,00
```

---

## 📊 SUDDIVISIONE TRA PROFESSIONISTI

Ogni incasso (anche parziale) viene suddiviso tra i professionisti:

```
Incasso € 500,00 del 10/05/2025:
├── Mario Rossi:     50% = € 250,00
├── Giuseppe Verdi:  30% = € 150,00
├── Anna Bianchi:    20% = € 100,00
└── TOTALE:         100% = € 500,00 ✅
```

La percentuale può variare per ogni incasso.

---

## 📊 REPORT PROFESSIONISTI - DETTAGLIO

Il report mostra **due modalità di visualizzazione**:

### 1️⃣ SOLO MESE (senza riporti)
Mostra esclusivamente gli incassi registrati nel mese selezionato.

```
REPORT MAGGIO 2025 (Solo Mese)
┌───────────────────┬──────────────┬──────────────┬──────────────┐
│ Professionista    │ Fatturato    │ Incassato    │ Da Incassare │
├───────────────────┼──────────────┼──────────────┼──────────────┤
│ Mario Rossi       │ € 3.500      │ € 2.800      │ € 700        │
│ Giuseppe Verdi    │ € 2.100      │ € 1.600      │ € 500        │
│ Anna Bianchi      │ € 1.400      │ € 1.000      │ € 400        │
├───────────────────┼──────────────┼──────────────┼──────────────┤
│ TOTALE            │ € 7.000      │ € 5.400      │ € 1.600      │
└───────────────────┴──────────────┴──────────────┴──────────────┘
```

### 2️⃣ CON RIPORTI (progressivo)
Mostra il **totale cumulativo** da inizio anno fino al mese selezionato.

```
REPORT MAGGIO 2025 (Progressivo Gen-Mag)
┌───────────────────┬──────────────┬──────────────┬──────────────┐
│ Professionista    │ Fatturato    │ Incassato    │ Da Incassare │
├───────────────────┼──────────────┼──────────────┼──────────────┤
│ Mario Rossi       │ € 15.200     │ € 12.500     │ € 2.700      │
│ Giuseppe Verdi    │ € 9.800      │ € 8.100      │ € 1.700      │
│ Anna Bianchi      │ € 6.500      │ € 5.200      │ € 1.300      │
├───────────────────┼──────────────┼──────────────┼──────────────┤
│ TOTALE            │ € 31.500     │ € 25.800     │ € 5.700      │
└───────────────────┴──────────────┴──────────────┴──────────────┘
```

### Interfaccia Report

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ 📊 REPORT PROFESSIONISTI                                                    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Anno: [2025 ▼]   Mese: [Maggio ▼]   Modalità: (●) Solo Mese (○) Progressivo│
│                                                                             │
│  [🔄 Aggiorna]  [📥 Export Excel]                                           │
│                                                                             │
│  ... tabella dati ...                                                       │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Calcolo

| Modalità | Formula |
|----------|---------|
| **Solo Mese** | Somma incassi dove `DataIncasso` è nel mese X |
| **Progressivo** | Somma incassi dove `DataIncasso` è da 01/01 a fine mese X |

Il **"Da Incassare"** rappresenta le fatture emesse nel periodo ma non ancora incassate (o parzialmente incassate).

---

## 🔢 CONTATORI AUTOMATICI

| Tipo | Comportamento |
|------|---------------|
| **Proforma** | Per anno, propone numero successivo |
| **Fattura** | Per anno, propone numero successivo |
| **Annullo ultimo** | Contatore torna indietro |

---

## 🔄 MODIFICA SCADENZE IN CORSO D'ANNO

### Esempio: Trimestrale → Annuale (a metà anno)

```
PRIMA (Trimestrale €12.000/anno):
├── 31/03 - €3.000 - FATTURATA ⛔ (protetta)
├── 30/06 - €3.000 - PROFORMA ⛔ (protetta)
├── 30/09 - €3.000 - APERTA ✅ (eliminabile)
└── 31/12 - €3.000 - APERTA ✅ (eliminabile)

DOPO (cambio a scadenza unica):
├── 31/03 - €3.000 - FATTURATA ⛔ (resta)
├── 30/06 - €3.000 - PROFORMA ⛔ (resta)
└── 31/12 - €6.000 - NUOVA ✅ (residuo calcolato)
```

### Regole:
- Scadenze con fattura/proforma → **protette, intoccabili**
- Scadenze aperte → **modificabili/eliminabili**
- Nuove scadenze → devono essere **DOPO l'ultima protetta**
- Importo residuo → **calcolato automaticamente**

---

## 📅 CAMBIO ANNO FATTURAZIONE

```
Pulsante "Genera Anno 2027":

1. Copia mandati attivi → importi/tipo scadenza modificabili
2. Copia Fatture in Cloud → importi/scadenze modificabili
3. Copia Bilanci CEE → importi/scadenze modificabili
4. Spese Pratiche → partono da zero
5. Accessi Clienti → partono da zero
6. Incassi → partono da zero (nuove fatture nuovo anno)

Dropdown per navigare tra anni (storico sempre visibile)
```

---

## 🔧 TABELLE DATABASE

### MandatoCliente
| Campo | Tipo | Descrizione |
|-------|------|-------------|
| Id | int | PK |
| ClienteId | int | FK Cliente |
| Anno | int | Anno fatturazione |
| ImportoAnnuo | decimal | Importo totale annuo |
| TipoScadenza | enum | Mensile/Bimestrale/Trimestrale/Semestrale/Annuale |
| IsActive | bool | Attivo |
| CreatedAt | DateTime | Data creazione |

### ScadenzaFatturazione
| Campo | Tipo | Descrizione |
|-------|------|-------------|
| Id | int | PK |
| MandatoClienteId | int | FK Mandato |
| ClienteId | int | FK Cliente |
| DataScadenza | DateTime | Data scadenza |
| ImportoMandato | decimal | Importo rata mandato |
| NumeroProforma | int? | Numero proforma |
| DataProforma | DateTime? | Data proforma |
| NumeroFattura | int? | Numero fattura |
| DataFattura | DateTime? | Data fattura |
| Stato | enum | Aperta/Proforma/Fatturata |
| Note | string | Note |
| CreatedAt | DateTime | Data creazione |

### SpesaPratica
| Campo | Tipo | Descrizione |
|-------|------|-------------|
| Id | int | PK |
| ClienteId | int | FK Cliente |
| ScadenzaFatturazioneId | int | FK Scadenza destinazione |
| UtenteId | int | FK Utente che inserisce |
| Descrizione | string | Descrizione spesa |
| Importo | decimal | Importo |
| Data | DateTime | Data spesa |
| CreatedAt | DateTime | Data creazione |

### AccessoCliente
| Campo | Tipo | Descrizione |
|-------|------|-------------|
| Id | int | PK |
| ClienteId | int | FK Cliente |
| ScadenzaFatturazioneId | int | FK Scadenza destinazione |
| UtenteId | int | FK Utente |
| Data | DateTime | Data accesso |
| OraInizioMattino | TimeSpan? | Ora inizio mattino |
| OraFineMattino | TimeSpan? | Ora fine mattino |
| OraInizioPomeriggio | TimeSpan? | Ora inizio pomeriggio |
| OraFinePomeriggio | TimeSpan? | Ora fine pomeriggio |
| TariffaOraria | decimal | €/h |
| TotaleOre | decimal | Calcolato |
| TotaleImporto | decimal | Calcolato (ore × tariffa) |
| Note | string | Note |
| CreatedAt | DateTime | Data creazione |

### FatturaCloud
| Campo | Tipo | Descrizione |
|-------|------|-------------|
| Id | int | PK |
| ClienteId | int | FK Cliente |
| Anno | int | Anno fatturazione |
| Importo | decimal | Importo |
| DataScadenza | DateTime | Scadenza (default 31/10) |
| ScadenzaFatturazioneId | int? | FK Scadenza associata |
| CreatedAt | DateTime | Data creazione |

### BilancioCEE
| Campo | Tipo | Descrizione |
|-------|------|-------------|
| Id | int | PK |
| ClienteId | int | FK Cliente (solo tipo SC) |
| Anno | int | Anno fatturazione |
| Importo | decimal | Importo |
| DataScadenza | DateTime | Scadenza (default 31/03) |
| ScadenzaFatturazioneId | int? | FK Scadenza associata |
| CreatedAt | DateTime | Data creazione |

### ContatoreDocumento
| Campo | Tipo | Descrizione |
|-------|------|-------------|
| Id | int | PK |
| Anno | int | Anno |
| TipoDocumento | enum | Proforma/Fattura |
| UltimoNumero | int | Ultimo numero utilizzato |

### IncassoFattura
| Campo | Tipo | Descrizione |
|-------|------|-------------|
| Id | int | PK |
| ScadenzaFatturazioneId | int | FK Scadenza fatturata |
| DataIncasso | DateTime | Data incasso |
| ImportoIncassato | decimal | Importo questo incasso (può essere parziale) |
| Note | string | Note |
| CreatedAt | DateTime | Data creazione |

### IncassoProfessionista
| Campo | Tipo | Descrizione |
|-------|------|-------------|
| Id | int | PK |
| IncassoFatturaId | int | FK Incasso |
| UtenteId | int | FK Utente/Professionista |
| Percentuale | decimal | % assegnata |
| Importo | decimal | Importo calcolato |

---

## 📆 TIPI SCADENZA MANDATO

| Tipo | Rate | Date (esempio 2025) |
|------|:----:|---------------------|
| **Mensile** | 12 | 31/01, 28/02, 31/03, 30/04, 31/05, 30/06, 31/07, 31/08, 30/09, 31/10, 30/11, 31/12 |
| **Bimestrale** | 6 | 28/02, 30/04, 30/06, 31/08, 31/10, 31/12 |
| **Trimestrale** | 4 | 31/03, 30/06, 30/09, 31/12 |
| **Semestrale** | 2 | 30/06, 31/12 |
| **Annuale** | 1 | 31/12 |

---

## 🚗 ACCESSI CLIENTI - DETTAGLIO

| Campo | Tipo | Note |
|-------|------|------|
| Cliente | Selezione | Dropdown clienti |
| Data | Data | Data dell'accesso |
| Ora inizio mattino | Ora | Opzionale |
| Ora fine mattino | Ora | Opzionale |
| Ora inizio pomeriggio | Ora | Opzionale |
| Ora fine pomeriggio | Ora | Opzionale |
| Tariffa €/h | Decimale | Tariffa oraria |
| Totale ore | Calcolato | Somma ore mattino + pomeriggio |
| Totale importo | Calcolato | ore × tariffa |
| Scadenza destinazione | Selezione | A quale scadenza addebitare |

---

## 🔗 SINCRONIZZAZIONE DATI TRA PAGINE

```
         ┌──────────────────────────────────────┐
         │           DATABASE                   │
         │  ┌─────────┐  ┌─────────┐  ┌──────┐  │
         │  │ Spese   │  │ Accessi │  │Incassi│ │
         │  │Pratiche │  │ Clienti │  │       │ │
         │  └────┬────┘  └────┬────┘  └───┬──┘  │
         └───────┼────────────┼───────────┼─────┘
                 │            │           │
    ┌────────────┼────────────┼───────────┼────────────┐
    │            │            │           │            │
    ▼            ▼            ▼           ▼            ▼
┌────────┐  ┌────────┐  ┌────────┐  ┌────────┐  ┌────────┐
│Spese   │  │Accessi │  │FATTURA-│  │INCASSI │  │REPORT  │
│Pratiche│  │Clienti │  │ZIONE   │  │(separ.)│  │PROFESS.│
│(pagina)│  │(pagina)│  │(pagina)│  │(pagina)│  │(pagina)│
└────────┘  └────────┘  └────────┘  └────────┘  └────────┘

⚠️ Spese/Accessi: cancello da una → sparisce da tutte
⚠️ Incassi: pagina SEPARATA, gestisce solo incassi
```

---

## 🔐 PERMESSI UTENTI

Tutti i permessi sono nella categoria **AMMINISTRAZIONE** e controllati dal sistema esistente.

| ID | Pagina | URL | Icona |
|:--:|--------|-----|-------|
| 200 | Dashboard Fatturazione | `/Amministrazione` | `fas fa-chart-line` |
| 201 | Mandati Clienti | `/Amministrazione/Mandati` | `fas fa-file-contract` |
| 202 | Scadenze Fatturazione | `/Amministrazione/Scadenze` | `fas fa-file-invoice-dollar` |
| 203 | Spese Pratiche | `/Amministrazione/SpesePratiche` | `fas fa-receipt` |
| 204 | Accessi Clienti | `/Amministrazione/AccessiClienti` | `fas fa-door-open` |
| 205 | Fatture in Cloud | `/Amministrazione/FattureCloud` | `fas fa-cloud` |
| 206 | Bilanci CEE | `/Amministrazione/BilanciCEE` | `fas fa-balance-scale` |
| 207 | Incassi | `/Amministrazione/Incassi` | `fas fa-money-bill-wave` |
| 208 | Report Professionisti | `/Amministrazione/ReportProfessionisti` | `fas fa-user-tie` |

### Assegnazione Permessi

- **Admin** → Accesso a tutto automaticamente
- **Utenti normali** → L'admin assegna i permessi dalla pagina Gestione Utenti
- I permessi controllano:
  - Visibilità menu laterale
  - Accesso alle pagine
  - Azioni disponibili

---

## 📅 FASI DI SVILUPPO

```
FASE 1 - Core Fatturazione:
├── 1.1 Modelli database + Migration
├── 1.2 Menu Amministrazione in navbar
├── 1.3 Dashboard Fatturazione
├── 1.4 Mandati Clienti (CRUD + genera scadenze)
├── 1.5 Scadenze Fatturazione (lista + proforma/fattura)
└── 1.6 Contatori automatici

FASE 2 - Spese:
├── 2.1 Spese Pratiche
├── 2.2 Fatture in Cloud
└── 2.3 Bilanci CEE

FASE 3 - Accessi:
└── 3.1 Accessi Clienti

FASE 4 - Incassi (PAGINA SEPARATA):
├── 4.1 Pagina Incassi (lista fatture da incassare)
├── 4.2 Registrazione incasso (anche parziale)
├── 4.3 Suddivisione tra professionisti
└── 4.4 Report Professionisti (fatturato/incassato)

FASE 5 - Cambio Anno:
└── 5.1 Wizard genera anno successivo

FASE 6 - Template (dopo):
└── 6.1 Template Mandato Word
```

---

*Documento aggiornato*
*Versione: 2.0 - Aggiunta sezione Incassi separata*
