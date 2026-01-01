-- Script per inizializzare il menu dinamico completo
-- Esegui con: sqlcmd -S .\SQLEXPRESS -d StudioCG -i Scripts\init_menu.sql

SET IDENTITY_INSERT VociMenu ON;

-- ============ DASHBOARD ============
INSERT INTO VociMenu (Id, Nome, Url, Icon, Categoria, DisplayOrder, IsVisible, IsActive, IsGroup, ExpandedByDefault, TipoVoce, ParentId, CreatedAt)
VALUES (1, 'Dashboard', '/Home', 'fas fa-home', NULL, 1, 1, 1, 0, 0, 'System', NULL, GETDATE());

-- ============ UTENTI (Solo Admin) ============
INSERT INTO VociMenu (Id, Nome, Url, Icon, Categoria, DisplayOrder, IsVisible, IsActive, IsGroup, ExpandedByDefault, TipoVoce, ParentId, CreatedAt)
VALUES (10, 'UTENTI', NULL, 'fas fa-users-cog', 'ADMIN', 10, 1, 1, 1, 0, 'System', NULL, GETDATE());

INSERT INTO VociMenu (Id, Nome, Url, Icon, Categoria, DisplayOrder, IsVisible, IsActive, IsGroup, ExpandedByDefault, TipoVoce, ParentId, CreatedAt)
VALUES (11, 'Gestione Utenti', '/Users', 'fas fa-users', 'ADMIN', 1, 1, 1, 0, 0, 'System', 10, GETDATE());

INSERT INTO VociMenu (Id, Nome, Url, Icon, Categoria, DisplayOrder, IsVisible, IsActive, IsGroup, ExpandedByDefault, TipoVoce, ParentId, CreatedAt)
VALUES (12, 'Gestione Pagine', '/Permissions/Pages', 'fas fa-key', 'ADMIN', 2, 1, 1, 0, 0, 'System', 10, GETDATE());

INSERT INTO VociMenu (Id, Nome, Url, Icon, Categoria, DisplayOrder, IsVisible, IsActive, IsGroup, ExpandedByDefault, TipoVoce, ParentId, CreatedAt)
VALUES (13, 'Gestione Menu', '/Menu', 'fas fa-bars', 'ADMIN', 3, 1, 1, 0, 0, 'System', 10, GETDATE());

-- ============ ANAGRAFICA ============
INSERT INTO VociMenu (Id, Nome, Url, Icon, Categoria, DisplayOrder, IsVisible, IsActive, IsGroup, ExpandedByDefault, TipoVoce, ParentId, CreatedAt)
VALUES (20, 'ANAGRAFICA', NULL, 'fas fa-address-book', 'ANAGRAFICA', 20, 1, 1, 1, 0, 'System', NULL, GETDATE());

INSERT INTO VociMenu (Id, Nome, Url, Icon, Categoria, DisplayOrder, IsVisible, IsActive, IsGroup, ExpandedByDefault, TipoVoce, ParentId, CreatedAt)
VALUES (21, 'Clienti', '/Clienti', 'fas fa-building', 'ANAGRAFICA', 1, 1, 1, 0, 0, 'System', 20, GETDATE());

INSERT INTO VociMenu (Id, Nome, Url, Icon, Categoria, DisplayOrder, IsVisible, IsActive, IsGroup, ExpandedByDefault, TipoVoce, ParentId, CreatedAt)
VALUES (22, 'Annualita Fiscali', '/Annualita', 'fas fa-calendar-alt', 'ANAGRAFICA', 2, 1, 1, 0, 0, 'System', 20, GETDATE());

INSERT INTO VociMenu (Id, Nome, Url, Icon, Categoria, DisplayOrder, IsVisible, IsActive, IsGroup, ExpandedByDefault, TipoVoce, ParentId, CreatedAt)
VALUES (23, 'Tipi Attivita', '/AttivitaTipi', 'fas fa-cogs', 'ANAGRAFICA', 3, 1, 1, 0, 0, 'System', 20, GETDATE());

-- ============ ATTIVITÀ (Dinamico - le voci si generano automaticamente) ============
INSERT INTO VociMenu (Id, Nome, Url, Icon, Categoria, DisplayOrder, IsVisible, IsActive, IsGroup, ExpandedByDefault, TipoVoce, ParentId, CreatedAt)
VALUES (30, 'ATTIVITA', NULL, 'fas fa-tasks', 'ATTIVITA', 30, 1, 1, 1, 0, 'DynamicAttivita', NULL, GETDATE());

INSERT INTO VociMenu (Id, Nome, Url, Icon, Categoria, DisplayOrder, IsVisible, IsActive, IsGroup, ExpandedByDefault, TipoVoce, ParentId, CreatedAt)
VALUES (31, 'Riepilogo', '/Attivita', 'fas fa-chart-pie', 'ATTIVITA', 1, 1, 1, 0, 0, 'System', 30, GETDATE());

