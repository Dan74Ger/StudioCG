using System.Security.Cryptography;
using System.Text;

namespace StudioCG.Web.Services
{
    public interface IPasswordService
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string hash);
    }

    public class PasswordService : IPasswordService
    {
        public string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        public bool VerifyPassword(string password, string hash)
        {
            var passwordHash = HashPassword(password);
            return string.Equals(passwordHash, hash, StringComparison.OrdinalIgnoreCase);
        }
    }
}


