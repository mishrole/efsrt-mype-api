using System;
using System.Text.Json.Serialization;
using Mype.Application.Auth.Commands.Login;

namespace Mype.Application.Auth.Commands.RefreshSession
{
    public sealed class RefreshSessionResult
    {
        public string AccessToken { get; set; } = string.Empty;
        public string TokenType { get; set; } = string.Empty;
        public DateTimeOffset ExpiresAt { get; set; }
        public AuthenticatedUserResult User { get; set; } = new();

        [JsonIgnore]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonIgnore]
        public DateTimeOffset RefreshTokenExpiresAt { get; set; }
    }
}
