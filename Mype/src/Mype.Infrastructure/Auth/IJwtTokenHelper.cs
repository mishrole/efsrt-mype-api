using System.Collections.Generic;

namespace Mype.Infrastructure.Auth
{
    public interface IJwtTokenHelper
    {
        string GenerateToken(int userId, string email, IDictionary<string, string> additionalClaims = null);
    }
}
