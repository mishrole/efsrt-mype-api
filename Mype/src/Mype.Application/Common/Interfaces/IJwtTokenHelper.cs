using System.Collections.Generic;

namespace Mype.Application.Common.Interfaces
{
    public interface IJwtTokenHelper
    {
        string GenerateToken(int userId, string email, IDictionary<string, string> additionalClaims = null);
    }
}
