using System;
using System.Diagnostics.CodeAnalysis;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mype.Application.Common.Interfaces;
using Mype.Shared.Constants;

namespace Mype.Infrastructure.Storage
{
    [ExcludeFromCodeCoverage]
    public static class StorageSetup
    {
        public static IServiceCollection AddObjectStorage(
            this IServiceCollection services,
            IConfiguration configuration
        )
        {
            var options = ReadOptions(configuration);

            services.AddSingleton(options);
            services.AddSingleton(_ =>
                new BlobServiceClient(new Uri(options.ServiceUri), new DefaultAzureCredential())
            );
            services.AddSingleton(provider =>
                provider
                    .GetRequiredService<BlobServiceClient>()
                    .GetBlobContainerClient(options.ContainerName)
            );
            services.AddSingleton<IObjectStorage, AzureBlobObjectStorage>();

            return services;
        }

        private static EvidenceStorageOptions ReadOptions(IConfiguration configuration)
        {
            var provider = Required(configuration, Env.EvidenceStorageProviderStringKey);
            if (!string.Equals(provider, "AzureBlob", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    string.Format(
                        ErrorMessages.VariableNotValid,
                        Env.EvidenceStorageProviderStringKey
                    )
                );

            var serviceUri = Required(configuration, Env.EvidenceStorageServiceUriStringKey);
            if (
                !Uri.TryCreate(serviceUri, UriKind.Absolute, out var parsedServiceUri)
                || parsedServiceUri.Scheme != Uri.UriSchemeHttps
            )
                throw new InvalidOperationException(
                    string.Format(
                        ErrorMessages.VariableNotValid,
                        Env.EvidenceStorageServiceUriStringKey
                    )
                );

            return new EvidenceStorageOptions
            {
                Provider = provider,
                ServiceUri = parsedServiceUri.AbsoluteUri.TrimEnd('/'),
                ContainerName = Required(
                    configuration,
                    Env.EvidenceStorageContainerStringKey
                ),
                MaxSizeBytes = PositiveLong(
                    configuration,
                    Env.EvidenceMaxSizeBytesStringKey
                ),
                ReadUriMinutes = PositiveInt(
                    configuration,
                    Env.EvidenceReadUriMinutesStringKey
                ),
                PurgeRetentionDays = PositiveInt(
                    configuration,
                    Env.EvidencePurgeRetentionDaysStringKey
                ),
            };
        }

        private static string Required(IConfiguration configuration, string key) =>
            configuration[key]
            ?? throw new InvalidOperationException(
                string.Format(ErrorMessages.VariableNotConfigured, key)
            );

        private static int PositiveInt(IConfiguration configuration, string key)
        {
            var value = Required(configuration, key);
            if (!int.TryParse(value, out var parsed) || parsed <= 0)
                throw new InvalidOperationException(
                    string.Format(ErrorMessages.VariableNotValid, key)
                );

            return parsed;
        }

        private static long PositiveLong(IConfiguration configuration, string key)
        {
            var value = Required(configuration, key);
            if (!long.TryParse(value, out var parsed) || parsed <= 0)
                throw new InvalidOperationException(
                    string.Format(ErrorMessages.VariableNotValid, key)
                );

            return parsed;
        }
    }
}
