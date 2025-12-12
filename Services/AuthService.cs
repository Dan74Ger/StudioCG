using Microsoft.EntityFrameworkCore;
using StudioCG.Web.Data;
using StudioCG.Web.Models;

namespace StudioCG.Web.Services
{
    public interface IAuthService
    {
        Task<User?> ValidateUserAsync(string username, string password);
        Task UpdateLastAccessAsync(int userId);
    }

    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordService _passwordService;

        public AuthService(ApplicationDbContext context, IPasswordService passwordService)
        {
            _context = context;
            _passwordService = passwordService;
        }

        public async Task<User?> ValidateUserAsync(string username, string password)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

            if (user == null)
                return null;

            if (!_passwordService.VerifyPassword(password, user.PasswordHash))
                return null;

            return user;
        }

        public async Task UpdateLastAccessAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.UltimoAccesso = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }
    }
}


