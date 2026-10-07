using Microsoft.Extensions.DependencyInjection;
using Mype.Application.Common.Interfaces;
using Mype.Application.Common.Normalizers;

namespace Mype.Application.Extensions
{
    public static class DependencyInjectionExtension
    {
        public static IServiceCollection AddApplication(
            this IServiceCollection services
        )
        {
            services.AddMediatR(x => x.RegisterServicesFromAssembly(typeof(DependencyInjectionExtension).Assembly));

            services.AddSingleton<IEmailNormalizer, EmailNormalizer>();

            return services;
        }
    }
}
