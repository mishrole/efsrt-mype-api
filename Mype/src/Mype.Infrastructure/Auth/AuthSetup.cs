using Microsoft.Extensions.DependencyInjection;

namespace Mype.Infrastructure.Auth
{
    public static class AuthSetup
    {
        public static IServiceCollection AddAuth(
            this IServiceCollection services
        )
        {
            services.AddSingleton<IJwtTokenHelper, JwtTokenHelper>();
            services.AddSingleton<IPasswordHasherHelper, PasswordHasherHelper>();

            return services;
        }
    }
}
