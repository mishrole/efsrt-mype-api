using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mype.Application.Businesses.Interfaces;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.BusinessRoles.Interfaces;
using Mype.Application.Categories.Interfaces;
using Mype.Application.Common.Interfaces;
using Mype.Application.Currencies.Interfaces;
using Mype.Application.FinancialMovements.Interfaces;
using Mype.Application.Permissions.Interfaces;
using Mype.Application.Products.Interfaces;
using Mype.Application.Users.Interfaces;
using Mype.Infrastructure.Auth;
using Mype.Infrastructure.Businesses.Repositories;
using Mype.Infrastructure.BusinessMemberships.Repositories;
using Mype.Infrastructure.BusinessRoles.Repositories;
using Mype.Infrastructure.Categories;
using Mype.Infrastructure.Categories.Repositories;
using Mype.Infrastructure.Common;
using Mype.Infrastructure.Currencies.Repositories;
using Mype.Infrastructure.FinancialMovements.Repositories;
using Mype.Infrastructure.Permissions.Repositories;
using Mype.Infrastructure.Persistence;
using Mype.Infrastructure.Persistence.Exceptions;
using Mype.Infrastructure.Products.Repositories;
using Mype.Infrastructure.Users.Repositories;

namespace Mype.Infrastructure.Extensions
{
    [ExcludeFromCodeCoverage]
    public static class DependencyInjectionExtension
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration
        )
        {
            services.AddPersistence(configuration);
            services.AddAuth();

            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ICurrencyRepository, CurrencyRepository>();
            services.AddScoped<IBusinessRoleRepository, BusinessRoleRepository>();
            services.AddScoped<IBusinessRepository, BusinessRepository>();
            services.AddScoped<IBusinessMembershipRepository, BusinessMembershipRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IPermissionRepository, PermissionRepository>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IFinancialMovementRepository, FinancialMovementRepository>();

            services.AddScoped<IUnitOfWork>(provider =>
                provider.GetRequiredService<MypeDbContext>()
            );

            services.AddSingleton<IClock, SystemClock>();
            services.AddSingleton<IPersistenceExceptionTranslator, PostgreSqlExceptionTranslator>();
            services.AddSingleton<IDefaultCategoryProvider, DefaultCategoryProvider>();

            return services;
        }
    }
}
