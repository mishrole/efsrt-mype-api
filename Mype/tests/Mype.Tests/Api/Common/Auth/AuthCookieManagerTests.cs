using System;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Mype.Api.Common;
using Mype.Shared.Constants;

namespace Mype.Tests.Api.Common.Auth
{
    public class AuthCookieManagerTests
    {
        private const string RefreshToken = "refresh-token";
        private static readonly DateTimeOffset ExpiresAt = new(
            2026,
            10,
            22,
            12,
            0,
            0,
            TimeSpan.Zero
        );

        [Fact]
        public void WriteRefreshToken_Should_Write_Secure_HttpOnly_Cookie()
        {
            var context = new DefaultHttpContext();

            AuthCookieManager.WriteRefreshToken(context.Response, RefreshToken, ExpiresAt);

            var header = context.Response.Headers.SetCookie.ToString();
            header.Should().Contain(AuthConstants.RefreshTokenCookieName);
            header.Should().Contain(RefreshToken);
            header.Should().Contain("httponly");
            header.Should().Contain("secure");
            header.Should().Contain("samesite=none");
            header.Should().Contain($"path={AuthConstants.RefreshTokenCookiePath}");
        }

        [Fact]
        public void ReadRefreshToken_Should_Read_Configured_Cookie()
        {
            var context = new DefaultHttpContext();
            context.Request.Headers.Cookie =
                $"{AuthConstants.RefreshTokenCookieName}={RefreshToken}";

            var result = AuthCookieManager.ReadRefreshToken(context.Request);

            result.Should().Be(RefreshToken);
        }

        [Fact]
        public void DeleteRefreshToken_Should_Expire_Configured_Cookie()
        {
            var context = new DefaultHttpContext();

            AuthCookieManager.DeleteRefreshToken(context.Response);

            var header = context.Response.Headers.SetCookie.ToString();
            header.Should().Contain(AuthConstants.RefreshTokenCookieName);
            header.Should().Contain("expires=");
            header.Should().Contain("httponly");
            header.Should().Contain("secure");
            header.Should().Contain("samesite=none");
            header.Should().Contain($"path={AuthConstants.RefreshTokenCookiePath}");
        }
    }
}
