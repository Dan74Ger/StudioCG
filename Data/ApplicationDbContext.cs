using Microsoft.EntityFrameworkCore;
using StudioCG.Web.Models;
using StudioCG.Web.Models.AttivitaPeriodiche;
using StudioCG.Web.Models.BudgetStudio;
using StudioCG.Web.Models.Documenti;
using StudioCG.Web.Models.Entita;
using StudioCG.Web.Models.Fatturazione;

namespace StudioCG.Web.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<UserPermission> UserPermissions { get; set; }
        
        // Tabelle per sistema dinamico
        public DbSet<DynamicPage> DynamicPages { get; set; }
        public DbSet<DynamicField> DynamicFields { get; set; }
        public DbSet<DynamicRecord> DynamicRecords { get; set; }
        public DbSet<DynamicFieldValue> DynamicFieldValues { get; set; }

        // Tabelle Anagrafica
        public DbSet<Cliente> Clienti { get; set; }
        public DbSet<ClienteSoggetto> ClientiSoggetti { get; set; }
        public DbSet<AnnualitaFiscale> AnnualitaFiscali { get; set; }

        // Tabelle Attività
        public DbSet<AttivitaTipo> AttivitaTipi { get; set; }
        public DbSet<AttivitaCampo> AttivitaCampi { get; set; }
        public DbSet<AttivitaAnnuale> AttivitaAnnuali { get; set; }
        public DbSet<ClienteAttivita> ClientiAttivita { get; set; }
        public DbSet<ClienteAttivitaValore> ClientiAttivitaValori { get; set; }
        public DbSet<StatoAttivitaTipo> StatiAttivitaTipo { get; set; }

        // Tabelle Amministrazione/Fatturazione
        public DbSet<AnnoFatturazione> AnniFatturazione { get; set; }
        public DbSet<MandatoCliente> MandatiClienti { get; set; }
        public DbSet<ScadenzaFatturazione> ScadenzeFatturazione { get; set; }
        public DbSet<SpesaPratica> SpesePratiche { get; set; }
        public DbSet<AccessoCliente> AccessiClienti { get; set; }
        public DbSet<FatturaCloud> FattureCloud { get; set; }
        public DbSet<BilancioCEE> BilanciCEE { get; set; }
        public DbSet<ContatoreDocumento> ContatoriDocumenti { get; set; }
        public DbSet<IncassoFattura> IncassiFatture { get; set; }
        public DbSet<IncassoProfessionista> IncassiProfessionisti { get; set; }

        // Tabelle Budget Studio
        public DbSet<MacroVoceBudget> MacroVociBudget { get; set; }
        public DbSet<VoceSpesaBudget> VociSpesaBudget { get; set; }
        public DbSet<BudgetSpesaMensile> BudgetSpeseMensili { get; set; }
        public DbSet<BancaBudget> BancheBudget { get; set; }
        public DbSet<SaldoBancaMese> SaldiBancheMese { get; set; }

        // Tabelle Sistema Documenti
        public DbSet<ConfigurazioneStudio> ConfigurazioniStudio { get; set; }
        public DbSet<ClausolaDocumento> ClausoleDocumenti { get; set; }
        public DbSet<TemplateDocumento> TemplateDocumenti { get; set; }
        public DbSet<DocumentoGenerato> DocumentiGenerati { get; set; }

        // Tabelle Campi Custom Cliente
        public DbSet<CampoCustomCliente> CampiCustomClienti { get; set; }
        public DbSet<ValoreCampoCustomCliente> ValoriCampiCustomClienti { get; set; }

        // Tabelle Menu Dinamico
        public DbSet<VoceMenu> VociMenu { get; set; }
        public DbSet<ConfigurazioneMenuUtente> ConfigurazioniMenuUtenti { get; set; }

        // Tabelle Entità Dinamiche
        public DbSet<EntitaDinamica> EntitaDinamiche { get; set; }
        public DbSet<CampoEntita> CampiEntita { get; set; }
        public DbSet<StatoEntita> StatiEntita { get; set; }
        public DbSet<RecordEntita> RecordsEntita { get; set; }
        public DbSet<ValoreCampoEntita> ValoriCampiEntita { get; set; }

        // Tabelle Attività Periodiche Dinamiche
        public DbSet<AttivitaPeriodica> AttivitaPeriodiche { get; set; }
        public DbSet<TipoPeriodo> TipiPeriodo { get; set; }
        public DbSet<CampoPeriodico> CampiPeriodici { get; set; }
        public DbSet<RegolaCampo> RegoleCampi { get; set; }
        public DbSet<ClienteAttivitaPeriodica> ClientiAttivitaPeriodiche { get; set; }
        public DbSet<ValorePeriodo> ValoriPeriodi { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Cognome).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).HasMaxLength(255);
            });

            // Permission configuration
            modelBuilder.Entity<Permission>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.PageUrl).IsUnique();
                entity.Property(e => e.PageName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PageUrl).IsRequired().HasMaxLength(255);
            });

            // UserPermission configuration
            modelBuilder.Entity<UserPermission>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.UserId, e.PermissionId }).IsUnique();

                entity.HasOne(e => e.User)
                    .WithMany(u => u.UserPermissions)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Permission)
                    .WithMany(p => p.UserPermissions)
                    .HasForeignKey(e => e.PermissionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // DynamicPage configuration
            modelBuilder.Entity<DynamicPage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.TableName).IsUnique();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
                entity.Property(e => e.TableName).IsRequired().HasMaxLength(100);
            });

            // DynamicField configuration
            modelBuilder.Entity<DynamicField>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Label).IsRequired().HasMaxLength(100);
                entity.Property(e => e.FieldType).IsRequired().HasMaxLength(50);

                entity.HasOne(e => e.DynamicPage)
                    .WithMany(p => p.Fields)
                    .HasForeignKey(e => e.DynamicPageId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // DynamicRecord configuration
            modelBuilder.Entity<DynamicRecord>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.DynamicPage)
                    .WithMany(p => p.Records)
                    .HasForeignKey(e => e.DynamicPageId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // DynamicFieldValue configuration
            modelBuilder.Entity<DynamicFieldValue>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.DynamicRecordId, e.DynamicFieldId }).IsUnique();

                entity.HasOne(e => e.DynamicRecord)
                    .WithMany(r => r.FieldValues)
                    .HasForeignKey(e => e.DynamicRecordId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.DynamicField)
                    .WithMany(f => f.FieldValues)
                    .HasForeignKey(e => e.DynamicFieldId)
                    .OnDelete(DeleteBehavior.NoAction); // Evita cicli di cascade
            });

            // ============ NUOVE CONFIGURAZIONI ============

            // Cliente configuration
            modelBuilder.Entity<Cliente>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.RagioneSociale).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Indirizzo).HasMaxLength(200);
                entity.Property(e => e.Citta).HasMaxLength(100);
                entity.Property(e => e.Provincia).HasMaxLength(2);
                entity.Property(e => e.CAP).HasMaxLength(5);
                entity.Property(e => e.Email).HasMaxLength(255);
                entity.Property(e => e.PEC).HasMaxLength(50);
                entity.Property(e => e.Telefono).HasMaxLength(20);
                entity.Property(e => e.CodiceFiscale).HasMaxLength(16);
                entity.Property(e => e.PartitaIVA).HasMaxLength(11);
            });

            // ClienteSoggetto configuration
            modelBuilder.Entity<ClienteSoggetto>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Cognome).IsRequired().HasMaxLength(100);
                entity.Property(e => e.CodiceFiscale).HasMaxLength(16);
                entity.Property(e => e.Indirizzo).HasMaxLength(200);
                entity.Property(e => e.Citta).HasMaxLength(100);
                entity.Property(e => e.Provincia).HasMaxLength(2);
                entity.Property(e => e.CAP).HasMaxLength(5);
                entity.Property(e => e.Email).HasMaxLength(255);
                entity.Property(e => e.Telefono).HasMaxLength(20);
                entity.Property(e => e.QuotaPercentuale).HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.Cliente)
                    .WithMany(c => c.Soggetti)
                    .HasForeignKey(e => e.ClienteId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // AnnualitaFiscale configuration
            modelBuilder.Entity<AnnualitaFiscale>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Anno).IsUnique();
                entity.Property(e => e.Anno).IsRequired();
                entity.Property(e => e.Descrizione).HasMaxLength(100);
            });

            // AttivitaTipo configuration
            modelBuilder.Entity<AttivitaTipo>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Descrizione).HasMaxLength(500);
                entity.Property(e => e.Icon).HasMaxLength(50);
            });

            // AttivitaCampo configuration
            modelBuilder.Entity<AttivitaCampo>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Label).IsRequired().HasMaxLength(100);

                entity.HasOne(e => e.AttivitaTipo)
                    .WithMany(t => t.Campi)
                    .HasForeignKey(e => e.AttivitaTipoId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // StatoAttivitaTipo configuration
            modelBuilder.Entity<StatoAttivitaTipo>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Icon).HasMaxLength(50);
                entity.Property(e => e.ColoreTesto).HasMaxLength(20);
                entity.Property(e => e.ColoreSfondo).HasMaxLength(20);

                entity.HasOne(e => e.AttivitaTipo)
                    .WithMany(t => t.Stati)
                    .HasForeignKey(e => e.AttivitaTipoId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.AttivitaTipoId, e.Nome }).IsUnique();
            });

            // AttivitaAnnuale configuration
            modelBuilder.Entity<AttivitaAnnuale>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.AttivitaTipoId, e.AnnualitaFiscaleId }).IsUnique();

                entity.HasOne(e => e.AttivitaTipo)
                    .WithMany(t => t.AttivitaAnnuali)
                    .HasForeignKey(e => e.AttivitaTipoId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.AnnualitaFiscale)
                    .WithMany(a => a.AttivitaAnnuali)
                    .HasForeignKey(e => e.AnnualitaFiscaleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ClienteAttivita configuration
            modelBuilder.Entity<ClienteAttivita>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.ClienteId, e.AttivitaAnnualeId }).IsUnique();

                entity.HasOne(e => e.Cliente)
                    .WithMany(c => c.Attivita)
                    .HasForeignKey(e => e.ClienteId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.AttivitaAnnuale)
                    .WithMany(a => a.ClientiAttivita)
                    .HasForeignKey(e => e.AttivitaAnnualeId)
                    .OnDelete(DeleteBehavior.NoAction); // Evita cicli

                entity.HasOne(e => e.StatoAttivitaTipoNav)
                    .WithMany()
                    .HasForeignKey(e => e.StatoAttivitaTipoId)
                    .OnDelete(DeleteBehavior.SetNull); // Se lo stato viene cancellato, imposta NULL
            });

            // ClienteAttivitaValore configuration
            modelBuilder.Entity<ClienteAttivitaValore>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.ClienteAttivitaId, e.AttivitaCampoId }).IsUnique();

                entity.HasOne(e => e.ClienteAttivita)
                    .WithMany(ca => ca.Valori)
                    .HasForeignKey(e => e.ClienteAttivitaId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.AttivitaCampo)
                    .WithMany()
                    .HasForeignKey(e => e.AttivitaCampoId)
                    .OnDelete(DeleteBehavior.NoAction); // Evita cicli
            });

            // ============ TABELLE FATTURAZIONE ============

            // MandatoCliente configuration
            modelBuilder.Entity<MandatoCliente>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.ClienteId, e.Anno }).IsUnique();
                entity.Property(e => e.ImportoAnnuo).HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.Cliente)
                    .WithMany()
                    .HasForeignKey(e => e.ClienteId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ScadenzaFatturazione configuration
            modelBuilder.Entity<ScadenzaFatturazione>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ImportoMandato).HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.MandatoCliente)
                    .WithMany(m => m.Scadenze)
                    .HasForeignKey(e => e.MandatoClienteId)
                    .OnDelete(DeleteBehavior.NoAction); // Evita cicli - gestito manualmente

                entity.HasOne(e => e.Cliente)
                    .WithMany()
                    .HasForeignKey(e => e.ClienteId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // SpesaPratica configuration
            modelBuilder.Entity<SpesaPratica>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Importo).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Descrizione).IsRequired().HasMaxLength(200);

                entity.HasOne(e => e.Cliente)
                    .WithMany()
                    .HasForeignKey(e => e.ClienteId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.ScadenzaFatturazione)
                    .WithMany(s => s.SpesePratiche)
                    .HasForeignKey(e => e.ScadenzaFatturazioneId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.Utente)
                    .WithMany()
                    .HasForeignKey(e => e.UtenteId)
                    .OnDelete(DeleteBehavior.NoAction); // Mantiene riferimento utente
            });

            // AccessoCliente configuration
            modelBuilder.Entity<AccessoCliente>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TariffaOraria).HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.Cliente)
                    .WithMany()
                    .HasForeignKey(e => e.ClienteId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.ScadenzaFatturazione)
                    .WithMany(s => s.AccessiClienti)
                    .HasForeignKey(e => e.ScadenzaFatturazioneId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.Utente)
                    .WithMany()
                    .HasForeignKey(e => e.UtenteId)
                    .OnDelete(DeleteBehavior.NoAction); // Mantiene riferimento utente
            });

            // FatturaCloud configuration
            modelBuilder.Entity<FatturaCloud>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.ClienteId, e.Anno }).IsUnique();
                entity.Property(e => e.Importo).HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.Cliente)
                    .WithMany()
                    .HasForeignKey(e => e.ClienteId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.ScadenzaFatturazione)
                    .WithMany(s => s.FattureCloud)
                    .HasForeignKey(e => e.ScadenzaFatturazioneId)
                    .OnDelete(DeleteBehavior.NoAction); // Evita cicli
            });

            // BilancioCEE configuration
            modelBuilder.Entity<BilancioCEE>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.ClienteId, e.Anno }).IsUnique();
                entity.Property(e => e.Importo).HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.Cliente)
                    .WithMany()
                    .HasForeignKey(e => e.ClienteId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.ScadenzaFatturazione)
                    .WithMany(s => s.BilanciCEE)
                    .HasForeignKey(e => e.ScadenzaFatturazioneId)
                    .OnDelete(DeleteBehavior.NoAction); // Evita cicli
            });

            // ContatoreDocumento configuration
            modelBuilder.Entity<ContatoreDocumento>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.Anno, e.TipoDocumento }).IsUnique();
            });

            // IncassoFattura configuration
            modelBuilder.Entity<IncassoFattura>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ImportoIncassato).HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.ScadenzaFatturazione)
                    .WithMany(s => s.Incassi)
                    .HasForeignKey(e => e.ScadenzaFatturazioneId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // IncassoProfessionista configuration
            modelBuilder.Entity<IncassoProfessionista>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Percentuale).HasColumnType("decimal(5,2)");
                entity.Property(e => e.Importo).HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.IncassoFattura)
                    .WithMany(i => i.SuddivisioneProfessionisti)
                    .HasForeignKey(e => e.IncassoFatturaId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Utente)
                    .WithMany()
                    .HasForeignKey(e => e.UtenteId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // ============ TABELLE BUDGET STUDIO ============

            modelBuilder.Entity<MacroVoceBudget>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Codice).IsUnique();
                entity.Property(e => e.Codice).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Descrizione).IsRequired().HasMaxLength(200);
            });

            modelBuilder.Entity<VoceSpesaBudget>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.CodiceSpesa).IsUnique();
                entity.Property(e => e.CodiceSpesa).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Descrizione).IsRequired().HasMaxLength(200);
                entity.Property(e => e.NoteDefault).HasMaxLength(500);

                entity.HasOne(e => e.MacroVoce)
                    .WithMany(m => m.VociAnalitiche)
                    .HasForeignKey(e => e.MacroVoceBudgetId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<BudgetSpesaMensile>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.VoceSpesaBudgetId, e.Anno, e.Mese }).IsUnique();
                entity.Property(e => e.Importo).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Note).HasMaxLength(500);

                entity.HasOne(e => e.VoceSpesaBudget)
                    .WithMany(v => v.BudgetMensile)
                    .HasForeignKey(e => e.VoceSpesaBudgetId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<BancaBudget>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nome).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Iban).HasMaxLength(50);
            });

            modelBuilder.Entity<SaldoBancaMese>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.BancaBudgetId, e.Anno, e.Mese }).IsUnique();
                entity.Property(e => e.Saldo).HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.BancaBudget)
                    .WithMany(b => b.SaldiMensili)
                    .HasForeignKey(e => e.BancaBudgetId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ============ SEED DATA ============

            // Seed admin user (password: 123456)
            modelBuilder.Entity<User>().HasData(new User
            {
                Id = 1,
                Username = "admin",
                PasswordHash = "8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92",
                Nome = "Amministratore",
                Cognome = "Sistema",
                Email = "admin@studiocg.it",
                IsActive = true,
                IsAdmin = true,
                DataCreazione = new DateTime(2024, 1, 1)
            });

            // Seed default permissions
            modelBuilder.Entity<Permission>().HasData(
                new Permission
                {
                    Id = 1,
                    PageName = "Dashboard",
                    PageUrl = "/Home",
                    Description = "Pagina principale",
                    Icon = "fas fa-home",
                    DisplayOrder = 1,
                    ShowInMenu = true
                },
                new Permission
                {
                    Id = 2,
                    PageName = "Gestione Utenti",
                    PageUrl = "/Users",
                    Description = "Gestione utenti del sistema",
                    Icon = "fas fa-users",
                    DisplayOrder = 2,
                    ShowInMenu = true
                },
                // Nuovi permessi per Anagrafica (ID alti per evitare conflitti)
                new Permission
                {
                    Id = 100,
                    PageName = "Clienti",
                    PageUrl = "/Clienti",
                    Description = "Gestione anagrafica clienti",
                    Icon = "fas fa-building",
                    Category = "ANAGRAFICA",
                    DisplayOrder = 10,
                    ShowInMenu = true
                },
                new Permission
                {
                    Id = 101,
                    PageName = "Annualità Fiscali",
                    PageUrl = "/Annualita",
                    Description = "Gestione annualità fiscali",
                    Icon = "fas fa-calendar-alt",
                    Category = "ANAGRAFICA",
                    DisplayOrder = 11,
                    ShowInMenu = true
                },
                new Permission
                {
                    Id = 102,
                    PageName = "Tipi Attività",
                    PageUrl = "/AttivitaTipi",
                    Description = "Gestione tipi attività",
                    Icon = "fas fa-cogs",
                    Category = "ANAGRAFICA",
                    DisplayOrder = 12,
                    ShowInMenu = true
                },
                // ============ PERMESSI AMMINISTRAZIONE ============
                new Permission
                {
                    Id = 200,
                    PageName = "Dashboard Fatturazione",
                    PageUrl = "/Amministrazione",
                    Description = "Dashboard riepilogo fatturazione",
                    Icon = "fas fa-chart-line",
                    Category = "AMMINISTRAZIONE",
                    DisplayOrder = 20,
                    ShowInMenu = true
                },
                new Permission
                {
                    Id = 201,
                    PageName = "Mandati Clienti",
                    PageUrl = "/Amministrazione/Mandati",
                    Description = "Gestione mandati professionali",
                    Icon = "fas fa-file-contract",
                    Category = "AMMINISTRAZIONE",
                    DisplayOrder = 21,
                    ShowInMenu = true
                },
                new Permission
                {
                    Id = 202,
                    PageName = "Scadenze Fatturazione",
                    PageUrl = "/Amministrazione/Scadenze",
                    Description = "Gestione scadenze e fatturazione",
                    Icon = "fas fa-file-invoice-dollar",
                    Category = "AMMINISTRAZIONE",
                    DisplayOrder = 22,
                    ShowInMenu = true
                },
                new Permission
                {
                    Id = 203,
                    PageName = "Spese Pratiche",
                    PageUrl = "/Amministrazione/SpesePratiche",
                    Description = "Gestione spese pratiche mensili",
                    Icon = "fas fa-receipt",
                    Category = "AMMINISTRAZIONE",
                    DisplayOrder = 23,
                    ShowInMenu = true
                },
                new Permission
                {
                    Id = 204,
                    PageName = "Accessi Clienti",
                    PageUrl = "/Amministrazione/AccessiClienti",
                    Description = "Registrazione accessi clienti",
                    Icon = "fas fa-door-open",
                    Category = "AMMINISTRAZIONE",
                    DisplayOrder = 24,
                    ShowInMenu = true
                },
                new Permission
                {
                    Id = 205,
                    PageName = "Fatture in Cloud",
                    PageUrl = "/Amministrazione/FattureCloud",
                    Description = "Gestione Fatture in Cloud",
                    Icon = "fas fa-cloud",
                    Category = "AMMINISTRAZIONE",
                    DisplayOrder = 25,
                    ShowInMenu = true
                },
                new Permission
                {
                    Id = 206,
                    PageName = "Bilanci CEE",
                    PageUrl = "/Amministrazione/BilanciCEE",
                    Description = "Gestione Bilanci CEE",
                    Icon = "fas fa-balance-scale",
                    Category = "AMMINISTRAZIONE",
                    DisplayOrder = 26,
                    ShowInMenu = true
                },
                new Permission
                {
                    Id = 207,
                    PageName = "Incassi",
                    PageUrl = "/Amministrazione/Incassi",
                    Description = "Gestione incassi fatture",
                    Icon = "fas fa-money-bill-wave",
                    Category = "AMMINISTRAZIONE",
                    DisplayOrder = 27,
                    ShowInMenu = true
                },
                new Permission
                {
                    Id = 208,
                    PageName = "Report Professionisti",
                    PageUrl = "/Amministrazione/ReportProfessionisti",
                    Description = "Report incassi per professionista",
                    Icon = "fas fa-user-tie",
                    Category = "AMMINISTRAZIONE",
                    DisplayOrder = 28,
                    ShowInMenu = true
                },
                new Permission
                {
                    Id = 209,
                    PageName = "Gestione Anni",
                    PageUrl = "/Amministrazione/GestioneAnni",
                    Description = "Gestione anni di fatturazione",
                    Icon = "fas fa-calendar-alt",
                    Category = "AMMINISTRAZIONE",
                    DisplayOrder = 29,
                    ShowInMenu = true
                },
                new Permission
                {
                    Id = 210,
                    PageName = "Budget Studio",
                    PageUrl = "/BudgetStudio",
                    Description = "Budget Studio - pianificazione spese mensili",
                    Icon = "fas fa-coins",
                    Category = null,
                    DisplayOrder = 30,
                    ShowInMenu = true
                },
                // Permessi Documenti
                new Permission
                {
                    Id = 301,
                    PageName = "Documenti",
                    PageUrl = "/Documenti",
                    Description = "Sistema gestione documenti e template",
                    Icon = "fas fa-file-alt",
                    Category = "DOCUMENTI",
                    DisplayOrder = 40,
                    ShowInMenu = true
                },
                new Permission
                {
                    Id = 302,
                    PageName = "Impostazioni Studio",
                    PageUrl = "/Documenti/ImpostazioniStudio",
                    Description = "Configurazione dati e logo studio",
                    Icon = "fas fa-building",
                    Category = "DOCUMENTI",
                    DisplayOrder = 41,
                    ShowInMenu = true
                },
                new Permission
                {
                    Id = 303,
                    PageName = "Clausole",
                    PageUrl = "/Documenti/Clausole",
                    Description = "Gestione clausole riutilizzabili",
                    Icon = "fas fa-paragraph",
                    Category = "DOCUMENTI",
                    DisplayOrder = 42,
                    ShowInMenu = true
                },
                new Permission
                {
                    Id = 304,
                    PageName = "Template",
                    PageUrl = "/Documenti/Template",
                    Description = "Gestione template documenti",
                    Icon = "fas fa-file-signature",
                    Category = "DOCUMENTI",
                    DisplayOrder = 43,
                    ShowInMenu = true
                },
                new Permission
                {
                    Id = 305,
                    PageName = "Genera Documento",
                    PageUrl = "/Documenti/Genera",
                    Description = "Genera documento da template",
                    Icon = "fas fa-file-export",
                    Category = "DOCUMENTI",
                    DisplayOrder = 44,
                    ShowInMenu = true
                },
                new Permission
                {
                    Id = 306,
                    PageName = "Archivio Documenti",
                    PageUrl = "/Documenti/Archivio",
                    Description = "Archivio documenti generati",
                    Icon = "fas fa-archive",
                    Category = "DOCUMENTI",
                    DisplayOrder = 45,
                    ShowInMenu = true
                },
                new Permission
                {
                    Id = 307,
                    PageName = "Controllo Antiriciclaggio",
                    PageUrl = "/Documenti/ControlloAntiriciclaggio",
                    Description = "Controllo scadenze documenti antiriciclaggio",
                    Icon = "fas fa-shield-alt",
                    Category = "DOCUMENTI",
                    DisplayOrder = 46,
                    ShowInMenu = true
                },
                // Permesso Scadenze Documenti Identità (in ANAGRAFICA)
                new Permission
                {
                    Id = 103,
                    PageName = "Scadenze Documenti Identità",
                    PageUrl = "/Clienti/ScadenzeDocumenti",
                    Description = "Gestione scadenze documenti identità soggetti",
                    Icon = "fas fa-id-card",
                    Category = "ANAGRAFICA",
                    DisplayOrder = 13,
                    ShowInMenu = true
                }
            );

            // Configurazione Sistema Documenti
            modelBuilder.Entity<ConfigurazioneStudio>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.NomeStudio).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Indirizzo).HasMaxLength(200);
                entity.Property(e => e.Citta).HasMaxLength(100);
                entity.Property(e => e.CAP).HasMaxLength(10);
                entity.Property(e => e.Provincia).HasMaxLength(50);
                entity.Property(e => e.PIVA).HasMaxLength(16);
                entity.Property(e => e.CF).HasMaxLength(16);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.PEC).HasMaxLength(100);
                entity.Property(e => e.Telefono).HasMaxLength(20);
            });

            modelBuilder.Entity<ClausolaDocumento>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Categoria).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Descrizione).HasMaxLength(500);
                entity.Property(e => e.Contenuto).IsRequired();
                entity.HasIndex(e => e.Categoria);
            });

            modelBuilder.Entity<TemplateDocumento>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Categoria).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Descrizione).HasMaxLength(500);
                entity.Property(e => e.Contenuto).IsRequired();
                entity.HasIndex(e => e.Categoria);
            });

            modelBuilder.Entity<DocumentoGenerato>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.NomeFile).IsRequired().HasMaxLength(200);
                entity.Property(e => e.ContentType).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Note).HasMaxLength(500);

                entity.HasOne(e => e.TemplateDocumento)
                    .WithMany(t => t.DocumentiGenerati)
                    .HasForeignKey(e => e.TemplateDocumentoId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Cliente)
                    .WithMany()
                    .HasForeignKey(e => e.ClienteId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.MandatoCliente)
                    .WithMany()
                    .HasForeignKey(e => e.MandatoClienteId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.GeneratoDa)
                    .WithMany()
                    .HasForeignKey(e => e.GeneratoDaUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.ClienteId);
                entity.HasIndex(e => e.GeneratoIl);
            });

            // ============ CAMPI CUSTOM CLIENTE ============
            modelBuilder.Entity<CampoCustomCliente>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Label).IsRequired().HasMaxLength(100);
                entity.Property(e => e.TipoCampo).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Options).HasMaxLength(500);
                entity.Property(e => e.DefaultValue).HasMaxLength(200);
                entity.Property(e => e.Placeholder).HasMaxLength(200);
                entity.HasIndex(e => e.Nome).IsUnique();
            });

            modelBuilder.Entity<ValoreCampoCustomCliente>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.ClienteId, e.CampoCustomClienteId }).IsUnique();

                entity.HasOne(e => e.Cliente)
                    .WithMany()
                    .HasForeignKey(e => e.ClienteId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.CampoCustom)
                    .WithMany(c => c.Valori)
                    .HasForeignKey(e => e.CampoCustomClienteId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ============ MENU DINAMICO ============
            modelBuilder.Entity<VoceMenu>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Url).HasMaxLength(255);
                entity.Property(e => e.Icon).HasMaxLength(50);
                entity.Property(e => e.Categoria).HasMaxLength(50);
                entity.Property(e => e.TipoVoce).HasMaxLength(50);

                entity.HasOne(e => e.Parent)
                    .WithMany(e => e.Children)
                    .HasForeignKey(e => e.ParentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ConfigurazioneMenuUtente>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.UserId, e.VoceMenuId }).IsUnique();

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.VoceMenu)
                    .WithMany()
                    .HasForeignKey(e => e.VoceMenuId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ============ ENTITÀ DINAMICHE ============
            modelBuilder.Entity<EntitaDinamica>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
                entity.Property(e => e.NomePluruale).HasMaxLength(100);
                entity.Property(e => e.Descrizione).HasMaxLength(500);
                entity.Property(e => e.Icon).HasMaxLength(50);
                entity.Property(e => e.Colore).HasMaxLength(20);
                entity.HasIndex(e => e.Nome).IsUnique();
            });

            modelBuilder.Entity<CampoEntita>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Label).IsRequired().HasMaxLength(100);
                entity.Property(e => e.TipoCampo).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Options).HasMaxLength(1000);
                entity.Property(e => e.DefaultValue).HasMaxLength(200);
                entity.Property(e => e.Placeholder).HasMaxLength(200);

                entity.HasOne(e => e.EntitaDinamica)
                    .WithMany(ed => ed.Campi)
                    .HasForeignKey(e => e.EntitaDinamicaId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.EntitaDinamicaId, e.Nome }).IsUnique();
            });

            modelBuilder.Entity<StatoEntita>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Icon).HasMaxLength(50);
                entity.Property(e => e.ColoreTesto).HasMaxLength(20);
                entity.Property(e => e.ColoreSfondo).HasMaxLength(20);

                entity.HasOne(e => e.EntitaDinamica)
                    .WithMany(ed => ed.Stati)
                    .HasForeignKey(e => e.EntitaDinamicaId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.EntitaDinamicaId, e.Nome }).IsUnique();
            });

            modelBuilder.Entity<RecordEntita>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Titolo).HasMaxLength(200);

                entity.HasOne(e => e.EntitaDinamica)
                    .WithMany(ed => ed.Records)
                    .HasForeignKey(e => e.EntitaDinamicaId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Cliente)
                    .WithMany()
                    .HasForeignKey(e => e.ClienteId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.StatoEntita)
                    .WithMany()
                    .HasForeignKey(e => e.StatoEntitaId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.EntitaDinamicaId);
                entity.HasIndex(e => e.ClienteId);
            });

            modelBuilder.Entity<ValoreCampoEntita>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.RecordEntitaId, e.CampoEntitaId }).IsUnique();

                entity.HasOne(e => e.RecordEntita)
                    .WithMany(r => r.Valori)
                    .HasForeignKey(e => e.RecordEntitaId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.CampoEntita)
                    .WithMany(c => c.Valori)
                    .HasForeignKey(e => e.CampoEntitaId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // ============ ATTIVITÀ PERIODICHE DINAMICHE ============
            modelBuilder.Entity<AttivitaPeriodica>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
                entity.Property(e => e.NomePlurale).HasMaxLength(100);
                entity.Property(e => e.Descrizione).HasMaxLength(500);
                entity.Property(e => e.Icona).HasMaxLength(50);
                entity.Property(e => e.Colore).HasMaxLength(20);
                entity.HasIndex(e => e.Nome).IsUnique();
            });

            modelBuilder.Entity<TipoPeriodo>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Icona).HasMaxLength(50);
                entity.Property(e => e.Colore).HasMaxLength(20);
                entity.Property(e => e.PercentualeInteressiDefault).HasColumnType("decimal(5,2)");

                entity.HasOne(e => e.AttivitaPeriodica)
                    .WithMany(a => a.TipiPeriodo)
                    .HasForeignKey(e => e.AttivitaPeriodicaId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.AttivitaPeriodicaId, e.Nome }).IsUnique();
            });

            modelBuilder.Entity<CampoPeriodico>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Label).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LabelPrimoPeriodo).HasMaxLength(100);
                entity.Property(e => e.TipoCampo).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Options).HasMaxLength(1000);
                entity.Property(e => e.DefaultValue).HasMaxLength(200);
                entity.Property(e => e.Placeholder).HasMaxLength(200);
                entity.Property(e => e.Formula).HasMaxLength(500);

                entity.HasOne(e => e.TipoPeriodo)
                    .WithMany(t => t.Campi)
                    .HasForeignKey(e => e.TipoPeriodoId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.TipoPeriodoId, e.Nome }).IsUnique();
            });

            modelBuilder.Entity<RegolaCampo>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TipoRegola).IsRequired().HasMaxLength(20);
                entity.Property(e => e.NomeRegola).HasMaxLength(100);
                entity.Property(e => e.CondizioneRiporto).HasMaxLength(50);
                entity.Property(e => e.Operatore).HasMaxLength(10);
                entity.Property(e => e.ValoreConfronto).HasMaxLength(100);
                entity.Property(e => e.ColoreTesto).HasMaxLength(20);
                entity.Property(e => e.ColoreSfondo).HasMaxLength(20);
                entity.Property(e => e.Icona).HasMaxLength(50);
                entity.Property(e => e.ApplicaA).HasMaxLength(20);

                entity.HasOne(e => e.CampoPeriodico)
                    .WithMany(c => c.Regole)
                    .HasForeignKey(e => e.CampoPeriodicoId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.CampoOrigine)
                    .WithMany()
                    .HasForeignKey(e => e.CampoOrigineId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.CampoDestinazione)
                    .WithMany()
                    .HasForeignKey(e => e.CampoDestinazioneId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<ClienteAttivitaPeriodica>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CodCoge).HasMaxLength(50);
                entity.Property(e => e.PercentualeInteressi).HasColumnType("decimal(5,2)");

                entity.HasOne(e => e.AttivitaPeriodica)
                    .WithMany(a => a.ClientiAssociati)
                    .HasForeignKey(e => e.AttivitaPeriodicaId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.TipoPeriodo)
                    .WithMany(t => t.ClientiAssociati)
                    .HasForeignKey(e => e.TipoPeriodoId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.Cliente)
                    .WithMany()
                    .HasForeignKey(e => e.ClienteId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.TipoPeriodoId, e.ClienteId, e.AnnoFiscale }).IsUnique();
            });

            modelBuilder.Entity<ValorePeriodo>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.ClienteAttivitaPeriodica)
                    .WithMany(c => c.ValoriPeriodi)
                    .HasForeignKey(e => e.ClienteAttivitaPeriodicaId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.ClienteAttivitaPeriodicaId, e.NumeroPeriodo }).IsUnique();
            });

            // Seed anno fiscale corrente
            modelBuilder.Entity<AnnualitaFiscale>().HasData(new AnnualitaFiscale
            {
                Id = 1,
                Anno = 2025,
                Descrizione = "Anno Fiscale 2025",
                IsActive = true,
                IsCurrent = true,
                CreatedAt = new DateTime(2025, 1, 1)
            });
        }
    }
}
