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

        private async Task<UserPermission?> GetUserPermission(int userId, string pageUrl)
        {
            // Normalizza l'URL (rimuovi leading slash se presente)
            var normalizedUrl = pageUrl.TrimStart('/');
            
            return await _context.UserPermissions
                .Include(up => up.Permission)
                .FirstOrDefaultAsync(up => 
                    up.UserId == userId && 
                    (up.Permission.PageUrl == pageUrl || 
                     up.Permission.PageUrl == "/" + normalizedUrl ||
                     up.Permission.PageUrl.TrimStart('/') == normalizedUrl));
        }
    }
}

