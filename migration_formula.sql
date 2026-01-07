IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251212120908_InitialCreate'
)
BEGIN
    CREATE TABLE [Permissions] (
        [Id] int NOT NULL IDENTITY,
        [PageName] nvarchar(100) NOT NULL,
        [PageUrl] nvarchar(255) NOT NULL,
        [Description] nvarchar(500) NULL,
        [Icon] nvarchar(100) NULL,
        [DisplayOrder] int NOT NULL,
        [ShowInMenu] bit NOT NULL,
        CONSTRAINT [PK_Permissions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251212120908_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] int NOT NULL IDENTITY,
        [Username] nvarchar(50) NOT NULL,
        [PasswordHash] nvarchar(255) NOT NULL,
        [Nome] nvarchar(100) NOT NULL,
        [Cognome] nvarchar(100) NOT NULL,
        [Email] nvarchar(255) NULL,
        [IsActive] bit NOT NULL,
        [IsAdmin] bit NOT NULL,
        [DataCreazione] datetime2 NOT NULL,
        [UltimoAccesso] datetime2 NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251212120908_InitialCreate'
)
BEGIN
    CREATE TABLE [UserPermissions] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [PermissionId] int NOT NULL,
        [CanView] bit NOT NULL,
        [CanEdit] bit NOT NULL,
        [CanDelete] bit NOT NULL,
        [CanCreate] bit NOT NULL,
        CONSTRAINT [PK_UserPermissions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserPermissions_Permissions_PermissionId] FOREIGN KEY ([PermissionId]) REFERENCES [Permissions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserPermissions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251212120908_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayOrder', N'Icon', N'PageName', N'PageUrl', N'ShowInMenu') AND [object_id] = OBJECT_ID(N'[Permissions]'))
        SET IDENTITY_INSERT [Permissions] ON;
    EXEC(N'INSERT INTO [Permissions] ([Id], [Description], [DisplayOrder], [Icon], [PageName], [PageUrl], [ShowInMenu])
    VALUES (1, N''Pagina principale'', 1, N''fas fa-home'', N''Dashboard'', N''/Home'', CAST(1 AS bit)),
    (2, N''Gestione utenti del sistema'', 2, N''fas fa-users'', N''Gestione Utenti'', N''/Users'', CAST(1 AS bit))');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'DisplayOrder', N'Icon', N'PageName', N'PageUrl', N'ShowInMenu') AND [object_id] = OBJECT_ID(N'[Permissions]'))
        SET IDENTITY_INSERT [Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251212120908_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Cognome', N'DataCreazione', N'Email', N'IsActive', N'IsAdmin', N'Nome', N'PasswordHash', N'UltimoAccesso', N'Username') AND [object_id] = OBJECT_ID(N'[Users]'))
        SET IDENTITY_INSERT [Users] ON;
    EXEC(N'INSERT INTO [Users] ([Id], [Cognome], [DataCreazione], [Email], [IsActive], [IsAdmin], [Nome], [PasswordHash], [UltimoAccesso], [Username])
    VALUES (1, N''Sistema'', ''2024-01-01T00:00:00.0000000'', N''admin@studiocg.it'', CAST(1 AS bit), CAST(1 AS bit), N''Amministratore'', N''8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92'', NULL, N''admin'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Cognome', N'DataCreazione', N'Email', N'IsActive', N'IsAdmin', N'Nome', N'PasswordHash', N'UltimoAccesso', N'Username') AND [object_id] = OBJECT_ID(N'[Users]'))
        SET IDENTITY_INSERT [Users] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251212120908_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Permissions_PageUrl] ON [Permissions] ([PageUrl]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251212120908_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UserPermissions_PermissionId] ON [UserPermissions] ([PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251212120908_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserPermissions_UserId_PermissionId] ON [UserPermissions] ([UserId], [PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251212120908_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Username] ON [Users] ([Username]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251212120908_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251212120908_InitialCreate', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251212131857_AddCategoryToPermission'
)
BEGIN
    ALTER TABLE [Permissions] ADD [Category] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251212131857_AddCategoryToPermission'
)
BEGIN
    EXEC(N'UPDATE [Permissions] SET [Category] = NULL
    WHERE [Id] = 1;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251212131857_AddCategoryToPermission'
)
BEGIN
    EXEC(N'UPDATE [Permissions] SET [Category] = NULL
    WHERE [Id] = 2;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251212131857_AddCategoryToPermission'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251212131857_AddCategoryToPermission', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251212151054_AddDynamicPagesTables'
)
BEGIN
    CREATE TABLE [DynamicPages] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [Category] nvarchar(50) NOT NULL,
        [Icon] nvarchar(100) NOT NULL,
        [DisplayOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [TableName] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_DynamicPages] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251212151054_AddDynamicPagesTables'
)
BEGIN
    CREATE TABLE [DynamicFields] (
        [Id] int NOT NULL IDENTITY,
        [DynamicPageId] int NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Label] nvarchar(100) NOT NULL,
        [FieldType] nvarchar(50) NOT NULL,
        [IsRequired] bit NOT NULL,
        [ShowInList] bit NOT NULL,
        [DisplayOrder] int NOT NULL,
        [DefaultValue] nvarchar(500) NULL,
        [Placeholder] nvarchar(200) NULL,
        CONSTRAINT [PK_DynamicFields] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DynamicFields_DynamicPages_DynamicPageId] FOREIGN KEY ([DynamicPageId]) REFERENCES [DynamicPages] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251212151054_AddDynamicPagesTables'
)
BEGIN
    CREATE TABLE [DynamicRecords] (
        [Id] int NOT NULL IDENTITY,
        [DynamicPageId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [CreatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_DynamicRecords] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DynamicRecords_DynamicPages_DynamicPageId] FOREIGN KEY ([DynamicPageId]) REFERENCES [DynamicPages] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251212151054_AddDynamicPagesTables'
)
BEGIN
    CREATE TABLE [DynamicFieldValues] (
        [Id] int NOT NULL IDENTITY,
        [DynamicRecordId] int NOT NULL,
        [DynamicFieldId] int NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_DynamicFieldValues] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DynamicFieldValues_DynamicFields_DynamicFieldId] FOREIGN KEY ([DynamicFieldId]) REFERENCES [DynamicFields] ([Id]),
        CONSTRAINT [FK_DynamicFieldValues_DynamicRecords_DynamicRecordId] FOREIGN KEY ([DynamicRecordId]) REFERENCES [DynamicRecords] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251212151054_AddDynamicPagesTables'
)
BEGIN
    CREATE INDEX [IX_DynamicFields_DynamicPageId] ON [DynamicFields] ([DynamicPageId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251212151054_AddDynamicPagesTables'
)
BEGIN
    CREATE INDEX [IX_DynamicFieldValues_DynamicFieldId] ON [DynamicFieldValues] ([DynamicFieldId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251212151054_AddDynamicPagesTables'
)
BEGIN
    CREATE UNIQUE INDEX [IX_DynamicFieldValues_DynamicRecordId_DynamicFieldId] ON [DynamicFieldValues] ([DynamicRecordId], [DynamicFieldId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251212151054_AddDynamicPagesTables'
)
BEGIN
    CREATE UNIQUE INDEX [IX_DynamicPages_TableName] ON [DynamicPages] ([TableName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251212151054_AddDynamicPagesTables'
)
BEGIN
    CREATE INDEX [IX_DynamicRecords_DynamicPageId] ON [DynamicRecords] ([DynamicPageId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251212151054_AddDynamicPagesTables'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251212151054_AddDynamicPagesTables', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251215120735_AddAnagraficaAttivitaTables'
)
BEGIN
    CREATE TABLE [AnnualitaFiscali] (
        [Id] int NOT NULL IDENTITY,
        [Anno] int NOT NULL,
        [Descrizione] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [IsCurrent] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_AnnualitaFiscali] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251215120735_AddAnagraficaAttivitaTables'
)
BEGIN
    CREATE TABLE [AttivitaTipi] (
        [Id] int NOT NULL IDENTITY,
        [Nome] nvarchar(100) NOT NULL,
        [Descrizione] nvarchar(500) NULL,
        [Icon] nvarchar(50) NULL,
        [DisplayOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_AttivitaTipi] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251215120735_AddAnagraficaAttivitaTables'
)
BEGIN
    CREATE TABLE [Clienti] (
        [Id] int NOT NULL IDENTITY,
        [RagioneSociale] nvarchar(200) NOT NULL,
        [Indirizzo] nvarchar(200) NULL,
        [Citta] nvarchar(100) NULL,
        [Provincia] nvarchar(2) NULL,
        [CAP] nvarchar(5) NULL,
        [Email] nvarchar(255) NULL,
        [PEC] nvarchar(50) NULL,
        [Telefono] nvarchar(20) NULL,
        [CodiceFiscale] nvarchar(16) NULL,
        [PartitaIVA] nvarchar(11) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [Note] nvarchar(max) NULL,
        CONSTRAINT [PK_Clienti] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251215120735_AddAnagraficaAttivitaTables'
)
BEGIN
    CREATE TABLE [AttivitaAnnuali] (
        [Id] int NOT NULL IDENTITY,
        [AttivitaTipoId] int NOT NULL,
        [AnnualitaFiscaleId] int NOT NULL,
        [IsActive] bit NOT NULL,
        [DataScadenza] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_AttivitaAnnuali] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AttivitaAnnuali_AnnualitaFiscali_AnnualitaFiscaleId] FOREIGN KEY ([AnnualitaFiscaleId]) REFERENCES [AnnualitaFiscali] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AttivitaAnnuali_AttivitaTipi_AttivitaTipoId] FOREIGN KEY ([AttivitaTipoId]) REFERENCES [AttivitaTipi] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251215120735_AddAnagraficaAttivitaTables'
)
BEGIN
    CREATE TABLE [AttivitaCampi] (
        [Id] int NOT NULL IDENTITY,
        [AttivitaTipoId] int NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Label] nvarchar(100) NOT NULL,
        [FieldType] int NOT NULL,
        [IsRequired] bit NOT NULL,
        [ShowInList] bit NOT NULL,
        [DisplayOrder] int NOT NULL,
        CONSTRAINT [PK_AttivitaCampi] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AttivitaCampi_AttivitaTipi_AttivitaTipoId] FOREIGN KEY ([AttivitaTipoId]) REFERENCES [AttivitaTipi] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251215120735_AddAnagraficaAttivitaTables'
)
BEGIN
    CREATE TABLE [ClientiSoggetti] (
        [Id] int NOT NULL IDENTITY,
        [ClienteId] int NOT NULL,
        [TipoSoggetto] int NOT NULL,
        [Nome] nvarchar(100) NOT NULL,
        [Cognome] nvarchar(100) NOT NULL,
        [CodiceFiscale] nvarchar(16) NULL,
        [Indirizzo] nvarchar(200) NULL,
        [Citta] nvarchar(100) NULL,
        [Provincia] nvarchar(2) NULL,
        [CAP] nvarchar(5) NULL,
        [Email] nvarchar(255) NULL,
        [Telefono] nvarchar(20) NULL,
        [QuotaPercentuale] decimal(5,2) NULL,
        [DisplayOrder] int NOT NULL,
        CONSTRAINT [PK_ClientiSoggetti] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ClientiSoggetti_Clienti_ClienteId] FOREIGN KEY ([ClienteId]) REFERENCES [Clienti] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251215120735_AddAnagraficaAttivitaTables'
)
BEGIN
    CREATE TABLE [ClientiAttivita] (
        [Id] int NOT NULL IDENTITY,
        [ClienteId] int NOT NULL,
        [AttivitaAnnualeId] int NOT NULL,
        [Stato] int NOT NULL,
        [DataCompletamento] datetime2 NULL,
        [Note] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_ClientiAttivita] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ClientiAttivita_AttivitaAnnuali_AttivitaAnnualeId] FOREIGN KEY ([AttivitaAnnualeId]) REFERENCES [AttivitaAnnuali] ([Id]),
        CONSTRAINT [FK_ClientiAttivita_Clienti_ClienteId] FOREIGN KEY ([ClienteId]) REFERENCES [Clienti] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251215120735_AddAnagraficaAttivitaTables'
)
BEGIN
    CREATE TABLE [ClientiAttivitaValori] (
        [Id] int NOT NULL IDENTITY,
        [ClienteAttivitaId] int NOT NULL,
        [AttivitaCampoId] int NOT NULL,
        [Valore] nvarchar(max) NULL,
        CONSTRAINT [PK_ClientiAttivitaValori] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ClientiAttivitaValori_AttivitaCampi_AttivitaCampoId] FOREIGN KEY ([AttivitaCampoId]) REFERENCES [AttivitaCampi] ([Id]),
        CONSTRAINT [FK_ClientiAttivitaValori_ClientiAttivita_ClienteAttivitaId] FOREIGN KEY ([ClienteAttivitaId]) REFERENCES [ClientiAttivita] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251215120735_AddAnagraficaAttivitaTables'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Anno', N'CreatedAt', N'Descrizione', N'IsActive', N'IsCurrent') AND [object_id] = OBJECT_ID(N'[AnnualitaFiscali]'))
        SET IDENTITY_INSERT [AnnualitaFiscali] ON;
    EXEC(N'INSERT INTO [AnnualitaFiscali] ([Id], [Anno], [CreatedAt], [Descrizione], [IsActive], [IsCurrent])
    VALUES (1, 2025, ''2025-01-01T00:00:00.0000000'', N''Anno Fiscale 2025'', CAST(1 AS bit), CAST(1 AS bit))');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Anno', N'CreatedAt', N'Descrizione', N'IsActive', N'IsCurrent') AND [object_id] = OBJECT_ID(N'[AnnualitaFiscali]'))
        SET IDENTITY_INSERT [AnnualitaFiscali] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251215120735_AddAnagraficaAttivitaTables'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'Description', N'DisplayOrder', N'Icon', N'PageName', N'PageUrl', N'ShowInMenu') AND [object_id] = OBJECT_ID(N'[Permissions]'))
        SET IDENTITY_INSERT [Permissions] ON;
    EXEC(N'INSERT INTO [Permissions] ([Id], [Category], [Description], [DisplayOrder], [Icon], [PageName], [PageUrl], [ShowInMenu])
    VALUES (100, N''ANAGRAFICA'', N''Gestione anagrafica clienti'', 10, N''fas fa-building'', N''Clienti'', N''/Clienti'', CAST(1 AS bit)),
    (101, N''ANAGRAFICA'', N''Gestione annualità fiscali'', 11, N''fas fa-calendar-alt'', N''Annualità Fiscali'', N''/Annualita'', CAST(1 AS bit)),
    (102, N''ANAGRAFICA'', N''Gestione tipi attività'', 12, N''fas fa-cogs'', N''Tipi Attività'', N''/AttivitaTipi'', CAST(1 AS bit))');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'Description', N'DisplayOrder', N'Icon', N'PageName', N'PageUrl', N'ShowInMenu') AND [object_id] = OBJECT_ID(N'[Permissions]'))
        SET IDENTITY_INSERT [Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251215120735_AddAnagraficaAttivitaTables'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AnnualitaFiscali_Anno] ON [AnnualitaFiscali] ([Anno]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251215120735_AddAnagraficaAttivitaTables'
)
BEGIN
    CREATE INDEX [IX_AttivitaAnnuali_AnnualitaFiscaleId] ON [AttivitaAnnuali] ([AnnualitaFiscaleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251215120735_AddAnagraficaAttivitaTables'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AttivitaAnnuali_AttivitaTipoId_AnnualitaFiscaleId] ON [AttivitaAnnuali] ([AttivitaTipoId], [AnnualitaFiscaleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251215120735_AddAnagraficaAttivitaTables'
)
BEGIN
    CREATE INDEX [IX_AttivitaCampi_AttivitaTipoId] ON [AttivitaCampi] ([AttivitaTipoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251215120735_AddAnagraficaAttivitaTables'
)
BEGIN
    CREATE INDEX [IX_ClientiAttivita_AttivitaAnnualeId] ON [ClientiAttivita] ([AttivitaAnnualeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251215120735_AddAnagraficaAttivitaTables'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ClientiAttivita_ClienteId_AttivitaAnnualeId] ON [ClientiAttivita] ([ClienteId], [AttivitaAnnualeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251215120735_AddAnagraficaAttivitaTables'
)
BEGIN
    CREATE INDEX [IX_ClientiAttivitaValori_AttivitaCampoId] ON [ClientiAttivitaValori] ([AttivitaCampoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251215120735_AddAnagraficaAttivitaTables'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ClientiAttivitaValori_ClienteAttivitaId_AttivitaCampoId] ON [ClientiAttivitaValori] ([ClienteAttivitaId], [AttivitaCampoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251215120735_AddAnagraficaAttivitaTables'
)
BEGIN
    CREATE INDEX [IX_ClientiSoggetti_ClienteId] ON [ClientiSoggetti] ([ClienteId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251215120735_AddAnagraficaAttivitaTables'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251215120735_AddAnagraficaAttivitaTables', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251215133618_FixQuotaDecimalPrecision'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ClientiSoggetti]') AND [c].[name] = N'QuotaPercentuale');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [ClientiSoggetti] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [ClientiSoggetti] ALTER COLUMN [QuotaPercentuale] decimal(18,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251215133618_FixQuotaDecimalPrecision'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251215133618_FixQuotaDecimalPrecision', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251215135252_AddCodiceAtecoToCliente'
)
BEGIN
    ALTER TABLE [Clienti] ADD [CodiceAteco] nvarchar(10) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251215135252_AddCodiceAtecoToCliente'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251215135252_AddCodiceAtecoToCliente', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216071717_AddTipoSoggettoToCliente'
)
BEGIN
    ALTER TABLE [Clienti] ADD [TipoSoggetto] nvarchar(10) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216071717_AddTipoSoggettoToCliente'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251216071717_AddTipoSoggettoToCliente', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216143032_AddFatturazioneTables'
)
BEGIN
    CREATE TABLE [ContatoriDocumenti] (
        [Id] int NOT NULL IDENTITY,
        [Anno] int NOT NULL,
        [TipoDocumento] int NOT NULL,
        [UltimoNumero] int NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ContatoriDocumenti] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216143032_AddFatturazioneTables'
)
BEGIN
    CREATE TABLE [MandatiClienti] (
        [Id] int NOT NULL IDENTITY,
        [ClienteId] int NOT NULL,
        [Anno] int NOT NULL,
        [ImportoAnnuo] decimal(18,2) NOT NULL,
        [TipoScadenza] int NOT NULL,
        [IsActive] bit NOT NULL,
        [Note] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_MandatiClienti] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MandatiClienti_Clienti_ClienteId] FOREIGN KEY ([ClienteId]) REFERENCES [Clienti] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216143032_AddFatturazioneTables'
)
BEGIN
    CREATE TABLE [ScadenzeFatturazione] (
        [Id] int NOT NULL IDENTITY,
        [MandatoClienteId] int NULL,
        [ClienteId] int NOT NULL,
        [Anno] int NOT NULL,
        [DataScadenza] datetime2 NOT NULL,
        [ImportoMandato] decimal(18,2) NOT NULL,
        [NumeroProforma] int NULL,
        [DataProforma] datetime2 NULL,
        [NumeroFattura] int NULL,
        [DataFattura] datetime2 NULL,
        [Stato] int NOT NULL,
        [StatoIncasso] int NOT NULL,
        [Note] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_ScadenzeFatturazione] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ScadenzeFatturazione_Clienti_ClienteId] FOREIGN KEY ([ClienteId]) REFERENCES [Clienti] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ScadenzeFatturazione_MandatiClienti_MandatoClienteId] FOREIGN KEY ([MandatoClienteId]) REFERENCES [MandatiClienti] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216143032_AddFatturazioneTables'
)
BEGIN
    CREATE TABLE [AccessiClienti] (
        [Id] int NOT NULL IDENTITY,
        [ClienteId] int NOT NULL,
        [ScadenzaFatturazioneId] int NOT NULL,
        [UtenteId] int NULL,
        [Data] datetime2 NOT NULL,
        [OraInizioMattino] time NULL,
        [OraFineMattino] time NULL,
        [OraInizioPomeriggio] time NULL,
        [OraFinePomeriggio] time NULL,
        [TariffaOraria] decimal(18,2) NOT NULL,
        [Note] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_AccessiClienti] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AccessiClienti_Clienti_ClienteId] FOREIGN KEY ([ClienteId]) REFERENCES [Clienti] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AccessiClienti_ScadenzeFatturazione_ScadenzaFatturazioneId] FOREIGN KEY ([ScadenzaFatturazioneId]) REFERENCES [ScadenzeFatturazione] ([Id]),
        CONSTRAINT [FK_AccessiClienti_Users_UtenteId] FOREIGN KEY ([UtenteId]) REFERENCES [Users] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216143032_AddFatturazioneTables'
)
BEGIN
    CREATE TABLE [BilanciCEE] (
        [Id] int NOT NULL IDENTITY,
        [ClienteId] int NOT NULL,
        [Anno] int NOT NULL,
        [Importo] decimal(18,2) NOT NULL,
        [DataScadenza] datetime2 NOT NULL,
        [ScadenzaFatturazioneId] int NULL,
        [Note] nvarchar(200) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_BilanciCEE] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BilanciCEE_Clienti_ClienteId] FOREIGN KEY ([ClienteId]) REFERENCES [Clienti] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_BilanciCEE_ScadenzeFatturazione_ScadenzaFatturazioneId] FOREIGN KEY ([ScadenzaFatturazioneId]) REFERENCES [ScadenzeFatturazione] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216143032_AddFatturazioneTables'
)
BEGIN
    CREATE TABLE [FattureCloud] (
        [Id] int NOT NULL IDENTITY,
        [ClienteId] int NOT NULL,
        [Anno] int NOT NULL,
        [Importo] decimal(18,2) NOT NULL,
        [DataScadenza] datetime2 NOT NULL,
        [ScadenzaFatturazioneId] int NULL,
        [Note] nvarchar(200) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_FattureCloud] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FattureCloud_Clienti_ClienteId] FOREIGN KEY ([ClienteId]) REFERENCES [Clienti] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_FattureCloud_ScadenzeFatturazione_ScadenzaFatturazioneId] FOREIGN KEY ([ScadenzaFatturazioneId]) REFERENCES [ScadenzeFatturazione] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216143032_AddFatturazioneTables'
)
BEGIN
    CREATE TABLE [IncassiFatture] (
        [Id] int NOT NULL IDENTITY,
        [ScadenzaFatturazioneId] int NOT NULL,
        [DataIncasso] datetime2 NOT NULL,
        [ImportoIncassato] decimal(18,2) NOT NULL,
        [Note] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_IncassiFatture] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_IncassiFatture_ScadenzeFatturazione_ScadenzaFatturazioneId] FOREIGN KEY ([ScadenzaFatturazioneId]) REFERENCES [ScadenzeFatturazione] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216143032_AddFatturazioneTables'
)
BEGIN
    CREATE TABLE [SpesePratiche] (
        [Id] int NOT NULL IDENTITY,
        [ClienteId] int NOT NULL,
        [ScadenzaFatturazioneId] int NOT NULL,
        [UtenteId] int NULL,
        [Descrizione] nvarchar(200) NOT NULL,
        [Importo] decimal(18,2) NOT NULL,
        [Data] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_SpesePratiche] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SpesePratiche_Clienti_ClienteId] FOREIGN KEY ([ClienteId]) REFERENCES [Clienti] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_SpesePratiche_ScadenzeFatturazione_ScadenzaFatturazioneId] FOREIGN KEY ([ScadenzaFatturazioneId]) REFERENCES [ScadenzeFatturazione] ([Id]),
        CONSTRAINT [FK_SpesePratiche_Users_UtenteId] FOREIGN KEY ([UtenteId]) REFERENCES [Users] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216143032_AddFatturazioneTables'
)
BEGIN
    CREATE TABLE [IncassiProfessionisti] (
        [Id] int NOT NULL IDENTITY,
        [IncassoFatturaId] int NOT NULL,
        [UtenteId] int NOT NULL,
        [Percentuale] decimal(5,2) NOT NULL,
        [Importo] decimal(18,2) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_IncassiProfessionisti] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_IncassiProfessionisti_IncassiFatture_IncassoFatturaId] FOREIGN KEY ([IncassoFatturaId]) REFERENCES [IncassiFatture] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_IncassiProfessionisti_Users_UtenteId] FOREIGN KEY ([UtenteId]) REFERENCES [Users] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216143032_AddFatturazioneTables'
)
BEGIN
    CREATE INDEX [IX_AccessiClienti_ClienteId] ON [AccessiClienti] ([ClienteId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216143032_AddFatturazioneTables'
)
BEGIN
    CREATE INDEX [IX_AccessiClienti_ScadenzaFatturazioneId] ON [AccessiClienti] ([ScadenzaFatturazioneId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216143032_AddFatturazioneTables'
)
BEGIN
    CREATE INDEX [IX_AccessiClienti_UtenteId] ON [AccessiClienti] ([UtenteId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216143032_AddFatturazioneTables'
)
BEGIN
    CREATE UNIQUE INDEX [IX_BilanciCEE_ClienteId_Anno] ON [BilanciCEE] ([ClienteId], [Anno]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216143032_AddFatturazioneTables'
)
BEGIN
    CREATE INDEX [IX_BilanciCEE_ScadenzaFatturazioneId] ON [BilanciCEE] ([ScadenzaFatturazioneId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216143032_AddFatturazioneTables'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ContatoriDocumenti_Anno_TipoDocumento] ON [ContatoriDocumenti] ([Anno], [TipoDocumento]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216143032_AddFatturazioneTables'
)
BEGIN
    CREATE UNIQUE INDEX [IX_FattureCloud_ClienteId_Anno] ON [FattureCloud] ([ClienteId], [Anno]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216143032_AddFatturazioneTables'
)
BEGIN
    CREATE INDEX [IX_FattureCloud_ScadenzaFatturazioneId] ON [FattureCloud] ([ScadenzaFatturazioneId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216143032_AddFatturazioneTables'
)
BEGIN
    CREATE INDEX [IX_IncassiFatture_ScadenzaFatturazioneId] ON [IncassiFatture] ([ScadenzaFatturazioneId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216143032_AddFatturazioneTables'
)
BEGIN
    CREATE INDEX [IX_IncassiProfessionisti_IncassoFatturaId] ON [IncassiProfessionisti] ([IncassoFatturaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216143032_AddFatturazioneTables'
)
BEGIN
    CREATE INDEX [IX_IncassiProfessionisti_UtenteId] ON [IncassiProfessionisti] ([UtenteId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216143032_AddFatturazioneTables'
)
BEGIN
    CREATE UNIQUE INDEX [IX_MandatiClienti_ClienteId_Anno] ON [MandatiClienti] ([ClienteId], [Anno]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216143032_AddFatturazioneTables'
)
BEGIN
    CREATE INDEX [IX_ScadenzeFatturazione_ClienteId] ON [ScadenzeFatturazione] ([ClienteId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216143032_AddFatturazioneTables'
)
BEGIN
    CREATE INDEX [IX_ScadenzeFatturazione_MandatoClienteId] ON [ScadenzeFatturazione] ([MandatoClienteId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216143032_AddFatturazioneTables'
)
BEGIN
    CREATE INDEX [IX_SpesePratiche_ClienteId] ON [SpesePratiche] ([ClienteId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216143032_AddFatturazioneTables'
)
BEGIN
    CREATE INDEX [IX_SpesePratiche_ScadenzaFatturazioneId] ON [SpesePratiche] ([ScadenzaFatturazioneId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216143032_AddFatturazioneTables'
)
BEGIN
    CREATE INDEX [IX_SpesePratiche_UtenteId] ON [SpesePratiche] ([UtenteId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216143032_AddFatturazioneTables'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251216143032_AddFatturazioneTables', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216143221_AddAmministrazionePermissions'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'Description', N'DisplayOrder', N'Icon', N'PageName', N'PageUrl', N'ShowInMenu') AND [object_id] = OBJECT_ID(N'[Permissions]'))
        SET IDENTITY_INSERT [Permissions] ON;
    EXEC(N'INSERT INTO [Permissions] ([Id], [Category], [Description], [DisplayOrder], [Icon], [PageName], [PageUrl], [ShowInMenu])
    VALUES (200, N''AMMINISTRAZIONE'', N''Dashboard riepilogo fatturazione'', 20, N''fas fa-chart-line'', N''Dashboard Fatturazione'', N''/Amministrazione'', CAST(1 AS bit)),
    (201, N''AMMINISTRAZIONE'', N''Gestione mandati professionali'', 21, N''fas fa-file-contract'', N''Mandati Clienti'', N''/Amministrazione/Mandati'', CAST(1 AS bit)),
    (202, N''AMMINISTRAZIONE'', N''Gestione scadenze e fatturazione'', 22, N''fas fa-file-invoice-dollar'', N''Scadenze Fatturazione'', N''/Amministrazione/Scadenze'', CAST(1 AS bit)),
    (203, N''AMMINISTRAZIONE'', N''Gestione spese pratiche mensili'', 23, N''fas fa-receipt'', N''Spese Pratiche'', N''/Amministrazione/SpesePratiche'', CAST(1 AS bit)),
    (204, N''AMMINISTRAZIONE'', N''Registrazione accessi clienti'', 24, N''fas fa-door-open'', N''Accessi Clienti'', N''/Amministrazione/AccessiClienti'', CAST(1 AS bit)),
    (205, N''AMMINISTRAZIONE'', N''Gestione Fatture in Cloud'', 25, N''fas fa-cloud'', N''Fatture in Cloud'', N''/Amministrazione/FattureCloud'', CAST(1 AS bit)),
    (206, N''AMMINISTRAZIONE'', N''Gestione Bilanci CEE'', 26, N''fas fa-balance-scale'', N''Bilanci CEE'', N''/Amministrazione/BilanciCEE'', CAST(1 AS bit)),
    (207, N''AMMINISTRAZIONE'', N''Gestione incassi fatture'', 27, N''fas fa-money-bill-wave'', N''Incassi'', N''/Amministrazione/Incassi'', CAST(1 AS bit)),
    (208, N''AMMINISTRAZIONE'', N''Report incassi per professionista'', 28, N''fas fa-user-tie'', N''Report Professionisti'', N''/Amministrazione/ReportProfessionisti'', CAST(1 AS bit))');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'Description', N'DisplayOrder', N'Icon', N'PageName', N'PageUrl', N'ShowInMenu') AND [object_id] = OBJECT_ID(N'[Permissions]'))
        SET IDENTITY_INSERT [Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216143221_AddAmministrazionePermissions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251216143221_AddAmministrazionePermissions', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216151509_AddAnnoFatturazione'
)
BEGIN
    ALTER TABLE [ScadenzeFatturazione] ADD [AnnoFatturazioneId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216151509_AddAnnoFatturazione'
)
BEGIN
    ALTER TABLE [MandatiClienti] ADD [AnnoFatturazioneId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216151509_AddAnnoFatturazione'
)
BEGIN
    CREATE TABLE [AnniFatturazione] (
        [Id] int NOT NULL IDENTITY,
        [Anno] int NOT NULL,
        [IsCurrent] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [Note] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_AnniFatturazione] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216151509_AddAnnoFatturazione'
)
BEGIN
    CREATE INDEX [IX_ScadenzeFatturazione_AnnoFatturazioneId] ON [ScadenzeFatturazione] ([AnnoFatturazioneId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216151509_AddAnnoFatturazione'
)
BEGIN
    CREATE INDEX [IX_MandatiClienti_AnnoFatturazioneId] ON [MandatiClienti] ([AnnoFatturazioneId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216151509_AddAnnoFatturazione'
)
BEGIN
    ALTER TABLE [MandatiClienti] ADD CONSTRAINT [FK_MandatiClienti_AnniFatturazione_AnnoFatturazioneId] FOREIGN KEY ([AnnoFatturazioneId]) REFERENCES [AnniFatturazione] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216151509_AddAnnoFatturazione'
)
BEGIN
    ALTER TABLE [ScadenzeFatturazione] ADD CONSTRAINT [FK_ScadenzeFatturazione_AnniFatturazione_AnnoFatturazioneId] FOREIGN KEY ([AnnoFatturazioneId]) REFERENCES [AnniFatturazione] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216151509_AddAnnoFatturazione'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251216151509_AddAnnoFatturazione', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216151707_CleanupAnnoFatturazione'
)
BEGIN
    ALTER TABLE [MandatiClienti] DROP CONSTRAINT [FK_MandatiClienti_AnniFatturazione_AnnoFatturazioneId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216151707_CleanupAnnoFatturazione'
)
BEGIN
    ALTER TABLE [ScadenzeFatturazione] DROP CONSTRAINT [FK_ScadenzeFatturazione_AnniFatturazione_AnnoFatturazioneId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216151707_CleanupAnnoFatturazione'
)
BEGIN
    DROP INDEX [IX_ScadenzeFatturazione_AnnoFatturazioneId] ON [ScadenzeFatturazione];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216151707_CleanupAnnoFatturazione'
)
BEGIN
    DROP INDEX [IX_MandatiClienti_AnnoFatturazioneId] ON [MandatiClienti];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216151707_CleanupAnnoFatturazione'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ScadenzeFatturazione]') AND [c].[name] = N'AnnoFatturazioneId');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [ScadenzeFatturazione] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [ScadenzeFatturazione] DROP COLUMN [AnnoFatturazioneId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216151707_CleanupAnnoFatturazione'
)
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MandatiClienti]') AND [c].[name] = N'AnnoFatturazioneId');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [MandatiClienti] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [MandatiClienti] DROP COLUMN [AnnoFatturazioneId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216151707_CleanupAnnoFatturazione'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251216151707_CleanupAnnoFatturazione', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216152443_AddGestioneAnniPermission'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'Description', N'DisplayOrder', N'Icon', N'PageName', N'PageUrl', N'ShowInMenu') AND [object_id] = OBJECT_ID(N'[Permissions]'))
        SET IDENTITY_INSERT [Permissions] ON;
    EXEC(N'INSERT INTO [Permissions] ([Id], [Category], [Description], [DisplayOrder], [Icon], [PageName], [PageUrl], [ShowInMenu])
    VALUES (209, N''AMMINISTRAZIONE'', N''Gestione anni di fatturazione'', 29, N''fas fa-calendar-alt'', N''Gestione Anni'', N''/Amministrazione/GestioneAnni'', CAST(1 AS bit))');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'Description', N'DisplayOrder', N'Icon', N'PageName', N'PageUrl', N'ShowInMenu') AND [object_id] = OBJECT_ID(N'[Permissions]'))
        SET IDENTITY_INSERT [Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216152443_AddGestioneAnniPermission'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251216152443_AddGestioneAnniPermission', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216162642_AddRimborsoSpese'
)
BEGIN
    ALTER TABLE [ScadenzeFatturazione] ADD [RimborsoSpese] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216162642_AddRimborsoSpese'
)
BEGIN
    ALTER TABLE [MandatiClienti] ADD [RimborsoSpese] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216162642_AddRimborsoSpese'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251216162642_AddRimborsoSpese', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251218094318_SpesaPraticaScadenzaNullable'
)
BEGIN
    DECLARE @var3 sysname;
    SELECT @var3 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SpesePratiche]') AND [c].[name] = N'ScadenzaFatturazioneId');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [SpesePratiche] DROP CONSTRAINT [' + @var3 + '];');
    ALTER TABLE [SpesePratiche] ALTER COLUMN [ScadenzaFatturazioneId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251218094318_SpesaPraticaScadenzaNullable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251218094318_SpesaPraticaScadenzaNullable', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251219161925_AddBudgetStudioTables'
)
BEGIN
    CREATE TABLE [VociSpesaBudget] (
        [Id] int NOT NULL IDENTITY,
        [CodiceSpesa] nvarchar(50) NOT NULL,
        [Descrizione] nvarchar(200) NOT NULL,
        [MetodoPagamentoDefault] int NOT NULL,
        [NoteDefault] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_VociSpesaBudget] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251219161925_AddBudgetStudioTables'
)
BEGIN
    CREATE TABLE [BudgetSpeseMensili] (
        [Id] int NOT NULL IDENTITY,
        [VoceSpesaBudgetId] int NOT NULL,
        [Anno] int NOT NULL,
        [Mese] int NOT NULL,
        [Importo] decimal(18,2) NOT NULL,
        [Pagata] bit NOT NULL,
        [MetodoPagamento] int NULL,
        [Note] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_BudgetSpeseMensili] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BudgetSpeseMensili_VociSpesaBudget_VoceSpesaBudgetId] FOREIGN KEY ([VoceSpesaBudgetId]) REFERENCES [VociSpesaBudget] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251219161925_AddBudgetStudioTables'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'Description', N'DisplayOrder', N'Icon', N'PageName', N'PageUrl', N'ShowInMenu') AND [object_id] = OBJECT_ID(N'[Permissions]'))
        SET IDENTITY_INSERT [Permissions] ON;
    EXEC(N'INSERT INTO [Permissions] ([Id], [Category], [Description], [DisplayOrder], [Icon], [PageName], [PageUrl], [ShowInMenu])
    VALUES (210, N''AMMINISTRAZIONE'', N''Budget Studio - pianificazione spese mensili'', 30, N''fas fa-coins'', N''Budget Studio'', N''/Amministrazione/BudgetStudio'', CAST(1 AS bit))');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'Description', N'DisplayOrder', N'Icon', N'PageName', N'PageUrl', N'ShowInMenu') AND [object_id] = OBJECT_ID(N'[Permissions]'))
        SET IDENTITY_INSERT [Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251219161925_AddBudgetStudioTables'
)
BEGIN
    CREATE UNIQUE INDEX [IX_BudgetSpeseMensili_VoceSpesaBudgetId_Anno_Mese] ON [BudgetSpeseMensili] ([VoceSpesaBudgetId], [Anno], [Mese]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251219161925_AddBudgetStudioTables'
)
BEGIN
    CREATE UNIQUE INDEX [IX_VociSpesaBudget_CodiceSpesa] ON [VociSpesaBudget] ([CodiceSpesa]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251219161925_AddBudgetStudioTables'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251219161925_AddBudgetStudioTables', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251219172826_UpdateBudgetStudioPermission'
)
BEGIN
    EXEC(N'UPDATE [Permissions] SET [Category] = NULL, [PageUrl] = N''/BudgetStudio''
    WHERE [Id] = 210;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251219172826_UpdateBudgetStudioPermission'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251219172826_UpdateBudgetStudioPermission', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251222100614_AddBancheBudget'
)
BEGIN
    CREATE TABLE [BancheBudget] (
        [Id] int NOT NULL IDENTITY,
        [Nome] nvarchar(100) NOT NULL,
        [Iban] nvarchar(50) NULL,
        [IsActive] bit NOT NULL,
        [Ordine] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_BancheBudget] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251222100614_AddBancheBudget'
)
BEGIN
    CREATE TABLE [SaldiBancheMese] (
        [Id] int NOT NULL IDENTITY,
        [BancaBudgetId] int NOT NULL,
        [Anno] int NOT NULL,
        [Mese] int NOT NULL,
        [Saldo] decimal(18,2) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_SaldiBancheMese] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SaldiBancheMese_BancheBudget_BancaBudgetId] FOREIGN KEY ([BancaBudgetId]) REFERENCES [BancheBudget] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251222100614_AddBancheBudget'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SaldiBancheMese_BancaBudgetId_Anno_Mese] ON [SaldiBancheMese] ([BancaBudgetId], [Anno], [Mese]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251222100614_AddBancheBudget'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251222100614_AddBancheBudget', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251222112309_AddMacroVociBudget'
)
BEGIN
    ALTER TABLE [VociSpesaBudget] ADD [MacroVoceBudgetId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251222112309_AddMacroVociBudget'
)
BEGIN
    CREATE TABLE [MacroVociBudget] (
        [Id] int NOT NULL IDENTITY,
        [Codice] nvarchar(50) NOT NULL,
        [Descrizione] nvarchar(200) NOT NULL,
        [Ordine] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_MacroVociBudget] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251222112309_AddMacroVociBudget'
)
BEGIN
    CREATE INDEX [IX_VociSpesaBudget_MacroVoceBudgetId] ON [VociSpesaBudget] ([MacroVoceBudgetId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251222112309_AddMacroVociBudget'
)
BEGIN
    CREATE UNIQUE INDEX [IX_MacroVociBudget_Codice] ON [MacroVociBudget] ([Codice]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251222112309_AddMacroVociBudget'
)
BEGIN
    ALTER TABLE [VociSpesaBudget] ADD CONSTRAINT [FK_VociSpesaBudget_MacroVociBudget_MacroVoceBudgetId] FOREIGN KEY ([MacroVoceBudgetId]) REFERENCES [MacroVociBudget] ([Id]) ON DELETE SET NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251222112309_AddMacroVociBudget'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251222112309_AddMacroVociBudget', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251222150653_AddSistemaDocumenti'
)
BEGIN
    CREATE TABLE [ClausoleDocumenti] (
        [Id] int NOT NULL IDENTITY,
        [Nome] nvarchar(100) NOT NULL,
        [Categoria] nvarchar(50) NOT NULL,
        [Descrizione] nvarchar(500) NULL,
        [Contenuto] nvarchar(max) NOT NULL,
        [Ordine] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_ClausoleDocumenti] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251222150653_AddSistemaDocumenti'
)
BEGIN
    CREATE TABLE [ConfigurazioniStudio] (
        [Id] int NOT NULL IDENTITY,
        [NomeStudio] nvarchar(200) NOT NULL,
        [Indirizzo] nvarchar(200) NULL,
        [Citta] nvarchar(100) NULL,
        [CAP] nvarchar(10) NULL,
        [Provincia] nvarchar(50) NULL,
        [PIVA] nvarchar(16) NULL,
        [CF] nvarchar(16) NULL,
        [Email] nvarchar(100) NULL,
        [PEC] nvarchar(100) NULL,
        [Telefono] nvarchar(20) NULL,
        [Logo] varbinary(max) NULL,
        [LogoContentType] nvarchar(100) NULL,
        [LogoFileName] nvarchar(200) NULL,
        [Firma] varbinary(max) NULL,
        [FirmaContentType] nvarchar(100) NULL,
        [FirmaFileName] nvarchar(200) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_ConfigurazioniStudio] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251222150653_AddSistemaDocumenti'
)
BEGIN
    CREATE TABLE [TemplateDocumenti] (
        [Id] int NOT NULL IDENTITY,
        [Nome] nvarchar(100) NOT NULL,
        [Categoria] nvarchar(50) NOT NULL,
        [Descrizione] nvarchar(500) NULL,
        [Contenuto] nvarchar(max) NOT NULL,
        [RichiestaMandato] bit NOT NULL,
        [TipoOutputDefault] int NOT NULL,
        [Ordine] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_TemplateDocumenti] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251222150653_AddSistemaDocumenti'
)
BEGIN
    CREATE TABLE [DocumentiGenerati] (
        [Id] int NOT NULL IDENTITY,
        [TemplateDocumentoId] int NOT NULL,
        [ClienteId] int NOT NULL,
        [MandatoClienteId] int NULL,
        [NomeFile] nvarchar(200) NOT NULL,
        [Contenuto] varbinary(max) NOT NULL,
        [ContentType] nvarchar(100) NOT NULL,
        [TipoOutput] int NOT NULL,
        [GeneratoDaUserId] int NULL,
        [GeneratoIl] datetime2 NOT NULL,
        [Note] nvarchar(500) NULL,
        CONSTRAINT [PK_DocumentiGenerati] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DocumentiGenerati_Clienti_ClienteId] FOREIGN KEY ([ClienteId]) REFERENCES [Clienti] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DocumentiGenerati_MandatiClienti_MandatoClienteId] FOREIGN KEY ([MandatoClienteId]) REFERENCES [MandatiClienti] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_DocumentiGenerati_TemplateDocumenti_TemplateDocumentoId] FOREIGN KEY ([TemplateDocumentoId]) REFERENCES [TemplateDocumenti] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DocumentiGenerati_Users_GeneratoDaUserId] FOREIGN KEY ([GeneratoDaUserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251222150653_AddSistemaDocumenti'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'Description', N'DisplayOrder', N'Icon', N'PageName', N'PageUrl', N'ShowInMenu') AND [object_id] = OBJECT_ID(N'[Permissions]'))
        SET IDENTITY_INSERT [Permissions] ON;
    EXEC(N'INSERT INTO [Permissions] ([Id], [Category], [Description], [DisplayOrder], [Icon], [PageName], [PageUrl], [ShowInMenu])
    VALUES (301, N''DOCUMENTI'', N''Sistema gestione documenti e template'', 40, N''fas fa-file-alt'', N''Documenti'', N''/Documenti'', CAST(1 AS bit)),
    (302, N''DOCUMENTI'', N''Configurazione dati e logo studio'', 41, N''fas fa-building'', N''Impostazioni Studio'', N''/Documenti/ImpostazioniStudio'', CAST(1 AS bit)),
    (303, N''DOCUMENTI'', N''Gestione clausole riutilizzabili'', 42, N''fas fa-paragraph'', N''Clausole'', N''/Documenti/Clausole'', CAST(1 AS bit)),
    (304, N''DOCUMENTI'', N''Gestione template documenti'', 43, N''fas fa-file-signature'', N''Template'', N''/Documenti/Template'', CAST(1 AS bit)),
    (305, N''DOCUMENTI'', N''Genera documento da template'', 44, N''fas fa-file-export'', N''Genera Documento'', N''/Documenti/Genera'', CAST(1 AS bit)),
    (306, N''DOCUMENTI'', N''Archivio documenti generati'', 45, N''fas fa-archive'', N''Archivio Documenti'', N''/Documenti/Archivio'', CAST(1 AS bit))');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'Description', N'DisplayOrder', N'Icon', N'PageName', N'PageUrl', N'ShowInMenu') AND [object_id] = OBJECT_ID(N'[Permissions]'))
        SET IDENTITY_INSERT [Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251222150653_AddSistemaDocumenti'
)
BEGIN
    CREATE INDEX [IX_ClausoleDocumenti_Categoria] ON [ClausoleDocumenti] ([Categoria]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251222150653_AddSistemaDocumenti'
)
BEGIN
    CREATE INDEX [IX_DocumentiGenerati_ClienteId] ON [DocumentiGenerati] ([ClienteId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251222150653_AddSistemaDocumenti'
)
BEGIN
    CREATE INDEX [IX_DocumentiGenerati_GeneratoDaUserId] ON [DocumentiGenerati] ([GeneratoDaUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251222150653_AddSistemaDocumenti'
)
BEGIN
    CREATE INDEX [IX_DocumentiGenerati_GeneratoIl] ON [DocumentiGenerati] ([GeneratoIl]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251222150653_AddSistemaDocumenti'
)
BEGIN
    CREATE INDEX [IX_DocumentiGenerati_MandatoClienteId] ON [DocumentiGenerati] ([MandatoClienteId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251222150653_AddSistemaDocumenti'
)
BEGIN
    CREATE INDEX [IX_DocumentiGenerati_TemplateDocumentoId] ON [DocumentiGenerati] ([TemplateDocumentoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251222150653_AddSistemaDocumenti'
)
BEGIN
    CREATE INDEX [IX_TemplateDocumenti_Categoria] ON [TemplateDocumenti] ([Categoria]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251222150653_AddSistemaDocumenti'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251222150653_AddSistemaDocumenti', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251222162559_AddIntestazioneFooterTemplate'
)
BEGIN
    ALTER TABLE [TemplateDocumenti] ADD [Intestazione] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251222162559_AddIntestazioneFooterTemplate'
)
BEGIN
    ALTER TABLE [TemplateDocumenti] ADD [PiePagina] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251222162559_AddIntestazioneFooterTemplate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251222162559_AddIntestazioneFooterTemplate', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251223100200_AddDocumentoIdentitaSoggetti'
)
BEGIN
    ALTER TABLE [ClientiSoggetti] ADD [DocumentoDataRilascio] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251223100200_AddDocumentoIdentitaSoggetti'
)
BEGIN
    ALTER TABLE [ClientiSoggetti] ADD [DocumentoNumero] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251223100200_AddDocumentoIdentitaSoggetti'
)
BEGIN
    ALTER TABLE [ClientiSoggetti] ADD [DocumentoRilasciatoDa] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251223100200_AddDocumentoIdentitaSoggetti'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251223100200_AddDocumentoIdentitaSoggetti', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251223153344_AddDocumentoScadenzaToClienteSoggetto'
)
BEGIN
    ALTER TABLE [ClientiSoggetti] ADD [DocumentoScadenza] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251223153344_AddDocumentoScadenzaToClienteSoggetto'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251223153344_AddDocumentoScadenzaToClienteSoggetto', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251227153759_AddDataLimiteAntiriciclaggio'
)
BEGIN
    ALTER TABLE [ConfigurazioniStudio] ADD [DataLimiteAntiriciclaggio] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251227153759_AddDataLimiteAntiriciclaggio'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251227153759_AddDataLimiteAntiriciclaggio', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251228143115_AddMissingPermissions'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'Description', N'DisplayOrder', N'Icon', N'PageName', N'PageUrl', N'ShowInMenu') AND [object_id] = OBJECT_ID(N'[Permissions]'))
        SET IDENTITY_INSERT [Permissions] ON;
    EXEC(N'INSERT INTO [Permissions] ([Id], [Category], [Description], [DisplayOrder], [Icon], [PageName], [PageUrl], [ShowInMenu])
    VALUES (103, N''ANAGRAFICA'', N''Gestione scadenze documenti identità soggetti'', 13, N''fas fa-id-card'', N''Scadenze Documenti Identità'', N''/Clienti/ScadenzeDocumenti'', CAST(1 AS bit)),
    (307, N''DOCUMENTI'', N''Controllo scadenze documenti antiriciclaggio'', 46, N''fas fa-shield-alt'', N''Controllo Antiriciclaggio'', N''/Documenti/ControlloAntiriciclaggio'', CAST(1 AS bit))');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'Description', N'DisplayOrder', N'Icon', N'PageName', N'PageUrl', N'ShowInMenu') AND [object_id] = OBJECT_ID(N'[Permissions]'))
        SET IDENTITY_INSERT [Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251228143115_AddMissingPermissions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251228143115_AddMissingPermissions', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251229082235_AddStatiAttivitaDinamici'
)
BEGIN
    ALTER TABLE [ClientiAttivita] ADD [StatoAttivitaTipoId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251229082235_AddStatiAttivitaDinamici'
)
BEGIN
    CREATE TABLE [StatiAttivitaTipo] (
        [Id] int NOT NULL IDENTITY,
        [AttivitaTipoId] int NOT NULL,
        [Nome] nvarchar(100) NOT NULL,
        [Icon] nvarchar(50) NULL,
        [ColoreTesto] nvarchar(20) NOT NULL,
        [ColoreSfondo] nvarchar(20) NOT NULL,
        [DisplayOrder] int NOT NULL,
        [IsDefault] bit NOT NULL,
        [IsFinale] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_StatiAttivitaTipo] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StatiAttivitaTipo_AttivitaTipi_AttivitaTipoId] FOREIGN KEY ([AttivitaTipoId]) REFERENCES [AttivitaTipi] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251229082235_AddStatiAttivitaDinamici'
)
BEGIN
    CREATE INDEX [IX_ClientiAttivita_StatoAttivitaTipoId] ON [ClientiAttivita] ([StatoAttivitaTipoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251229082235_AddStatiAttivitaDinamici'
)
BEGIN
    CREATE UNIQUE INDEX [IX_StatiAttivitaTipo_AttivitaTipoId_Nome] ON [StatiAttivitaTipo] ([AttivitaTipoId], [Nome]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251229082235_AddStatiAttivitaDinamici'
)
BEGIN
    ALTER TABLE [ClientiAttivita] ADD CONSTRAINT [FK_ClientiAttivita_StatiAttivitaTipo_StatoAttivitaTipoId] FOREIGN KEY ([StatoAttivitaTipoId]) REFERENCES [StatiAttivitaTipo] ([Id]) ON DELETE SET NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251229082235_AddStatiAttivitaDinamici'
)
BEGIN

                    -- Per ogni AttivitaTipo esistente, crea 5 stati di default
                    INSERT INTO StatiAttivitaTipo (AttivitaTipoId, Nome, Icon, ColoreTesto, ColoreSfondo, DisplayOrder, IsDefault, IsFinale, IsActive, CreatedAt)
                    SELECT 
                        Id,
                        'Da Fare',
                        'fas fa-clock',
                        '#000000',
                        '#ffc107',
                        0,
                        1,  -- IsDefault = true (stato iniziale)
                        0,  -- IsFinale = false
                        1,
                        GETDATE()
                    FROM AttivitaTipi;

                    INSERT INTO StatiAttivitaTipo (AttivitaTipoId, Nome, Icon, ColoreTesto, ColoreSfondo, DisplayOrder, IsDefault, IsFinale, IsActive, CreatedAt)
                    SELECT 
                        Id,
                        'Completata',
                        'fas fa-check-circle',
                        '#FFFFFF',
                        '#28a745',
                        1,
                        0,
                        1,  -- IsFinale = true (stato completamento)
                        1,
                        GETDATE()
                    FROM AttivitaTipi;

                    INSERT INTO StatiAttivitaTipo (AttivitaTipoId, Nome, Icon, ColoreTesto, ColoreSfondo, DisplayOrder, IsDefault, IsFinale, IsActive, CreatedAt)
                    SELECT 
                        Id,
                        'Da inviare Entratel',
                        'fas fa-paper-plane',
                        '#FFFFFF',
                        '#17a2b8',
                        2,
                        0,
                        0,
                        1,
                        GETDATE()
                    FROM AttivitaTipi;

                    INSERT INTO StatiAttivitaTipo (AttivitaTipoId, Nome, Icon, ColoreTesto, ColoreSfondo, DisplayOrder, IsDefault, IsFinale, IsActive, CreatedAt)
                    SELECT 
                        Id,
                        'DR Inviate',
                        'fas fa-envelope-open-text',
                        '#FFFFFF',
                        '#6f42c1',
                        3,
                        0,
                        0,
                        1,
                        GETDATE()
                    FROM AttivitaTipi;

                    INSERT INTO StatiAttivitaTipo (AttivitaTipoId, Nome, Icon, ColoreTesto, ColoreSfondo, DisplayOrder, IsDefault, IsFinale, IsActive, CreatedAt)
                    SELECT 
                        Id,
                        'Sospesa',
                        'fas fa-pause-circle',
                        '#FFFFFF',
                        '#dc3545',
                        4,
                        0,
                        0,
                        1,
                        GETDATE()
                    FROM AttivitaTipi;
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251229082235_AddStatiAttivitaDinamici'
)
BEGIN

                    -- Aggiorna ClienteAttivita con il nuovo StatoAttivitaTipoId basato sul vecchio campo Stato
                    UPDATE ca
                    SET ca.StatoAttivitaTipoId = sat.Id
                    FROM ClientiAttivita ca
                    INNER JOIN AttivitaAnnuali aa ON ca.AttivitaAnnualeId = aa.Id
                    INNER JOIN StatiAttivitaTipo sat ON sat.AttivitaTipoId = aa.AttivitaTipoId
                    WHERE 
                        (ca.Stato = 0 AND sat.Nome = 'Da Fare') OR
                        (ca.Stato = 1 AND sat.Nome = 'Completata') OR
                        (ca.Stato = 2 AND sat.Nome = 'Da inviare Entratel') OR
                        (ca.Stato = 3 AND sat.Nome = 'DR Inviate') OR
                        (ca.Stato = 4 AND sat.Nome = 'Sospesa');
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251229082235_AddStatiAttivitaDinamici'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251229082235_AddStatiAttivitaDinamici', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260101130927_AddCampiCustomClienteEMenuDinamico'
)
BEGIN
    CREATE TABLE [CampiCustomClienti] (
        [Id] int NOT NULL IDENTITY,
        [Nome] nvarchar(100) NOT NULL,
        [Label] nvarchar(100) NOT NULL,
        [TipoCampo] nvarchar(50) NOT NULL,
        [IsRequired] bit NOT NULL,
        [ShowInList] bit NOT NULL,
        [UseAsFilter] bit NOT NULL,
        [Options] nvarchar(500) NULL,
        [DefaultValue] nvarchar(200) NULL,
        [Placeholder] nvarchar(200) NULL,
        [DisplayOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_CampiCustomClienti] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260101130927_AddCampiCustomClienteEMenuDinamico'
)
BEGIN
    CREATE TABLE [VociMenu] (
        [Id] int NOT NULL IDENTITY,
        [Nome] nvarchar(100) NOT NULL,
        [Url] nvarchar(255) NULL,
        [Icon] nvarchar(50) NOT NULL,
        [Categoria] nvarchar(50) NULL,
        [DisplayOrder] int NOT NULL,
        [IsVisible] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [IsGroup] bit NOT NULL,
        [ExpandedByDefault] bit NOT NULL,
        [ParentId] int NULL,
        [TipoVoce] nvarchar(50) NOT NULL,
        [ReferenceId] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_VociMenu] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_VociMenu_VociMenu_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [VociMenu] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260101130927_AddCampiCustomClienteEMenuDinamico'
)
BEGIN
    CREATE TABLE [ValoriCampiCustomClienti] (
        [Id] int NOT NULL IDENTITY,
        [ClienteId] int NOT NULL,
        [CampoCustomClienteId] int NOT NULL,
        [Valore] nvarchar(max) NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ValoriCampiCustomClienti] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ValoriCampiCustomClienti_CampiCustomClienti_CampoCustomClienteId] FOREIGN KEY ([CampoCustomClienteId]) REFERENCES [CampiCustomClienti] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ValoriCampiCustomClienti_Clienti_ClienteId] FOREIGN KEY ([ClienteId]) REFERENCES [Clienti] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260101130927_AddCampiCustomClienteEMenuDinamico'
)
BEGIN
    CREATE TABLE [ConfigurazioniMenuUtenti] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [VoceMenuId] int NOT NULL,
        [IsVisible] bit NOT NULL,
        [CustomOrder] int NULL,
        CONSTRAINT [PK_ConfigurazioniMenuUtenti] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ConfigurazioniMenuUtenti_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ConfigurazioniMenuUtenti_VociMenu_VoceMenuId] FOREIGN KEY ([VoceMenuId]) REFERENCES [VociMenu] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260101130927_AddCampiCustomClienteEMenuDinamico'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CampiCustomClienti_Nome] ON [CampiCustomClienti] ([Nome]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260101130927_AddCampiCustomClienteEMenuDinamico'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ConfigurazioniMenuUtenti_UserId_VoceMenuId] ON [ConfigurazioniMenuUtenti] ([UserId], [VoceMenuId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260101130927_AddCampiCustomClienteEMenuDinamico'
)
BEGIN
    CREATE INDEX [IX_ConfigurazioniMenuUtenti_VoceMenuId] ON [ConfigurazioniMenuUtenti] ([VoceMenuId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260101130927_AddCampiCustomClienteEMenuDinamico'
)
BEGIN
    CREATE INDEX [IX_ValoriCampiCustomClienti_CampoCustomClienteId] ON [ValoriCampiCustomClienti] ([CampoCustomClienteId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260101130927_AddCampiCustomClienteEMenuDinamico'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ValoriCampiCustomClienti_ClienteId_CampoCustomClienteId] ON [ValoriCampiCustomClienti] ([ClienteId], [CampoCustomClienteId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260101130927_AddCampiCustomClienteEMenuDinamico'
)
BEGIN
    CREATE INDEX [IX_VociMenu_ParentId] ON [VociMenu] ([ParentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260101130927_AddCampiCustomClienteEMenuDinamico'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260101130927_AddCampiCustomClienteEMenuDinamico', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260101132447_AddEntitaDinamiche'
)
BEGIN
    CREATE TABLE [EntitaDinamiche] (
        [Id] int NOT NULL IDENTITY,
        [Nome] nvarchar(100) NOT NULL,
        [NomePluruale] nvarchar(100) NOT NULL,
        [Descrizione] nvarchar(500) NULL,
        [Icon] nvarchar(50) NOT NULL,
        [Colore] nvarchar(20) NOT NULL,
        [CollegataACliente] bit NOT NULL,
        [DisplayOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_EntitaDinamiche] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260101132447_AddEntitaDinamiche'
)
BEGIN
    CREATE TABLE [CampiEntita] (
        [Id] int NOT NULL IDENTITY,
        [EntitaDinamicaId] int NOT NULL,
        [Nome] nvarchar(100) NOT NULL,
        [Label] nvarchar(100) NOT NULL,
        [TipoCampo] nvarchar(50) NOT NULL,
        [IsRequired] bit NOT NULL,
        [ShowInList] bit NOT NULL,
        [UseAsFilter] bit NOT NULL,
        [Options] nvarchar(1000) NULL,
        [DefaultValue] nvarchar(200) NULL,
        [Placeholder] nvarchar(200) NULL,
        [DisplayOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        [ColWidth] int NOT NULL,
        CONSTRAINT [PK_CampiEntita] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CampiEntita_EntitaDinamiche_EntitaDinamicaId] FOREIGN KEY ([EntitaDinamicaId]) REFERENCES [EntitaDinamiche] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260101132447_AddEntitaDinamiche'
)
BEGIN
    CREATE TABLE [StatiEntita] (
        [Id] int NOT NULL IDENTITY,
        [EntitaDinamicaId] int NOT NULL,
        [Nome] nvarchar(100) NOT NULL,
        [Icon] nvarchar(50) NULL,
        [ColoreTesto] nvarchar(20) NOT NULL,
        [ColoreSfondo] nvarchar(20) NOT NULL,
        [DisplayOrder] int NOT NULL,
        [IsDefault] bit NOT NULL,
        [IsFinale] bit NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_StatiEntita] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StatiEntita_EntitaDinamiche_EntitaDinamicaId] FOREIGN KEY ([EntitaDinamicaId]) REFERENCES [EntitaDinamiche] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260101132447_AddEntitaDinamiche'
)
BEGIN
    CREATE TABLE [RecordsEntita] (
        [Id] int NOT NULL IDENTITY,
        [EntitaDinamicaId] int NOT NULL,
        [ClienteId] int NULL,
        [StatoEntitaId] int NULL,
        [Titolo] nvarchar(200) NULL,
        [Note] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [CreatedByUserId] int NULL,
        CONSTRAINT [PK_RecordsEntita] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RecordsEntita_Clienti_ClienteId] FOREIGN KEY ([ClienteId]) REFERENCES [Clienti] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_RecordsEntita_EntitaDinamiche_EntitaDinamicaId] FOREIGN KEY ([EntitaDinamicaId]) REFERENCES [EntitaDinamiche] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_RecordsEntita_StatiEntita_StatoEntitaId] FOREIGN KEY ([StatoEntitaId]) REFERENCES [StatiEntita] ([Id]),
        CONSTRAINT [FK_RecordsEntita_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260101132447_AddEntitaDinamiche'
)
BEGIN
    CREATE TABLE [ValoriCampiEntita] (
        [Id] int NOT NULL IDENTITY,
        [RecordEntitaId] int NOT NULL,
        [CampoEntitaId] int NOT NULL,
        [Valore] nvarchar(max) NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ValoriCampiEntita] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ValoriCampiEntita_CampiEntita_CampoEntitaId] FOREIGN KEY ([CampoEntitaId]) REFERENCES [CampiEntita] ([Id]),
        CONSTRAINT [FK_ValoriCampiEntita_RecordsEntita_RecordEntitaId] FOREIGN KEY ([RecordEntitaId]) REFERENCES [RecordsEntita] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260101132447_AddEntitaDinamiche'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CampiEntita_EntitaDinamicaId_Nome] ON [CampiEntita] ([EntitaDinamicaId], [Nome]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260101132447_AddEntitaDinamiche'
)
BEGIN
    CREATE UNIQUE INDEX [IX_EntitaDinamiche_Nome] ON [EntitaDinamiche] ([Nome]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260101132447_AddEntitaDinamiche'
)
BEGIN
    CREATE INDEX [IX_RecordsEntita_ClienteId] ON [RecordsEntita] ([ClienteId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260101132447_AddEntitaDinamiche'
)
BEGIN
    CREATE INDEX [IX_RecordsEntita_CreatedByUserId] ON [RecordsEntita] ([CreatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260101132447_AddEntitaDinamiche'
)
BEGIN
    CREATE INDEX [IX_RecordsEntita_EntitaDinamicaId] ON [RecordsEntita] ([EntitaDinamicaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260101132447_AddEntitaDinamiche'
)
BEGIN
    CREATE INDEX [IX_RecordsEntita_StatoEntitaId] ON [RecordsEntita] ([StatoEntitaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260101132447_AddEntitaDinamiche'
)
BEGIN
    CREATE UNIQUE INDEX [IX_StatiEntita_EntitaDinamicaId_Nome] ON [StatiEntita] ([EntitaDinamicaId], [Nome]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260101132447_AddEntitaDinamiche'
)
BEGIN
    CREATE INDEX [IX_ValoriCampiEntita_CampoEntitaId] ON [ValoriCampiEntita] ([CampoEntitaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260101132447_AddEntitaDinamiche'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ValoriCampiEntita_RecordEntitaId_CampoEntitaId] ON [ValoriCampiEntita] ([RecordEntitaId], [CampoEntitaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260101132447_AddEntitaDinamiche'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260101132447_AddEntitaDinamiche', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260102173651_AddCalculatedFieldsToAttivitaCampo'
)
BEGIN
    ALTER TABLE [AttivitaCampi] ADD [Formula] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260102173651_AddCalculatedFieldsToAttivitaCampo'
)
BEGIN
    ALTER TABLE [AttivitaCampi] ADD [IsCalculated] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260102173651_AddCalculatedFieldsToAttivitaCampo'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260102173651_AddCalculatedFieldsToAttivitaCampo', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260102190333_AddColumnWidthToAttivitaCampo'
)
BEGIN
    ALTER TABLE [AttivitaCampi] ADD [ColumnWidth] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260102190333_AddColumnWidthToAttivitaCampo'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260102190333_AddColumnWidthToAttivitaCampo', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260102191523_AddCalculatedFieldsToCampoEntita'
)
BEGIN
    ALTER TABLE [CampiEntita] ADD [ColumnWidth] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260102191523_AddCalculatedFieldsToCampoEntita'
)
BEGIN
    ALTER TABLE [CampiEntita] ADD [Formula] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260102191523_AddCalculatedFieldsToCampoEntita'
)
BEGIN
    ALTER TABLE [CampiEntita] ADD [IsCalculated] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260102191523_AddCalculatedFieldsToCampoEntita'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260102191523_AddCalculatedFieldsToCampoEntita', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260102200331_AddScadenzaFields'
)
BEGIN
    ALTER TABLE [EntitaDinamiche] ADD [GiorniPreavvisoScadenza] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260102200331_AddScadenzaFields'
)
BEGIN
    ALTER TABLE [CampiEntita] ADD [IsDataScadenza] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260102200331_AddScadenzaFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260102200331_AddScadenzaFields', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260102202229_AddColumnWidthsToEntita'
)
BEGIN
    ALTER TABLE [EntitaDinamiche] ADD [LarghezzaColonnaCliente] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260102202229_AddColumnWidthsToEntita'
)
BEGIN
    ALTER TABLE [EntitaDinamiche] ADD [LarghezzaColonnaStato] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260102202229_AddColumnWidthsToEntita'
)
BEGIN
    ALTER TABLE [EntitaDinamiche] ADD [LarghezzaColonnaTitolo] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260102202229_AddColumnWidthsToEntita'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260102202229_AddColumnWidthsToEntita', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104102839_AddAttivitaPeriodicheTabelle'
)
BEGIN
    CREATE TABLE [AttivitaPeriodiche] (
        [Id] int NOT NULL IDENTITY,
        [Nome] nvarchar(100) NOT NULL,
        [NomePlurale] nvarchar(100) NOT NULL,
        [Descrizione] nvarchar(500) NULL,
        [Icona] nvarchar(50) NOT NULL,
        [Colore] nvarchar(20) NOT NULL,
        [CollegataACliente] bit NOT NULL,
        [OrdineMenu] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [LarghezzaColonnaCliente] int NOT NULL,
        [LarghezzaColonnaTitolo] int NOT NULL,
        CONSTRAINT [PK_AttivitaPeriodiche] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104102839_AddAttivitaPeriodicheTabelle'
)
BEGIN
    CREATE TABLE [TipiPeriodo] (
        [Id] int NOT NULL IDENTITY,
        [AttivitaPeriodicaId] int NOT NULL,
        [Nome] nvarchar(100) NOT NULL,
        [NumeroPeriodi] int NOT NULL,
        [EtichettePeriodi] nvarchar(max) NOT NULL,
        [DateInizioPeriodi] nvarchar(max) NOT NULL,
        [DateFinePeriodi] nvarchar(max) NOT NULL,
        [Icona] nvarchar(50) NOT NULL,
        [Colore] nvarchar(20) NOT NULL,
        [MostraInteressi] bit NOT NULL,
        [PercentualeInteressiDefault] decimal(5,2) NOT NULL,
        [MostraAccordion] bit NOT NULL,
        [DisplayOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_TipiPeriodo] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TipiPeriodo_AttivitaPeriodiche_AttivitaPeriodicaId] FOREIGN KEY ([AttivitaPeriodicaId]) REFERENCES [AttivitaPeriodiche] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104102839_AddAttivitaPeriodicheTabelle'
)
BEGIN
    CREATE TABLE [CampiPeriodici] (
        [Id] int NOT NULL IDENTITY,
        [TipoPeriodoId] int NOT NULL,
        [Nome] nvarchar(100) NOT NULL,
        [Label] nvarchar(100) NOT NULL,
        [LabelPrimoPeriodo] nvarchar(100) NULL,
        [TipoCampo] nvarchar(50) NOT NULL,
        [IsRequired] bit NOT NULL,
        [ShowInList] bit NOT NULL,
        [UseAsFilter] bit NOT NULL,
        [Options] nvarchar(1000) NULL,
        [DefaultValue] nvarchar(200) NULL,
        [Placeholder] nvarchar(200) NULL,
        [ColWidth] int NOT NULL,
        [ColumnWidth] int NOT NULL,
        [DisplayOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        [IsCalculated] bit NOT NULL,
        [Formula] nvarchar(500) NULL,
        CONSTRAINT [PK_CampiPeriodici] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CampiPeriodici_TipiPeriodo_TipoPeriodoId] FOREIGN KEY ([TipoPeriodoId]) REFERENCES [TipiPeriodo] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104102839_AddAttivitaPeriodicheTabelle'
)
BEGIN
    CREATE TABLE [ClientiAttivitaPeriodiche] (
        [Id] int NOT NULL IDENTITY,
        [AttivitaPeriodicaId] int NOT NULL,
        [TipoPeriodoId] int NOT NULL,
        [ClienteId] int NOT NULL,
        [AnnoFiscale] int NOT NULL,
        [CodCoge] nvarchar(50) NULL,
        [PercentualeInteressi] decimal(5,2) NULL,
        [Note] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ClientiAttivitaPeriodiche] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ClientiAttivitaPeriodiche_AttivitaPeriodiche_AttivitaPeriodicaId] FOREIGN KEY ([AttivitaPeriodicaId]) REFERENCES [AttivitaPeriodiche] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ClientiAttivitaPeriodiche_Clienti_ClienteId] FOREIGN KEY ([ClienteId]) REFERENCES [Clienti] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ClientiAttivitaPeriodiche_TipiPeriodo_TipoPeriodoId] FOREIGN KEY ([TipoPeriodoId]) REFERENCES [TipiPeriodo] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104102839_AddAttivitaPeriodicheTabelle'
)
BEGIN
    CREATE TABLE [RegoleCampi] (
        [Id] int NOT NULL IDENTITY,
        [CampoPeriodicoId] int NOT NULL,
        [TipoRegola] nvarchar(20) NOT NULL,
        [NomeRegola] nvarchar(100) NULL,
        [CampoOrigineId] int NULL,
        [CampoDestinazioneId] int NULL,
        [CondizioneRiporto] nvarchar(50) NULL,
        [Operatore] nvarchar(10) NULL,
        [ValoreConfronto] nvarchar(100) NULL,
        [ColoreTesto] nvarchar(20) NULL,
        [ColoreSfondo] nvarchar(20) NULL,
        [Grassetto] bit NOT NULL,
        [Icona] nvarchar(50) NULL,
        [ApplicaA] nvarchar(20) NOT NULL,
        [Priorita] int NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_RegoleCampi] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RegoleCampi_CampiPeriodici_CampoDestinazioneId] FOREIGN KEY ([CampoDestinazioneId]) REFERENCES [CampiPeriodici] ([Id]),
        CONSTRAINT [FK_RegoleCampi_CampiPeriodici_CampoOrigineId] FOREIGN KEY ([CampoOrigineId]) REFERENCES [CampiPeriodici] ([Id]),
        CONSTRAINT [FK_RegoleCampi_CampiPeriodici_CampoPeriodicoId] FOREIGN KEY ([CampoPeriodicoId]) REFERENCES [CampiPeriodici] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104102839_AddAttivitaPeriodicheTabelle'
)
BEGIN
    CREATE TABLE [ValoriPeriodi] (
        [Id] int NOT NULL IDENTITY,
        [ClienteAttivitaPeriodicaId] int NOT NULL,
        [NumeroPeriodo] int NOT NULL,
        [Valori] nvarchar(max) NOT NULL,
        [ValoriCalcolati] nvarchar(max) NOT NULL,
        [DataAggiornamento] datetime2 NULL,
        [Note] nvarchar(max) NULL,
        CONSTRAINT [PK_ValoriPeriodi] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ValoriPeriodi_ClientiAttivitaPeriodiche_ClienteAttivitaPeriodicaId] FOREIGN KEY ([ClienteAttivitaPeriodicaId]) REFERENCES [ClientiAttivitaPeriodiche] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104102839_AddAttivitaPeriodicheTabelle'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AttivitaPeriodiche_Nome] ON [AttivitaPeriodiche] ([Nome]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104102839_AddAttivitaPeriodicheTabelle'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CampiPeriodici_TipoPeriodoId_Nome] ON [CampiPeriodici] ([TipoPeriodoId], [Nome]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104102839_AddAttivitaPeriodicheTabelle'
)
BEGIN
    CREATE INDEX [IX_ClientiAttivitaPeriodiche_AttivitaPeriodicaId] ON [ClientiAttivitaPeriodiche] ([AttivitaPeriodicaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104102839_AddAttivitaPeriodicheTabelle'
)
BEGIN
    CREATE INDEX [IX_ClientiAttivitaPeriodiche_ClienteId] ON [ClientiAttivitaPeriodiche] ([ClienteId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104102839_AddAttivitaPeriodicheTabelle'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ClientiAttivitaPeriodiche_TipoPeriodoId_ClienteId_AnnoFiscale] ON [ClientiAttivitaPeriodiche] ([TipoPeriodoId], [ClienteId], [AnnoFiscale]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104102839_AddAttivitaPeriodicheTabelle'
)
BEGIN
    CREATE INDEX [IX_RegoleCampi_CampoDestinazioneId] ON [RegoleCampi] ([CampoDestinazioneId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104102839_AddAttivitaPeriodicheTabelle'
)
BEGIN
    CREATE INDEX [IX_RegoleCampi_CampoOrigineId] ON [RegoleCampi] ([CampoOrigineId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104102839_AddAttivitaPeriodicheTabelle'
)
BEGIN
    CREATE INDEX [IX_RegoleCampi_CampoPeriodicoId] ON [RegoleCampi] ([CampoPeriodicoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104102839_AddAttivitaPeriodicheTabelle'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TipiPeriodo_AttivitaPeriodicaId_Nome] ON [TipiPeriodo] ([AttivitaPeriodicaId], [Nome]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104102839_AddAttivitaPeriodicheTabelle'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ValoriPeriodi_ClienteAttivitaPeriodicaId_NumeroPeriodo] ON [ValoriPeriodi] ([ClienteAttivitaPeriodicaId], [NumeroPeriodo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104102839_AddAttivitaPeriodicheTabelle'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260104102839_AddAttivitaPeriodicheTabelle', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104113752_AddAttivitaPeriodicheFlags'
)
BEGIN
    ALTER TABLE [Clienti] ADD [AttivitaPeriodicheFlags] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104113752_AddAttivitaPeriodicheFlags'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260104113752_AddAttivitaPeriodicheFlags', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104123122_AddIsCampoClienteToCampoPeriodico'
)
BEGIN
    ALTER TABLE [CampiPeriodici] ADD [IsCampoCliente] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104123122_AddIsCampoClienteToCampoPeriodico'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260104123122_AddIsCampoClienteToCampoPeriodico', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104132509_AddCompletionAndResultIndicators'
)
BEGIN
    ALTER TABLE [CampiPeriodici] ADD [IsCompletionIndicator] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104132509_AddCompletionAndResultIndicators'
)
BEGIN
    ALTER TABLE [CampiPeriodici] ADD [IsResultIndicator] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104132509_AddCompletionAndResultIndicators'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260104132509_AddCompletionAndResultIndicators', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104135908_AddPeriodiVisibiliCampo'
)
BEGIN
    ALTER TABLE [CampiPeriodici] ADD [PeriodiVisibili] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104135908_AddPeriodiVisibiliCampo'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260104135908_AddPeriodiVisibiliCampo', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260105124422_AddMissingPagePermissions'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'Description', N'DisplayOrder', N'Icon', N'PageName', N'PageUrl', N'ShowInMenu') AND [object_id] = OBJECT_ID(N'[Permissions]'))
        SET IDENTITY_INSERT [Permissions] ON;
    EXEC(N'INSERT INTO [Permissions] ([Id], [Category], [Description], [DisplayOrder], [Icon], [PageName], [PageUrl], [ShowInMenu])
    VALUES (104, N''ANAGRAFICA'', N''Gestione dati attività annuali clienti'', 14, N''fas fa-tasks'', N''Attività'', N''/Attivita'', CAST(1 AS bit)),
    (105, N''ANAGRAFICA'', N''Gestione attività periodiche (LIPE, ecc.)'', 15, N''fas fa-calendar-check'', N''Attività Periodiche'', N''/AttivitaPeriodiche'', CAST(1 AS bit)),
    (106, N''ANAGRAFICA'', N''Gestione entità dinamiche'', 16, N''fas fa-cubes'', N''Entità'', N''/Entita'', CAST(1 AS bit)),
    (400, N''SISTEMA'', N''Gestione pagine dinamiche'', 50, N''fas fa-file-alt'', N''Pagine Dinamiche'', N''/DynamicPages'', CAST(1 AS bit)),
    (402, N''SISTEMA'', N''Gestione permessi utenti'', 52, N''fas fa-user-shield'', N''Permessi'', N''/Permissions'', CAST(1 AS bit))');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'Description', N'DisplayOrder', N'Icon', N'PageName', N'PageUrl', N'ShowInMenu') AND [object_id] = OBJECT_ID(N'[Permissions]'))
        SET IDENTITY_INSERT [Permissions] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260105124422_AddMissingPagePermissions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260105124422_AddMissingPagePermissions', N'9.0.0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260107172915_AddFormulaFieldToBudgetSpesaMensile'
)
BEGIN
    ALTER TABLE [BudgetSpeseMensili] ADD [Formula] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260107172915_AddFormulaFieldToBudgetSpesaMensile'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260107172915_AddFormulaFieldToBudgetSpesaMensile', N'9.0.0');
END;

COMMIT;
GO

