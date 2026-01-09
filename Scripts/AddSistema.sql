-- ============================================================
-- Script per aggiungere la pagina SISTEMA con backup database
-- Eseguire sul server di produzione srv-dc
-- Data: 09/01/2026
-- ============================================================

-- Verifica se il permesso esiste già
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Id = 401)
BEGIN
    -- Aggiungi permesso Sistema
    SET IDENTITY_INSERT Permissions ON;
    
    INSERT INTO Permissions (Id, PageName, PageUrl, [Description], Icon, DisplayOrder, ShowInMenu, Category)
    VALUES (401, 'Sistema', '/Sistema', 'Backup database e funzioni di sistema', 'fas fa-server', 51, 1, 'ADMIN');
    
    SET IDENTITY_INSERT Permissions OFF;
    
    PRINT 'Permesso Sistema aggiunto con successo (Id=401)';
END
ELSE
BEGIN
    PRINT 'Permesso Sistema già esistente (Id=401)';
END

-- Verifica se esiste il gruppo UTENTI (Id=10)
IF NOT EXISTS (SELECT 1 FROM VociMenu WHERE Id = 10)
BEGIN
    -- Prima crea il gruppo UTENTI
    SET IDENTITY_INSERT VociMenu ON;
    
    INSERT INTO VociMenu (Id, Nome, Url, Icon, Categoria, DisplayOrder, IsVisible, IsActive, IsGroup, ExpandedByDefault, TipoVoce, ParentId, CreatedAt)
    VALUES (10, 'UTENTI', NULL, 'fas fa-users-cog', 'ADMIN', 10, 1, 1, 1, 0, 'System', NULL, GETDATE());
    
    -- Aggiungi anche le altre voci del menu UTENTI
    INSERT INTO VociMenu (Id, Nome, Url, Icon, Categoria, DisplayOrder, IsVisible, IsActive, IsGroup, ExpandedByDefault, TipoVoce, ParentId, CreatedAt)
    VALUES (11, 'Gestione Utenti', '/Users', 'fas fa-users', 'ADMIN', 1, 1, 1, 0, 0, 'System', 10, GETDATE());
    
    INSERT INTO VociMenu (Id, Nome, Url, Icon, Categoria, DisplayOrder, IsVisible, IsActive, IsGroup, ExpandedByDefault, TipoVoce, ParentId, CreatedAt)
    VALUES (12, 'Gestione Pagine', '/Permissions/Pages', 'fas fa-key', 'ADMIN', 2, 1, 1, 0, 0, 'System', 10, GETDATE());
    
    INSERT INTO VociMenu (Id, Nome, Url, Icon, Categoria, DisplayOrder, IsVisible, IsActive, IsGroup, ExpandedByDefault, TipoVoce, ParentId, CreatedAt)
    VALUES (13, 'Gestione Menu', '/Menu', 'fas fa-bars', 'ADMIN', 3, 1, 1, 0, 0, 'System', 10, GETDATE());
    
    SET IDENTITY_INSERT VociMenu OFF;
    
    PRINT 'Gruppo UTENTI e voci base create (Id=10,11,12,13)';
END

-- Verifica se la voce menu Sistema esiste già
IF NOT EXISTS (SELECT 1 FROM VociMenu WHERE Id = 14)
BEGIN
    -- Aggiungi voce menu Sistema sotto UTENTI (ParentId = 10)
    SET IDENTITY_INSERT VociMenu ON;
    
    INSERT INTO VociMenu (Id, Nome, Url, Icon, Categoria, DisplayOrder, IsVisible, IsActive, IsGroup, ExpandedByDefault, TipoVoce, ParentId, CreatedAt)
    VALUES (14, 'Sistema', '/Sistema', 'fas fa-server', 'ADMIN', 4, 1, 1, 0, 0, 'System', 10, GETDATE());
    
    SET IDENTITY_INSERT VociMenu OFF;
    
    PRINT 'Voce menu Sistema aggiunta con successo (Id=14)';
END
ELSE
BEGIN
    PRINT 'Voce menu Sistema già esistente (Id=14)';
END

-- Verifica finale
SELECT 'Permessi Sistema:' AS Info;
SELECT Id, PageName, PageUrl, Category FROM Permissions WHERE PageUrl = '/Sistema';

SELECT 'Voci Menu UTENTI:' AS Info;
SELECT Id, Nome, Url, ParentId, DisplayOrder FROM VociMenu WHERE ParentId = 10 OR Id = 10 ORDER BY ISNULL(ParentId, 0), DisplayOrder;

PRINT '';
PRINT '============================================================';
PRINT 'Script completato!';
PRINT 'La pagina Sistema sarà visibile nel menu UTENTI.';
PRINT 'Ricorda di assegnare il permesso agli utenti che devono accedere.';
PRINT '============================================================';
