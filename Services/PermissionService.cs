using Microsoft.EntityFrameworkCore;
using StudioCG.Web.Data;
using StudioCG.Web.Models;

namespace StudioCG.Web.Services
{
    public interface IPermissionService
    {
        Task<bool> CanViewAsync(int userId, string pageUrl);
        Task<bool> CanEditAsync(int userId, string pageUrl);
        Task<bool> CanCreateAsync(int userId, string pageUrl);
        Task<bool> CanDeleteAsync(int userId, string pageUrl);
        Task<List<Permission>> GetUserMenuItemsAsync(int userId);
        Task<bool> IsAdminUserAsync(string username);
        Task<List<DynamicPage>> GetDynamicPagesAsync(string? category = null);
        Task<List<DynamicPage>> GetUserDynamicPagesAsync(string username, string? category = null);
        Task<int?> GetUserIdByUsernameAsync(string username);
        Task<List<AttivitaAnnuale>> GetUserAttivitaAsync(string username);
        Task<AnnualitaFiscale?> GetAnnoCorrenteAsync();
        Task<bool> UserHasPermissionAsync(string username, string pageUrl);
    }

    public class PermissionService : IPermissionService
    {
        private readonly ApplicationDbContext _context;

        public PermissionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<bool> IsAdminUserAsync(string username)
        {
            return Task.FromResult(string.Equals(username, "admin", StringComparison.OrdinalIgnoreCase));
        }

        public async Task<bool> CanViewAsync(int userId, string pageUrl)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            // Admin ha sempre accesso
            if (user.Username.ToLower() == "admin") return true;

            var permission = await GetUserPermission(userId, pageUrl);
            return permission?.CanView ?? false;
        }

        public async Task<bool> CanEditAsync(int userId, string pageUrl)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            // Admin ha sempre accesso
            if (user.Username.ToLower() == "admin") return true;

