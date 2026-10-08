using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Mype.Application.Common.Interfaces;
using Mype.Application.Common.Models;
using Mype.Shared.Constants;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Mype.Infrastructure.Auth
{
    [ExcludeFromCodeCoverage]
    public class JwtTokenHelper(IConfiguration configuration) : IJwtTokenHelper
    {
        private const string TokenType = "Bearer";
        private readonly IConfiguration _configuration = configuration;

        public JwtTokenResult GenerateToken(
            Guid userId,
            string email,
            string displayName,
            DateTimeOffset issuedAt
        )
        {
            var secretKey = GetRequiredConfiguration(Env.JwtSecretKeyStringKey);

            var issuer = GetRequiredConfiguration(Env.JwtIssuerStringKey);

            var audience = GetRequiredConfiguration(Env.JwtAudienceStringKey);

            var expirationMinutes = GetExpirationMinutes();

            var expiresAt = issuedAt.AddMinutes(expirationMinutes);

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = userId.ToString(),
                [JwtRegisteredClaimNames.Email] = email,
                [JwtRegisteredClaimNames.Name] = displayName,
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString()
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Claims = claims,
                IssuedAt = issuedAt.UtcDateTime,
                Expires = expiresAt.UtcDateTime,
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = credentials
            };

            var tokenHandler = new JsonWebTokenHandler();

            return new JwtTokenResult
            {
                Value = tokenHandler.CreateToken(
                    tokenDescriptor
                ),
                TokenType = TokenType,
                IssuedAt = issuedAt,
                ExpiresAt = expiresAt
            };
        }

        private string GetRequiredConfiguration(
            string configurationKey
        )
        {
            return _configuration[configurationKey]
                ?? throw new InvalidOperationException(
                    string.Format(
                        ErrorMessages.VariableNotConfigured,
                        configurationKey
                    )
                );
        }

        private int GetExpirationMinutes()
        {
            var configuredValue = GetRequiredConfiguration(
                Env.JwtExpirationMinutesStringKey
            );

            if (
                !int.TryParse(
                    configuredValue,
                    out var expirationMinutes
                ) ||
                expirationMinutes <= 0
            )
            {
                throw new InvalidOperationException(
                    string.Format(
                        ErrorMessages.VariableNotValid,
                        Env.JwtExpirationMinutesStringKey
                    )
                );
            }

            return expirationMinutes;
        }
    }
}
