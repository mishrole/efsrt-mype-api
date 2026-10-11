using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Mype.Application.Common.Interfaces;
using Mype.Shared.Constants;

namespace Mype.Tests.Integration
{
    public sealed class MypeWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly IReadOnlyDictionary<string, string> _testEnvironment;
        private readonly Dictionary<string, string> _originalEnvironment = new();

        public MypeWebApplicationFactory(string connectionString)
        {
            _testEnvironment = new Dictionary<string, string>
            {
                [Env.ConnectionStringKey] = connectionString,
                [Env.JwtSecretKeyStringKey] = "test-only-secret-key-with-at-least-32-characters",
                [Env.JwtIssuerStringKey] = "Mype.Tests",
                [Env.JwtAudienceStringKey] = "Mype.Tests",
                [Env.JwtExpirationMinutesStringKey] = "60",
                [Env.CorsPolicyNameStringKey] = "Mype.Tests",
                [Env.OriginsConfigurationStringKey] = "http://localhost",
                [Env.RetryCountStringKey] = "1",
                [Env.SleepDurationProviderStringKey] = "1",
                [Env.ServiceNameStringKey] = "Mype.Tests",
                [Env.TimeZoneStringKey] = "UTC",
                [Env.EvidenceStorageProviderStringKey] = "AzureBlob",
                [Env.EvidenceStorageServiceUriStringKey] = "https://storage.test",
                [Env.EvidenceStorageContainerStringKey] = "evidences",
                [Env.EvidenceMaxSizeBytesStringKey] = "10485760",
                [Env.EvidenceReadUriMinutesStringKey] = "5",
                [Env.EvidencePurgeRetentionDaysStringKey] = "30",
            };
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IObjectStorage>();
                services.AddSingleton<IObjectStorage, FakeObjectStorage>();
            });
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            ApplyTestEnvironment();

            try
            {
                return base.CreateHost(builder);
            }
            finally
            {
                RestoreEnvironment();
            }
        }

        private void ApplyTestEnvironment()
        {
            foreach (var variable in _testEnvironment)
            {
                _originalEnvironment[variable.Key] = Environment.GetEnvironmentVariable(
                    variable.Key
                );

                Environment.SetEnvironmentVariable(variable.Key, variable.Value);
            }
        }

        private void RestoreEnvironment()
        {
            foreach (var variable in _originalEnvironment)
            {
                Environment.SetEnvironmentVariable(variable.Key, variable.Value);
            }

            _originalEnvironment.Clear();
        }
    }
}
