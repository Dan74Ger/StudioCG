# StudioCG - Documentazione Sistema

## Panoramica

Applicazione ASP.NET Core MVC (.NET 9) con SQL Express per la gestione di uno Studio Professionale Commercialista.

---

## 1. SISTEMA UTENTI

### 1.1 Modelli

| File | Descrizione |
|------|-------------|
| `Models/User.cs` | Modello utente con Id, Username, PasswordHash, Nome, Cognome, Email, IsActive, IsAdmin |
| `Models/Permission.cs` | Modello permessi pagine con PageName, PageUrl, Category, Icon, ShowInMenu |
| `Models/UserPermission.cs` | Collegamento utente-permesso con CanView, CanEdit, CanCreate, CanDelete |

### 1.2 Tabelle Database

```sql
-- Utenti
CREATE TABLE Users (
    Id INT PRIMARY KEY IDENTITY,
    Username NVARCHAR(50) NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    Nome NVARCHAR(100),
    Cognome NVARCHAR(100),
    Email NVARCHAR(255),
    IsActive BIT DEFAULT 1,
    IsAdmin BIT DEFAULT 0,
    DataCreazione DATETIME
)

-- Permessi/Pagine
CREATE TABLE Permissions (
    Id INT PRIMARY KEY IDENTITY,
    PageName NVARCHAR(100) NOT NULL,
    PageUrl NVARCHAR(255) NOT NULL,
    Description NVARCHAR(500),
    Icon NVARCHAR(100),
    DisplayOrder INT DEFAULT 0,
    ShowInMenu BIT DEFAULT 1,
    Category NVARCHAR(50)
)

-- Permessi Utente
CREATE TABLE UserPermissions (
    Id INT PRIMARY KEY IDENTITY,
    UserId INT NOT NULL,
    PermissionId INT NOT NULL,
    CanView BIT DEFAULT 1,
    CanEdit BIT DEFAULT 0,
    CanCreate BIT DEFAULT 0,
    CanDelete BIT DEFAULT 0
)
```

### 1.3 Controller

| File | Descrizione |
|------|-------------|
| `Controllers/AccountController.cs` | Login, Logout, AccessDenied |
| `Controllers/UsersController.cs` | CRUD utenti (solo admin) |
| `Controllers/PermissionsController.cs` | Gestione pagine e permessi utente |

### 1.4 Servizi

| File | Descrizione |
|------|-------------|
| `Services/PasswordService.cs` | Hash password SHA256 |
| `Services/AuthService.cs` | Autenticazione utenti |
| `Services/PermissionService.cs` | Verifica permessi, menu dinamico |

### 1.5 Filtri

| File | Descrizione |
|------|-------------|
| `Filters/AdminOnlyAttribute.cs` | Restringe accesso solo a utente "admin" |

### 1.6 Logica Permessi

- **admin** (username) → Accesso completo a tutto, sempre
- **Altri utenti** → Vedono solo pagine con CanView=true
- **IsAdmin flag** → NON dà accesso alla gestione utenti (solo "admin" può)

---

## 2. SISTEMA PAGINE DINAMICHE

### 2.1 Concetto

Permette all'admin di creare pagine personalizzate con campi custom, senza scrivere codice.
Le pagine vengono mostrate sotto **DATI UTENZA** → **DATI Riservati** o **DATI Generali**.

### 2.2 Modelli

| File | Descrizione |
|------|-------------|
| `Models/DynamicPage.cs` | Definizione pagina (Nome, Categoria, Icona, ecc.) |
| `Models/DynamicField.cs` | Campi della pagina (Testo, Numero, Data, Checkbox, ecc.) |
| `Models/DynamicRecord.cs` | Record/righe dati inseriti |
| `Models/DynamicFieldValue.cs` | Valori dei campi per ogni record |

### 2.3 Tabelle Database

```sql
-- Pagine Dinamiche
CREATE TABLE DynamicPages (
    Id INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(100) NOT NULL,
    TableName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500),
    Category NVARCHAR(50),          -- "DatiRiservati" o "DatiGenerali"
    Icon NVARCHAR(100),
    DisplayOrder INT DEFAULT 0,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME
)

-- Campi Dinamici
CREATE TABLE DynamicFields (
    Id INT PRIMARY KEY IDENTITY,
    DynamicPageId INT NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    Label NVARCHAR(100) NOT NULL,
    FieldType INT NOT NULL,         -- 0=Text, 1=LongText, 2=Number, 3=Decimal, 4=Date, 5=Boolean
    IsRequired BIT DEFAULT 0,
    ShowInList BIT DEFAULT 1,
    DisplayOrder INT DEFAULT 0
)

-- Record Dinamici
CREATE TABLE DynamicRecords (
    Id INT PRIMARY KEY IDENTITY,
    DynamicPageId INT NOT NULL,
    CreatedAt DATETIME,
    CreatedBy NVARCHAR(100),
    UpdatedAt DATETIME
)

-- Valori Campi
CREATE TABLE DynamicFieldValues (
    Id INT PRIMARY KEY IDENTITY,
    DynamicRecordId INT NOT NULL,
    DynamicFieldId INT NOT NULL,
    Value NVARCHAR(MAX)
)
```

### 2.4 Tipi di Campo Disponibili

| Tipo | Enum | Descrizione |
|------|------|-------------|
| Testo | `FieldTypes.Text` | Input testo singola riga |
| Testo Lungo | `FieldTypes.LongText` | Textarea multiriga |
| Numero | `FieldTypes.Number` | Numeri interi |
| Decimale | `FieldTypes.Decimal` | Numeri con virgola |
| Data | `FieldTypes.Date` | Selettore data |
| Sì/No | `FieldTypes.Boolean` | Checkbox |

