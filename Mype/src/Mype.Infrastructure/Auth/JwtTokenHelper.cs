using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Mype.Shared.Constants;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Text;

namespace Mype.Infrastructure.Auth
{
    [ExcludeFromCodeCoverage]
    public class JwtTokenHelper(IConfiguration configuration) : IJwtTokenHelper
    {
        private readonly IConfiguration _configuration = configuration;

        public string GenerateToken(int userId, string email, IDictionary<string, string> additionalClaims = null)
        {
            var secretKey =
                _configuration[Env.JwtSecretKeyStringKey]
                ?? throw new InvalidOperationException(
                    string.Format(ErrorMessages.VariableNotConfigured, Env.JwtSecretKeyStringKey));

            var issuer =
                _configuration[Env.JwtIssuerStringKey]
                ?? throw new InvalidOperationException(
                    string.Format(ErrorMessages.VariableNotConfigured, Env.JwtIssuerStringKey));

            var audience =
                _configuration[Env.JwtAudienceStringKey]
                ?? throw new InvalidOperationException(
                    string.Format(ErrorMessages.VariableNotConfigured, Env.JwtAudienceStringKey));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString(),
                [JwtRegisteredClaimNames.Sub] = userId.ToString(),
                [JwtRegisteredClaimNames.Email] = email,
                [ClaimTypes.NameIdentifier] = userId.ToString()
            };

            if (additionalClaims != null)
            {
                foreach (var claim in additionalClaims)
                {
                    claims[claim.Key] = claim.Value;
                }
            }
            
            var tokenExpiresInHours = 1;

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Claims = claims,
                Expires = DateTime.UtcNow.AddHours(tokenExpiresInHours),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = credentials
            };

            var tokenHandler = new JsonWebTokenHandler();

            return tokenHandler.CreateToken(tokenDescriptor);
        }
    }
}
