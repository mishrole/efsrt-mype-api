using System;
using Microsoft.AspNetCore.Http;
using Mype.Shared.Constants;

namespace Mype.Api.Common
{
    public static class AuthCookieManager
    {
        public static void WriteRefreshToken(
            HttpResponse response,
            string refreshToken,
            DateTimeOffset expiresAt
        )
        {
            response.Cookies.Append(
                AuthConstants.RefreshTokenCookieName,
                refreshToken,
                CreateCookieOptions(expiresAt)
            );
        }

        public static string ReadRefreshToken(HttpRequest request)
        {
            return request.Cookies[AuthConstants.RefreshTokenCookieName];
        }

        public static void DeleteRefreshToken(HttpResponse response)
        {
            response.Cookies.Delete(
                AuthConstants.RefreshTokenCookieName,
                CreateCookieOptions(DateTimeOffset.UnixEpoch)
            );
        }

        private static CookieOptions CreateCookieOptions(DateTimeOffset expiresAt)
        {
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = AuthConstants.RefreshTokenCookiePath,
                Expires = expiresAt,
                IsEssential = true,
            };
        }
    }
}
