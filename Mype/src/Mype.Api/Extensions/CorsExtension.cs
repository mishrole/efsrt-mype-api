using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mype.Shared.Constants;
using System;
using System.Linq;

namespace Mype.Api.Extensions
{
    public static class CorsExtension
    {
        public static IServiceCollection AddCors(
            this IServiceCollection services,
            IConfiguration configuration,
            IHostEnvironment environment
        )
        {
            services.AddCors(options =>
            {
                options.AddPolicy(configuration[Env.CorsPolicyNameStringKey], policy =>
                {
                    var originsConfig = configuration[Env.OriginsConfigurationStringKey] ?? string.Empty;

                    if (!string.IsNullOrWhiteSpace(originsConfig))
                    {
                        var allowedOrigins = originsConfig
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(origin => origin.Trim())
                            .ToArray();

                        policy.WithOrigins(allowedOrigins)
                            .AllowAnyHeader()
                            .AllowAnyMethod();
                    }
                    else if (environment.IsDevelopment())
                    {
                        policy.AllowAnyOrigin()
                            .AllowAnyHeader()
                            .AllowAnyMethod();
                    }
                    else
                    {
                        throw new InvalidOperationException(string.Format(ErrorMessages.VariableRequiredInProduction, Env.OriginsConfigurationStringKey));
                    }
                });
            });

            return services;
        }
    }
}