-- ============ ENTITÀ (Dinamico - le voci si generano automaticamente) ============
INSERT INTO VociMenu (Id, Nome, Url, Icon, Categoria, DisplayOrder, IsVisible, IsActive, IsGroup, ExpandedByDefault, TipoVoce, ParentId, CreatedAt)
VALUES (40, 'ENTITA', NULL, 'fas fa-cubes', 'ENTITA', 40, 1, 1, 1, 0, 'DynamicEntita', NULL, GETDATE());

INSERT INTO VociMenu (Id, Nome, Url, Icon, Categoria, DisplayOrder, IsVisible, IsActive, IsGroup, ExpandedByDefault, TipoVoce, ParentId, CreatedAt)
VALUES (41, 'Gestione Entita', '/Entita/Gestione', 'fas fa-cogs', 'ENTITA', 1, 1, 1, 0, 0, 'System', 40, GETDATE());

-- ============ DATI UTENZA (Dinamico) ============
INSERT INTO VociMenu (Id, Nome, Url, Icon, Categoria, DisplayOrder, IsVisible, IsActive, IsGroup, ExpandedByDefault, TipoVoce, ParentId, CreatedAt)
VALUES (50, 'DATI UTENZA', NULL, 'fas fa-database', 'DATI_UTENZA', 50, 1, 1, 1, 0, 'DynamicDatiUtenza', NULL, GETDATE());

INSERT INTO VociMenu (Id, Nome, Url, Icon, Categoria, DisplayOrder, IsVisible, IsActive, IsGroup, ExpandedByDefault, TipoVoce, ParentId, CreatedAt)
VALUES (51, 'Gestione Pagine Dati', '/DynamicPages', 'fas fa-cogs', 'DATI_UTENZA', 1, 1, 1, 0, 0, 'System', 50, GETDATE());

-- ============ BUDGET STUDIO ============
INSERT INTO VociMenu (Id, Nome, Url, Icon, Categoria, DisplayOrder, IsVisible, IsActive, IsGroup, ExpandedByDefault, TipoVoce, ParentId, CreatedAt)
VALUES (60, 'Budget Studio', '/BudgetStudio', 'fas fa-coins', NULL, 60, 1, 1, 0, 0, 'System', NULL, GETDATE());

-- ============ DOCUMENTI ============
INSERT INTO VociMenu (Id, Nome, Url, Icon, Categoria, DisplayOrder, IsVisible, IsActive, IsGroup, ExpandedByDefault, TipoVoce, ParentId, CreatedAt)
VALUES (70, 'DOCUMENTI', NULL, 'fas fa-file-alt', 'DOCUMENTI', 70, 1, 1, 1, 0, 'System', NULL, GETDATE());

INSERT INTO VociMenu (Id, Nome, Url, Icon, Categoria, DisplayOrder, IsVisible, IsActive, IsGroup, ExpandedByDefault, TipoVoce, ParentId, CreatedAt)
VALUES (71, 'Dashboard', '/Documenti', 'fas fa-tachometer-alt', 'DOCUMENTI', 1, 1, 1, 0, 0, 'System', 70, GETDATE());

INSERT INTO VociMenu (Id, Nome, Url, Icon, Categoria, DisplayOrder, IsVisible, IsActive, IsGroup, ExpandedByDefault, TipoVoce, ParentId, CreatedAt)
VALUES (72, 'Impostazioni Studio', '/Documenti/ImpostazioniStudio', 'fas fa-building', 'DOCUMENTI', 2, 1, 1, 0, 0, 'System', 70, GETDATE());

INSERT INTO VociMenu (Id, Nome, Url, Icon, Categoria, DisplayOrder, IsVisible, IsActive, IsGroup, ExpandedByDefault, TipoVoce, ParentId, CreatedAt)
VALUES (73, 'Clausole', '/Documenti/Clausole', 'fas fa-paragraph', 'DOCUMENTI', 3, 1, 1, 0, 0, 'System', 70, GETDATE());

INSERT INTO VociMenu (Id, Nome, Url, Icon, Categoria, DisplayOrder, IsVisible, IsActive, IsGroup, ExpandedByDefault, TipoVoce, ParentId, CreatedAt)
VALUES (74, 'Template', '/Documenti/Template', 'fas fa-file-signature', 'DOCUMENTI', 4, 1, 1, 0, 0, 'System', 70, GETDATE());

INSERT INTO VociMenu (Id, Nome, Url, Icon, Categoria, DisplayOrder, IsVisible, IsActive, IsGroup, ExpandedByDefault, TipoVoce, ParentId, CreatedAt)
VALUES (75, 'Genera Documento', '/Documenti/Genera', 'fas fa-file-export', 'DOCUMENTI', 5, 1, 1, 0, 0, 'System', 70, GETDATE());

INSERT INTO VociMenu (Id, Nome, Url, Icon, Categoria, DisplayOrder, IsVisible, IsActive, IsGroup, ExpandedByDefault, TipoVoce, ParentId, CreatedAt)
VALUES (76, 'Archivio', '/Documenti/Archivio', 'fas fa-archive', 'DOCUMENTI', 6, 1, 1, 0, 0, 'System', 70, GETDATE());

