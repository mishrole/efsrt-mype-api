using Mype.Application.Common.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace Mype.Infrastructure.Auth
{
    [ExcludeFromCodeCoverage]
    public class PasswordHasherHelper : IPasswordHasherHelper
    {
        private const int WorkFactor = 11;

        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
        }

        public bool VerifyPassword(string hashedPassword, string providedPassword)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(providedPassword, hashedPassword);
            }
            catch
            {
                return false;
            }
        }
    }
}
