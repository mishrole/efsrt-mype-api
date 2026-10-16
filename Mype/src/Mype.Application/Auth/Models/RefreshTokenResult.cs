using System;

namespace Mype.Application.Auth.Models
{
    public sealed record RefreshTokenResult(
        string Value,
        string Verifier,
        DateTimeOffset IdleExpiresAt,
        DateTimeOffset AbsoluteExpiresAt
    );
}