-- ============ AMMINISTRAZIONE ============
INSERT INTO VociMenu (Id, Nome, Url, Icon, Categoria, DisplayOrder, IsVisible, IsActive, IsGroup, ExpandedByDefault, TipoVoce, ParentId, CreatedAt)
VALUES (80, 'AMMINISTRAZIONE', NULL, 'fas fa-file-invoice-dollar', 'AMMINISTRAZIONE', 80, 1, 1, 1, 0, 'System', NULL, GETDATE());

INSERT INTO VociMenu (Id, Nome, Url, Icon, Categoria, DisplayOrder, IsVisible, IsActive, IsGroup, ExpandedByDefault, TipoVoce, ParentId, CreatedAt)
VALUES (81, 'Dashboard', '/Amministrazione', 'fas fa-chart-line', 'AMMINISTRAZIONE', 1, 1, 1, 0, 0, 'System', 80, GETDATE());

INSERT INTO VociMenu (Id, Nome, Url, Icon, Categoria, DisplayOrder, IsVisible, IsActive, IsGroup, ExpandedByDefault, TipoVoce, ParentId, CreatedAt)
VALUES (82, 'Gestione Anni', '/Amministrazione/GestioneAnni', 'fas fa-calendar-alt', 'AMMINISTRAZIONE', 2, 1, 1, 0, 0, 'System', 80, GETDATE());

INSERT INTO VociMenu (Id, Nome, Url, Icon, Categoria, DisplayOrder, IsVisible, IsActive, IsGroup, ExpandedByDefault, TipoVoce, ParentId, CreatedAt)
VALUES (83, 'Mandati Clienti', '/Amministrazione/Mandati', 'fas fa-file-contract', 'AMMINISTRAZIONE', 3, 1, 1, 0, 0, 'System', 80, GETDATE());

INSERT INTO VociMenu (Id, Nome, Url, Icon, Categoria, DisplayOrder, IsVisible, IsActive, IsGroup, ExpandedByDefault, TipoVoce, ParentId, CreatedAt)
VALUES (84, 'Fatturazione', '/Amministrazione/Scadenze', 'fas fa-file-invoice-dollar', 'AMMINISTRAZIONE', 4, 1, 1, 0, 0, 'System', 80, GETDATE());

INSERT INTO VociMenu (Id, Nome, Url, Icon, Categoria, DisplayOrder, IsVisible, IsActive, IsGroup, ExpandedByDefault, TipoVoce, ParentId, CreatedAt)
VALUES (85, 'Spese Pratiche', '/Amministrazione/SpesePratiche', 'fas fa-receipt', 'AMMINISTRAZIONE', 5, 1, 1, 0, 0, 'System', 80, GETDATE());

INSERT INTO VociMenu (Id, Nome, Url, Icon, Categoria, DisplayOrder, IsVisible, IsActive, IsGroup, ExpandedByDefault, TipoVoce, ParentId, CreatedAt)
VALUES (86, 'Accessi Clienti', '/Amministrazione/AccessiClienti', 'fas fa-door-open', 'AMMINISTRAZIONE', 6, 1, 1, 0, 0, 'System', 80, GETDATE());

INSERT INTO VociMenu (Id, Nome, Url, Icon, Categoria, DisplayOrder, IsVisible, IsActive, IsGroup, ExpandedByDefault, TipoVoce, ParentId, CreatedAt)
VALUES (87, 'Fatture in Cloud', '/Amministrazione/FattureCloud', 'fas fa-cloud', 'AMMINISTRAZIONE', 7, 1, 1, 0, 0, 'System', 80, GETDATE());

INSERT INTO VociMenu (Id, Nome, Url, Icon, Categoria, DisplayOrder, IsVisible, IsActive, IsGroup, ExpandedByDefault, TipoVoce, ParentId, CreatedAt)
VALUES (88, 'Bilanci CEE', '/Amministrazione/BilanciCEE', 'fas fa-balance-scale', 'AMMINISTRAZIONE', 8, 1, 1, 0, 0, 'System', 80, GETDATE());

INSERT INTO VociMenu (Id, Nome, Url, Icon, Categoria, DisplayOrder, IsVisible, IsActive, IsGroup, ExpandedByDefault, TipoVoce, ParentId, CreatedAt)
VALUES (89, 'Incassi', '/Amministrazione/Incassi', 'fas fa-money-bill-wave', 'AMMINISTRAZIONE', 9, 1, 1, 0, 0, 'System', 80, GETDATE());

INSERT INTO VociMenu (Id, Nome, Url, Icon, Categoria, DisplayOrder, IsVisible, IsActive, IsGroup, ExpandedByDefault, TipoVoce, ParentId, CreatedAt)
VALUES (90, 'Report Professionisti', '/Amministrazione/ReportProfessionisti', 'fas fa-user-tie', 'AMMINISTRAZIONE', 10, 1, 1, 0, 0, 'System', 80, GETDATE());

SET IDENTITY_INSERT VociMenu OFF;

PRINT 'Menu inizializzato con successo!';
