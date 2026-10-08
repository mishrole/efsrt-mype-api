using Microsoft.AspNetCore.Http;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;

namespace Mype.Api.Context
{
    [ExcludeFromCodeCoverage]
    public class UserContextProvider : IUserContextProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserContextProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid GetCurrentUserId()
        {
            var user = _httpContextAccessor.HttpContext?.User;

            if (user?.Identity?.IsAuthenticated != true)
            {
                throw new UnauthorizedAccessException();
            }

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? user.FindFirstValue("sub")
                        ?? throw new UnauthorizedAccessException();

            if (!Guid.TryParse(userId, out var parsedUserId))
            {
                throw new UnauthorizedAccessException();
            }

            return parsedUserId;
        }

        public bool IsAuthenticated =>
            _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;

    }
}
