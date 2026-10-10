using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Mype.Application.Auth.Commands.Login;
using Mype.Application.Businesses.Interfaces;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.BusinessRoles.Interfaces;
using Mype.Application.Categories.Interfaces;
using Mype.Application.Common;
using Mype.Application.Common.Interfaces;
using Mype.Application.Currencies.Interfaces;
using Mype.Application.Users.Interfaces;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.BusinessRoles.Constants;
using Mype.Domain.Categories;

namespace Mype.Application.Businesses.Commands.CreateBusiness
{
    public class CreateBusinessCommandHandler
        : IRequestHandler<CreateBusinessCommand, Result<CreateBusinessResult>>
    {
        private readonly IUserRepository _userRepository;
        private readonly ICurrencyRepository _currencyRepository;
        private readonly IBusinessRoleRepository _roleRepository;
        private readonly IBusinessRepository _businessRepository;
        private readonly IBusinessMembershipRepository _membershipRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IDefaultCategoryProvider _defaultCategoryProvider;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClock _clock;

        public CreateBusinessCommandHandler(
            IUserRepository userRepository,
            ICurrencyRepository currencyRepository,
            IBusinessRoleRepository roleRepository,
            IBusinessRepository businessRepository,
            IBusinessMembershipRepository membershipRepository,
            ICategoryRepository categoryRepository,
            IDefaultCategoryProvider defaultCategoryProvider,
            IUnitOfWork unitOfWork,
            IClock clock
        )
        {
            _userRepository = userRepository;
            _currencyRepository = currencyRepository;
            _roleRepository = roleRepository;
            _businessRepository = businessRepository;
            _membershipRepository = membershipRepository;
            _categoryRepository = categoryRepository;
            _defaultCategoryProvider = defaultCategoryProvider;
            _unitOfWork = unitOfWork;
            _clock = clock;
        }

        public async Task<Result<CreateBusinessResult>> Handle(
            CreateBusinessCommand request,
            CancellationToken cancellationToken
        )
        {
            var user = await _userRepository.GetByIdAsync(request.CurrentUserId, cancellationToken);

            if (user == null || !user.IsActive())
            {
                return Result<CreateBusinessResult>.Failure(LoginErrors.AccountUnavailable);
            }

            var currencyCode = request.CurrencyCode.Trim().ToUpperInvariant();

            var currency = await _currencyRepository.GetActiveByCodeAsync(
                currencyCode,
                cancellationToken
            );

            if (currency == null)
            {
                return Result<CreateBusinessResult>.Failure(
                    CreateBusinessErrors.UnsupportedCurrency
                );
            }

            var ownerRole = await _roleRepository.GetActiveByCodeAsync(
                BusinessRoleConstants.OwnerCode,
                cancellationToken
            );

            if (ownerRole == null)
            {
                return Result<CreateBusinessResult>.Failure(
                    CreateBusinessErrors.SystemRoleUnavailable
                );
            }

            var normalizedRuc = string.IsNullOrWhiteSpace(request.Ruc) ? null : request.Ruc.Trim();

            if (
                normalizedRuc != null
                && await _businessRepository.ExistsByRucAsync(normalizedRuc, cancellationToken)
            )
            {
                return Result<CreateBusinessResult>.Failure(
                    CreateBusinessErrors.RucAlreadyRegistered
                );
            }

            var utcNow = _clock.UtcNow;

            var business = Business.Create(
                request.DisplayName,
                request.LegalName,
                normalizedRuc,
                currency.Id,
                request.CurrentUserId,
                utcNow
            );

            var membership = BusinessMembership.CreateOwner(
                business.Id,
                request.CurrentUserId,
                ownerRole.Id,
                utcNow
            );

            var categories = CreateDefaultCategories(business.Id, request.CurrentUserId, utcNow);

            await _businessRepository.AddAsync(business, cancellationToken);

            await _membershipRepository.AddAsync(membership, cancellationToken);

            foreach (var category in categories)
            {
                await _categoryRepository.AddAsync(category, cancellationToken);
            }

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return Result<CreateBusinessResult>.Failure(
                    CreateBusinessErrors.BusinessCreationFailed
                );
            }

            return Result<CreateBusinessResult>.Success(
                new CreateBusinessResult
                {
                    BusinessId = business.Id,
                    DisplayName = business.DisplayName,
                    CurrencyCode = currency.Code,
                    MembershipId = membership.Id,
                    RoleCode = ownerRole.Code,
                    DefaultCategories = categories
                        .Select(category => new CategoryResult
                        {
                            Id = category.Id,
                            Name = category.Name,
                            Type = category.Type,
                            IsDefault = category.IsDefault,
                            IsActive = category.IsActive,
                        })
                        .ToArray(),
                    Version = business.Version,
                }
            );
        }

        private Category[] CreateDefaultCategories(
            Guid businessId,
            Guid currentUserId,
            DateTimeOffset utcNow
        )
        {
            return _defaultCategoryProvider
                .GetDefaultCategories()
                .Select(definition =>
                    Category.CreateDefault(
                        businessId,
                        definition.Type,
                        definition.Name,
                        NormalizeCategoryName(definition.Name),
                        currentUserId,
                        utcNow
                    )
                )
                .ToArray();
        }

        private static string NormalizeCategoryName(string name)
        {
            return name.Trim().ToUpperInvariant();
        }
    }
}
