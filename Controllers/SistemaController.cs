using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using StudioCG.Web.Data;
using StudioCG.Web.Services;

namespace StudioCG.Web.Controllers
{
    [Authorize]
    public class SistemaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPermissionService _permissionService;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        // Percorso dove salvare i backup
        private const string BackupPath = @"c:\inetpub\backupdatabase";

        public SistemaController(
            ApplicationDbContext context, 
            IPermissionService permissionService,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            _context = context;
            _permissionService = permissionService;
            _configuration = configuration;
            _environment = environment;
        }

        private async Task<bool> CanAccessAsync()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return false;
            if (username.Equals("admin", StringComparison.OrdinalIgnoreCase)) return true;
            return await _permissionService.UserHasPermissionAsync(username, "/Sistema");
        }

        // GET: Sistema
        public async Task<IActionResult> Index()
        {
            if (!await CanAccessAsync())
                return RedirectToAction("AccessDenied", "Account");

            var backupList = GetBackupList();
            return View(backupList);
        }

        // POST: Sistema/BackupDatabase
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BackupDatabase()
        {
            if (!await CanAccessAsync())
                return RedirectToAction("AccessDenied", "Account");

            try
            {
                // Verifica che la cartella esista
                if (!Directory.Exists(BackupPath))
                {
                    try
                    {
                        Directory.CreateDirectory(BackupPath);
                    }
                    catch (Exception ex)
                    {
                        TempData["Error"] = $"Impossibile creare la cartella di backup: {ex.Message}";
                        return RedirectToAction(nameof(Index));
                    }
                }

                // Genera nome file con timestamp
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupFileName = $"StudioCG_{timestamp}.bak";
                var backupFilePath = Path.Combine(BackupPath, backupFileName);

                // Ottieni il nome del database dalla connection string
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                var builder = new SqlConnectionStringBuilder(connectionString);
                var databaseName = builder.InitialCatalog;

                // Esegui il backup usando ADO.NET diretto (EF Core non supporta BACKUP DATABASE)
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Comando BACKUP DATABASE (senza COMPRESSION per compatibilità SQL Express)
                    var backupQuery = $@"
                        BACKUP DATABASE [{databaseName}] 
                        TO DISK = @backupPath 
                        WITH FORMAT, 
                             MEDIANAME = 'StudioCG_Backup',
                             NAME = 'Backup completo di {databaseName}'";

                    using (var command = new SqlCommand(backupQuery, connection))
                    {
                        command.Parameters.AddWithValue("@backupPath", backupFilePath);
                        command.CommandTimeout = 300; // 5 minuti timeout

                        await command.ExecuteNonQueryAsync();
                    }
                }

                // Verifica che il file sia stato creato
                if (System.IO.File.Exists(backupFilePath))
                {
                    var fileInfo = new FileInfo(backupFilePath);
                    var sizeFormatted = FormatFileSize(fileInfo.Length);
                    TempData["Success"] = $"Backup completato con successo!<br/><strong>File:</strong> {backupFileName}<br/><strong>Dimensione:</strong> {sizeFormatted}";
                }
                else
                {
                    TempData["Success"] = $"Backup completato: {backupFileName}";
                }
            }
            catch (SqlException ex)
            {
                TempData["Error"] = $"Errore SQL durante il backup: {ex.Message}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore durante il backup: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Sistema/DownloadBackup
        public async Task<IActionResult> DownloadBackup(string fileName)
        {
            if (!await CanAccessAsync())
                return RedirectToAction("AccessDenied", "Account");

            if (string.IsNullOrEmpty(fileName))
                return NotFound();

            // Sicurezza: verifica che il nome file non contenga path traversal
            if (fileName.Contains("..") || fileName.Contains("/") || fileName.Contains("\\"))
                return BadRequest("Nome file non valido");

            var filePath = Path.Combine(BackupPath, fileName);

            if (!System.IO.File.Exists(filePath))
            {
                TempData["Error"] = "File di backup non trovato.";
                return RedirectToAction(nameof(Index));
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, "application/octet-stream", fileName);
        }

        // POST: Sistema/DeleteBackup
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBackup(string fileName)
        {
            if (!await CanAccessAsync())
                return RedirectToAction("AccessDenied", "Account");

            if (string.IsNullOrEmpty(fileName))
                return NotFound();

            // Sicurezza: verifica che il nome file non contenga path traversal
            if (fileName.Contains("..") || fileName.Contains("/") || fileName.Contains("\\"))
                return BadRequest("Nome file non valido");

            var filePath = Path.Combine(BackupPath, fileName);

            if (!System.IO.File.Exists(filePath))
            {
                TempData["Error"] = "File di backup non trovato.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                System.IO.File.Delete(filePath);
                TempData["Success"] = $"Backup '{fileName}' eliminato con successo.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Errore durante l'eliminazione: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        private List<BackupFileInfo> GetBackupList()
        {
            var backups = new List<BackupFileInfo>();

            if (!Directory.Exists(BackupPath))
                return backups;

            var files = Directory.GetFiles(BackupPath, "*.bak")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime);

            foreach (var file in files)
            {
                backups.Add(new BackupFileInfo
                {
                    FileName = file.Name,
                    CreatedAt = file.CreationTime,
                    Size = file.Length,
                    SizeFormatted = FormatFileSize(file.Length)
                });
            }

            return backups;
        }

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;
            
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size = size / 1024;
            }

            return $"{size:0.##} {sizes[order]}";
        }
    }

    public class BackupFileInfo
    {
        public string FileName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public long Size { get; set; }
        public string SizeFormatted { get; set; } = string.Empty;
    }
}
