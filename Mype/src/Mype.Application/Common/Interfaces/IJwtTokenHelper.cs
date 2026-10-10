using System;
using Mype.Application.Common.Models;

namespace Mype.Application.Common.Interfaces
{
    public interface IJwtTokenHelper
    {
        JwtTokenResult GenerateToken(
            Guid userId,
            string email,
            string displayName,
            DateTimeOffset issuedAt
        );
    }
}
