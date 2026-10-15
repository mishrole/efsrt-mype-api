using System;
using Mype.Application.Auth.Models;

namespace Mype.Application.Auth.Interfaces
{
    public interface IRefreshTokenHelper
    {
        RefreshTokenResult Generate(DateTimeOffset utcNow);
        string ComputeVerifier(string refreshToken);
    }
}