### 2.5 Controller

| File | Descrizione |
|------|-------------|
| `Controllers/DynamicPagesController.cs` | CRUD pagine e gestione campi (solo admin) |
| `Controllers/DynamicDataController.cs` | CRUD record con controllo permessi |

### 2.6 Views

| Cartella | Descrizione |
|----------|-------------|
| `Views/DynamicPages/` | Index, Create, Edit, Delete, Fields |
| `Views/DynamicData/` | Page (lista record), Create, Edit, Delete |

---

## 3. FLUSSO CREAZIONE NUOVA PAGINA

### Passo 1: Crea la Pagina
1. Vai in **DATI UTENZA** → **Gestione Pagine Dati Utenza**
2. Clicca **"Nuova Pagina"**
3. Compila:
   - **Nome**: es. "Anagrafica Clienti"
   - **Categoria**: "DatiRiservati" o "DatiGenerali"
   - **Icona**: es. "fas fa-users" (vedi fontawesome.com)
   - **Ordine**: numero per ordinamento menu
4. Salva

### Passo 2: Aggiungi i Campi
1. Dopo il salvataggio, vai a **"Gestisci Campi"**
2. Per ogni campo:
   - **Etichetta**: nome visualizzato (es. "Ragione Sociale")
   - **Tipo**: Testo, Numero, Data, ecc.
   - **Obbligatorio**: se deve essere compilato
   - **Mostra in Lista**: se appare nella tabella principale
3. Usa le frecce per ordinare i campi

### Passo 3: Assegna Permessi
1. Vai in **UTENTI** → **Gestione Utenti**
2. Clicca l'icona **chiave** accanto all'utente
3. Trova la nuova pagina (badge azzurro = pagina dinamica)
4. Spunta i permessi:
   - ✅ Visualizza → può vedere la pagina
   - ✅ Crea → può aggiungere record
   - ✅ Modifica → può modificare record
   - ✅ Elimina → può eliminare record
5. Salva

### Passo 4: Usa la Pagina
- La pagina appare nel menu sotto la categoria scelta
- Gli utenti vedono solo le pagine per cui hanno permesso
- CRUD completo automatico!

---

## 4. STRUTTURA MENU

```
📊 Dashboard

👥 UTENTI (solo admin)
   ├── Gestione Utenti
   └── Gestione Pagine

📂 DATI UTENZA
   ├── ⚙️ Gestione Pagine Dati Utenza (solo admin)
   ├── 🔒 DATI Riservati
   │      └── [Pagine dinamiche categoria DatiRiservati]
   └── 📂 DATI Generali
          └── [Pagine dinamiche categoria DatiGenerali]
```

---

## 5. COME CREARE NUOVE SEZIONI

Per creare una nuova sezione simile a "DATI UTENZA":

### 5.1 Aggiungere Categoria

1. Modifica `Models/DynamicPage.cs` per aggiungere la nuova categoria:
```csharp
// Nel campo Category, usare valori come:
// "DatiRiservati", "DatiGenerali", "NuovaSezione"
```

2. Modifica `Views/DynamicPages/Create.cshtml` e `Edit.cshtml` per aggiungere l'opzione nel dropdown:
```html
<option value="NuovaSezione">Nuova Sezione</option>
```

### 5.2 Aggiungere al Menu

Modifica `Views/Shared/_Layout.cshtml`:

1. Carica le pagine della nuova categoria:
```csharp
var dynPagesNuova = await PermissionService.GetUserDynamicPagesAsync(currentUsername, "NuovaSezione");
```

2. Aggiungi l'accordion per la nuova sezione (copia il pattern di DATI Riservati/Generali)

### 5.3 Aggiungere Permission

Aggiungi un record nella tabella Permissions per la nuova sezione:
```sql
INSERT INTO Permissions (PageName, PageUrl, Description, Icon, ShowInMenu, DisplayOrder)
VALUES ('NUOVA SEZIONE', '/NUOVA SEZIONE', 'Descrizione', 'fas fa-folder', 1, 5)
```

---

## 6. FILE BATCH UTILI

| File | Descrizione |
|------|-------------|
| `copia.bat` | Backup ZIP applicazione + database SQL |
| `t.bat` | Termina l'applicazione in esecuzione |
| `commit.bat` | Git add + commit + push |

---

## 7. CONFIGURAZIONE

### Connection String
File: `appsettings.json`
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=StudioCG;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### Utente Admin Default
- **Username**: admin
- **Password**: 123456

---

## 8. COMANDI UTILI

```powershell
# Compilare
dotnet build

# Avviare
dotnet run

# Creare migrazione
dotnet ef migrations add NomeMigrazione

# Applicare migrazione
dotnet ef database update

# Backup database
sqlcmd -S ".\SQLEXPRESS" -Q "BACKUP DATABASE StudioCG TO DISK='C:\backup\StudioCG.bak'"
```

---

## Note

- Le pagine dinamiche creano automaticamente un Permission per la gestione dei permessi utente
- L'eliminazione di una pagina dinamica elimina anche il Permission e tutti i record associati
- Il menu mostra solo le sezioni che contengono pagine per cui l'utente ha permessi

---

*Documentazione creata il 12/12/2024*
*Developer: Dott. Geron Daniele - Commercialista e Revisore Contabile*

