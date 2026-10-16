using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Mype.Application.Auth.Interfaces;
using Mype.Application.Auth.Models;
using Mype.Shared.Constants;

namespace Mype.Infrastructure.Auth
{
    public sealed class RefreshTokenHelper : IRefreshTokenHelper
    {
        private readonly byte[] _pepper;

        public RefreshTokenHelper(IConfiguration configuration)
        {
            var pepper =
                configuration[Env.RefreshTokenPepperStringKey]
                ?? throw new InvalidOperationException(
                    string.Format(
                        ErrorMessages.VariableNotConfigured,
                        Env.RefreshTokenPepperStringKey
                    )
                );

            _pepper = Encoding.UTF8.GetBytes(pepper);
        }

        public RefreshTokenResult Generate(DateTimeOffset utcNow)
        {
            var tokenBytes = RandomNumberGenerator.GetBytes(AuthConstants.RefreshTokenSizeBytes);
            var value = Base64UrlEncode(tokenBytes);

            return new RefreshTokenResult(
                value,
                ComputeVerifier(value),
                utcNow.AddDays(AuthConstants.RefreshTokenIdleExpirationDays),
                utcNow.AddDays(AuthConstants.RefreshTokenAbsoluteExpirationDays)
            );
        }

        public string ComputeVerifier(string refreshToken)
        {
            using var hmac = new HMACSHA256(_pepper);
            var verifier = hmac.ComputeHash(Encoding.UTF8.GetBytes(refreshToken));
            return Convert.ToHexString(verifier);
        }

        private static string Base64UrlEncode(byte[] value)
        {
            return Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
    }
}
