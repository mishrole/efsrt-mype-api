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

namespace Mype.Application.ProductCategories.Commands.DeactivateProductCategory
{
    public sealed class DeactivateProductCategoryCommandHandler
        : IRequestHandler<DeactivateProductCategoryCommand, Result<ProductCategoryResult>>
    {
        private readonly IBusinessMembershipRepository _membershipRepository;
        private readonly IPermissionRepository _permissionRepository;
        private readonly IProductCategoryRepository _productCategoryRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClock _clock;

        public DeactivateProductCategoryCommandHandler(
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
            DeactivateProductCategoryCommand request,
            CancellationToken cancellationToken
        )
        {
            var accessError = await ProductCategoryAccess.ValidateAsync(
                _membershipRepository,
                _permissionRepository,
                request.BusinessId,
                request.CurrentUserId,
                ProductCategoryPermissions.Deactivate,
                cancellationToken
            );
            if (accessError != null)
            {
                return Result<ProductCategoryResult>.Failure(accessError);
            }
            var productCategory = await _productCategoryRepository.GetTrackedByIdAndBusinessAsync(
                request.ProductCategoryId,
                request.BusinessId,
                cancellationToken
            );
            if (productCategory == null)
            {
                return Result<ProductCategoryResult>.Failure(ProductCategoryErrors.NotFound);
            }
            if (!productCategory.IsActive)
            {
                return Result<ProductCategoryResult>.Failure(ProductCategoryErrors.AlreadyInactive);
            }
            _productCategoryRepository.SetOriginalVersion(productCategory, request.Version);
            productCategory.Deactivate(request.CurrentUserId, _clock.UtcNow);
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