            var permission = await GetUserPermission(userId, pageUrl);
            return permission?.CanEdit ?? false;
        }

        public async Task<bool> CanCreateAsync(int userId, string pageUrl)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            // Admin ha sempre accesso
            if (user.Username.ToLower() == "admin") return true;

            var permission = await GetUserPermission(userId, pageUrl);
            return permission?.CanCreate ?? false;
        }

        public async Task<bool> CanDeleteAsync(int userId, string pageUrl)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            // Admin ha sempre accesso
            if (user.Username.ToLower() == "admin") return true;

            var permission = await GetUserPermission(userId, pageUrl);
            return permission?.CanDelete ?? false;
        }

        public async Task<List<Permission>> GetUserMenuItemsAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return new List<Permission>();

            // Admin vede tutte le pagine
            if (user.Username.ToLower() == "admin")
            {
                return await _context.Permissions
                    .Where(p => p.ShowInMenu)
                    .OrderBy(p => p.DisplayOrder)
                    .ToListAsync();
            }

            // Altri utenti vedono solo le pagine a cui hanno accesso CanView
            var userPermissionIds = await _context.UserPermissions
                .Where(up => up.UserId == userId && up.CanView)
                .Select(up => up.PermissionId)
                .ToListAsync();

            return await _context.Permissions
                .Where(p => p.ShowInMenu && userPermissionIds.Contains(p.Id))
                .OrderBy(p => p.DisplayOrder)
                .ToListAsync();
        }

        /// <summary>
        /// Ottiene le pagine dinamiche, opzionalmente filtrate per categoria
        /// </summary>
        public async Task<List<DynamicPage>> GetDynamicPagesAsync(string? category = null)
        {
            var query = _context.DynamicPages
                .Where(p => p.IsActive)
                .OrderBy(p => p.DisplayOrder)
                .ThenBy(p => p.Name);

            if (!string.IsNullOrEmpty(category))
            {
                query = (IOrderedQueryable<DynamicPage>)query.Where(p => p.Category == category);
            }

            return await query.ToListAsync();
        }

        /// <summary>
        /// Ottiene le pagine dinamiche filtrate per i permessi dell'utente
        /// </summary>
        public async Task<List<DynamicPage>> GetUserDynamicPagesAsync(string username, string? category = null)
        {
            // Admin vede tutte le pagine
            if (string.Equals(username, "admin", StringComparison.OrdinalIgnoreCase))
            {
                return await GetDynamicPagesAsync(category);
            }

            // Ottieni l'utente
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return new List<DynamicPage>();

            // Ottieni tutti i permessi dell'utente con CanView = true
            var userPermissions = await _context.UserPermissions
                .Include(up => up.Permission)
                .Where(up => up.UserId == user.Id && up.CanView)
                .Select(up => up.Permission.PageUrl)
                .ToListAsync();

            // Filtra le pagine dinamiche in base ai permessi
            var query = _context.DynamicPages
                .Where(p => p.IsActive)
                .OrderBy(p => p.DisplayOrder)
                .ThenBy(p => p.Name);

            if (!string.IsNullOrEmpty(category))
            {
                query = (IOrderedQueryable<DynamicPage>)query.Where(p => p.Category == category);
            }

            var allPages = await query.ToListAsync();

            // Filtra solo le pagine per cui l'utente ha il permesso
            return allPages.Where(p => userPermissions.Contains($"/DynamicData/Page/{p.Id}")).ToList();
        }

        /// <summary>
        /// Ottiene l'ID utente dal nome utente
        /// </summary>
        public async Task<int?> GetUserIdByUsernameAsync(string username)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            return user?.Id;
        }

        /// <summary>
        /// Ottiene le attività per l'anno corrente filtrate per i permessi dell'utente
        /// </summary>
        public async Task<List<AttivitaAnnuale>> GetUserAttivitaAsync(string username)
        {
            var annoCorrente = await GetAnnoCorrenteAsync();
            if (annoCorrente == null) return new List<AttivitaAnnuale>();

            var attivita = await _context.AttivitaAnnuali
                .Include(aa => aa.AttivitaTipo)
                .Where(aa => aa.AnnualitaFiscaleId == annoCorrente.Id && aa.IsActive)
                .OrderBy(aa => aa.AttivitaTipo!.DisplayOrder)
                .ThenBy(aa => aa.AttivitaTipo!.Nome)
                .ToListAsync();

            // Admin vede tutto
            if (string.Equals(username, "admin", StringComparison.OrdinalIgnoreCase))
            {
                return attivita;
            }

            // Altri utenti: filtra per permessi
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return new List<AttivitaAnnuale>();

            var userPermissions = await _context.UserPermissions
                .Include(up => up.Permission)
                .Where(up => up.UserId == user.Id && up.CanView)
                .Select(up => up.Permission.PageUrl)
                .ToListAsync();

            return attivita.Where(aa => userPermissions.Contains($"/Attivita/Tipo/{aa.AttivitaTipoId}")).ToList();
        }

        /// <summary>
        /// Ottiene l'anno fiscale corrente
        /// </summary>
        public async Task<AnnualitaFiscale?> GetAnnoCorrenteAsync()
        {
            return await _context.AnnualitaFiscali.FirstOrDefaultAsync(a => a.IsCurrent);
        }

        /// <summary>
        /// Verifica se l'utente ha il permesso per una specifica pagina URL
        /// </summary>
        public async Task<bool> UserHasPermissionAsync(string username, string pageUrl)
        {
            // Admin ha sempre accesso
            if (string.Equals(username, "admin", StringComparison.OrdinalIgnoreCase))
                return true;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return false;

            // Normalizza l'URL
            var normalizedUrl = pageUrl.TrimStart('/');
            var urlWithSlash = "/" + normalizedUrl;

            return await _context.UserPermissions
                .Include(up => up.Permission)
                .AnyAsync(up => 
                    up.UserId == user.Id && 
                    up.CanView &&
                    (up.Permission.PageUrl == pageUrl || 
                     up.Permission.PageUrl == normalizedUrl ||
                     up.Permission.PageUrl == urlWithSlash));
        }

        private async Task<UserPermission?> GetUserPermission(int userId, string pageUrl)
        {
            // Normalizza l'URL - prepara tutte le varianti possibili
            var normalizedUrl = pageUrl.TrimStart('/');
            var urlWithSlash = "/" + normalizedUrl;
            
            // Cerca il permesso usando varianti pre-calcolate (evita TrimStart nella query SQL)
            return await _context.UserPermissions
                .Include(up => up.Permission)
                .FirstOrDefaultAsync(up => 
                    up.UserId == userId && 
                    (up.Permission.PageUrl == pageUrl || 
                     up.Permission.PageUrl == normalizedUrl ||
                     up.Permission.PageUrl == urlWithSlash));
        }
    }
}
