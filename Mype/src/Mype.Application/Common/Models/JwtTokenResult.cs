using System;

namespace Mype.Application.Common.Models
{
    public class JwtTokenResult
    {
        public string Value { get; set; } = string.Empty;

        public string TokenType { get; set; } = string.Empty;

        public DateTimeOffset IssuedAt { get; set; }

        public DateTimeOffset ExpiresAt { get; set; }
    }
}