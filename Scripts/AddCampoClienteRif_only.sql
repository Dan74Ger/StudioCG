BEGIN TRANSACTION;
ALTER TABLE [AttivitaCampi] ADD [CampoClienteRif] nvarchar(50) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260108165956_AddCampoClienteRifToAttivitaCampo', N'9.0.0');

COMMIT;
GO

