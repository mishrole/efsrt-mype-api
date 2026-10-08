using Mype.Application.Common.Models;
using System;

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
