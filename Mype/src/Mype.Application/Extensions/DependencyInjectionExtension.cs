using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Mype.Application.Common.Behaviors;
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
            services.AddMediatR(configuration =>
            {
                configuration.RegisterServicesFromAssembly(typeof(DependencyInjectionExtension).Assembly);

                configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });

            services.AddValidatorsFromAssembly(typeof(DependencyInjectionExtension).Assembly);

            services.AddSingleton<IEmailNormalizer, EmailNormalizer>();

            return services;
        }
    }
}
