using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.Common;
using Mype.Application.Common.Exceptions;
using Mype.Application.Common.Interfaces;
using Mype.Application.Permissions.Interfaces;
using Mype.Application.ProductCategories.Interfaces;
using Mype.Domain.ProductCategories;

namespace Mype.Application.ProductCategories.Commands.CreateProductCategory
{
    public sealed class CreateProductCategoryCommandHandler
        : IRequestHandler<CreateProductCategoryCommand, Result<ProductCategoryResult>>
    {
        private readonly IBusinessMembershipRepository _membershipRepository;
        private readonly IPermissionRepository _permissionRepository;
        private readonly IProductCategoryRepository _productCategoryRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClock _clock;

        public CreateProductCategoryCommandHandler(
            IBusinessMembershipRepository membershipRepository,
            IPermissionRepository permissionRepository,
            IProductCategoryRepository productCategoryRepository,
            IUnitOfWork unitOfWork,
            IClock clock
        )
        {
            _membershipRepository = membershipRepository;
            _permissionRepository = permissionRepository;
            _productCategoryRepository = productCategoryRepository;
            _unitOfWork = unitOfWork;
            _clock = clock;
        }

        public async Task<Result<ProductCategoryResult>> Handle(
            CreateProductCategoryCommand request,
            CancellationToken cancellationToken
        )
        {
            var accessError = await ProductCategoryAccess.ValidateAsync(
                _membershipRepository,
                _permissionRepository,
                request.BusinessId,
                request.CurrentUserId,
                ProductCategoryPermissions.Create,
                cancellationToken
            );
            if (accessError != null)
            {
                return Result<ProductCategoryResult>.Failure(accessError);
            }
            var normalizedName = ProductCategoryNameNormalizer.NormalizeForComparison(request.Name);
            var existingProductCategory = await _productCategoryRepository.GetByNormalizedNameAsync(
                request.BusinessId,
                normalizedName,
                cancellationToken
            );
            if (existingProductCategory != null)
            {
                return Result<ProductCategoryResult>.Failure(
                    existingProductCategory.IsActive
                        ? ProductCategoryErrors.AlreadyExists
                        : ProductCategoryErrors.ReactivationRequired
                );
            }
            var normalizedColor = string.IsNullOrWhiteSpace(request.Color)
                ? ProductCategoryPalette.Neutral
                : request.Color.ToUpperInvariant();
            var productCategory = ProductCategory.Create(
                request.BusinessId,
                ProductCategoryNameNormalizer.NormalizeName(request.Name),
                normalizedName,
                normalizedColor,
                false,
                request.CurrentUserId,
                _clock.UtcNow
            );
            await _productCategoryRepository.AddAsync(productCategory, cancellationToken);
            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (ApplicationErrorException persistenceException)
            {
                return Result<ProductCategoryResult>.Failure(
                    ProductCategoryPersistenceErrorMapper.Map(persistenceException)
                );
            }
            catch
            {
                return Result<ProductCategoryResult>.Failure(
                    ProductCategoryErrors.PersistenceFailed
                );
            }
            return Result<ProductCategoryResult>.Success(
                ProductCategoryAccess.Map(productCategory)
            );
        }
    }
}
