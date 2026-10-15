using Microsoft.Extensions.DependencyInjection;
using Mype.Application.Auth.Interfaces;
using Mype.Application.Common.Interfaces;
using Mype.Infrastructure.Auth.Repositories;

namespace Mype.Infrastructure.Auth
{
    public static class AuthSetup
    {
        public static IServiceCollection AddAuth(this IServiceCollection services)
        {
            services.AddSingleton<IJwtTokenHelper, JwtTokenHelper>();
            services.AddSingleton<IPasswordHasherHelper, PasswordHasherHelper>();
            services.AddSingleton<IRefreshTokenHelper, RefreshTokenHelper>();
            services.AddScoped<IAuthSessionRepository, AuthSessionRepository>();

            return services;
        }
    }
}
