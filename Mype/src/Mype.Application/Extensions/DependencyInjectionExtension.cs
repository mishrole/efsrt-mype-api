using Microsoft.Extensions.DependencyInjection;

namespace Mype.Application.Extensions
{
    public static class DependencyInjectionExtension
    {
        public static IServiceCollection AddApplication(
            this IServiceCollection services
        )
        {
            services.AddMediatR(x => x.RegisterServicesFromAssembly(typeof(DependencyInjectionExtension).Assembly));

            return services;
        }
    }
}
