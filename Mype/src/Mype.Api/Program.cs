using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Mype.Api;
using Mype.Api.Context;
using Mype.Api.Extensions;
using Mype.Application.Extensions;
using Mype.Infrastructure.Extensions;
using Mype.Infrastructure.Persistence;
using Mype.Shared.Constants;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;
using System;
using System.Text;

try
{
    // Logs before the host is built
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Service", "Mype.Api")
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Service} | CorrelationId={CorrelationId} | {Message:lj}{NewLine}{Exception}")
        .CreateLogger();

    var builder = WebApplication.CreateBuilder(args);

    // Logging
    builder.Host.UseSerilog((ctx, services, config) => config
    .ReadFrom.Configuration(ctx.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "Mype.Api")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Service} | CorrelationId={CorrelationId} | {Message:lj}{NewLine}{Exception}"));

    // Load environment variables from .env file in development (local)   
    if (builder.Environment.IsDevelopment())
    {
        Bootstrap.LoadEnvironmentVariables(builder.Environment.ContentRootPath);
    }

    // Add environment variables to the configuration
    builder.Configuration.AddEnvironmentVariables();

    // Configuration
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApplication();
    builder.Services.AddCors(builder.Configuration, builder.Environment);

    // Authentication
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration[Env.JwtIssuerStringKey],
                ValidAudience = builder.Configuration[Env.JwtAudienceStringKey],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(
                        builder.Configuration[Env.JwtSecretKeyStringKey] ?? throw new InvalidOperationException(
                            string.Format(ErrorMessages.VariableNotConfigured, Env.JwtSecretKeyStringKey)
                        )
                    )
                ),
                ClockSkew = TimeSpan.Zero
            };
        });

    builder.Services.AddAuthorization();

    builder.Services.AddHttpContextAccessor();

    // Add User context provider to add claims from the token to the handlers
    builder.Services.AddScoped<IUserContextProvider, UserContextProvider>();

    // Add services to the container for Swagger/OpenAPI
    builder.Services.AddEndpointsApiExplorer();

    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();

    // Add health checks
    builder.Services
        .AddHealthChecks()
        .AddDbContextCheck<MypeDbContext>();

    var app = builder.Build();

    // Migrations
    await app.Services.ApplyMigrationsAsync();

    // Middlewares
    app.AddMiddleware();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        // Enable OpenAPI
        app.MapOpenApi();

        // Enable Scalar API Reference
        app.MapScalarApiReference(options =>
        {
            options.WithTitle("Mype API").WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
        });
    }

    app.UseHttpsRedirection();
    app.UseCors(builder.Configuration[Env.CorsPolicyNameStringKey]);
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapHealthChecks("/health");
    app.AddEndpoints();

    await app.RunAsync();

}
catch (Exception ex)
{
    throw new InvalidOperationException(
        string.Format(ErrorMessages.StartupFailed, ex.GetType().ToString()),
        ex
    );
}
finally
{
    await Log.CloseAndFlushAsync();
}