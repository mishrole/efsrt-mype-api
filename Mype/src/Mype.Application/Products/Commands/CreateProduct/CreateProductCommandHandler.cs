using MediatR;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.Categories.Interfaces;
using Mype.Application.Common;
using Mype.Application.Common.Exceptions;
using Mype.Application.Common.Interfaces;
using Mype.Application.Permissions.Interfaces;
using Mype.Application.Products.Interfaces;
using Mype.Application.Products.Normalizers;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.Categories;
using Mype.Domain.Permissions.Constants;
using Mype.Domain.Products;
using Mype.Shared.Constants;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mype.Application.Products.Commands.CreateProduct
{
    public sealed class CreateProductCommandHandler
        : IRequestHandler<
            CreateProductCommand,
            Result<CreateProductResult>
        >
    {
        private readonly
            IBusinessMembershipRepository
            _membershipRepository;

        private readonly
            IPermissionRepository
            _permissionRepository;

        private readonly
            ICategoryRepository
            _categoryRepository;

        private readonly
            IProductRepository
            _productRepository;

        private readonly IUnitOfWork
            _unitOfWork;

        private readonly IClock _clock;

        public CreateProductCommandHandler(
            IBusinessMembershipRepository
                membershipRepository,
            IPermissionRepository
                permissionRepository,
            ICategoryRepository
                categoryRepository,
            IProductRepository
                productRepository,
            IUnitOfWork unitOfWork,
            IClock clock
        )
        {
            _membershipRepository =
                membershipRepository;

            _permissionRepository =
                permissionRepository;

            _categoryRepository =
                categoryRepository;

            _productRepository =
                productRepository;

            _unitOfWork = unitOfWork;
            _clock = clock;
        }

        public async Task<
            Result<CreateProductResult>
        > Handle(
            CreateProductCommand request,
            CancellationToken cancellationToken
        )
        {
            var context =
                await _membershipRepository
                    .GetContextByBusinessAndUserAsync(
                        request.BusinessId,
                        request.CurrentUserId,
                        cancellationToken
                    );

            if (
                context == null ||
                context.MembershipStatus !=
                    BusinessMembershipStatus.Active ||
                !context.RoleIsActive
            )
            {
                return Failure(
                    CreateProductErrors
                        .BusinessAccessForbidden
                );
            }

            if (
                context.BusinessStatus !=
                BusinessStatus.Active
            )
            {
                return Failure(
                    CreateProductErrors
                        .BusinessUnavailable
                );
            }

            var permissions =
                await _permissionRepository
                    .ListActiveCodesByRoleIdAsync(
                        context.RoleId,
                        cancellationToken
                    );

            if (
                !permissions.Contains(
                    SystemPermissions
                        .ProductCreate.Code
                )
            )
            {
                return Failure(
                    CreateProductErrors
                        .ProductAccessForbidden
                );
            }

            var category =
                await _categoryRepository
                    .GetByIdAndBusinessAsync(
                        request.CategoryId,
                        request.BusinessId,
                        cancellationToken
                    );

            if (category == null)
            {
                return Failure(
                    CreateProductErrors
                        .CategoryNotFound
                );
            }

            if (
                category.Type !=
                CategoryType.Sale
            )
            {
                return Failure(
                    CreateProductErrors
                        .ProductCategoryMustBeSale
                );
            }

            if (!category.IsActive)
            {
                return Failure(
                    CreateProductErrors
                        .CategoryUnavailable
                );
            }

            var name =
                ProductNameNormalizer
                    .NormalizeName(
                        request.Name
                    );

            var normalizedName =
                ProductNameNormalizer
                    .NormalizeForComparison(
                        request.Name
                    );

            if (
                await _productRepository
                    .ExistsByBusinessAndNormalizedNameAsync(
                        request.BusinessId,
                        normalizedName,
                        cancellationToken
                    )
            )
            {
                return Failure(
                    CreateProductErrors
                        .ProductAlreadyExists
                );
            }

            var utcNow = _clock.UtcNow;

            var product = Product.Create(
                request.BusinessId,
                request.CategoryId,
                name,
                normalizedName,
                request.SalePrice,
                request.UnitCost,
                request.CurrentUserId,
                utcNow
            );

            await _productRepository.AddAsync(
                product,
                cancellationToken
            );

            try
            {
                await _unitOfWork.SaveChangesAsync(
                    cancellationToken
                );
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (
                ApplicationErrorException exception
            ) when (
                exception.Code ==
                ErrorCodes.ProductAlreadyExists
            )
            {
                return Failure(
                    CreateProductErrors
                        .ProductAlreadyExists
                );
            }
            catch
            {
                return Failure(
                    CreateProductErrors
                        .ProductCreationFailed
                );
            }

            return Result<
                CreateProductResult
            >.Success(
                new CreateProductResult
                {
                    Id = product.Id,
                    BusinessId =
                        product.BusinessId,
                    CategoryId =
                        product.CategoryId,
                    CategoryName =
                        category.Name,
                    Name = product.Name,
                    SalePrice =
                        product.SalePrice,
                    UnitCost =
                        product.UnitCost,
                    IsActive =
                        product.IsActive,
                    CreatedAt =
                        product.CreatedAt,
                    UpdatedAt =
                        product.UpdatedAt
                }
            );
        }

        private static Result<
            CreateProductResult
        > Failure(
            ApplicationError error
        )
        {
            return Result<
                CreateProductResult
            >.Failure(error);
        }
    }
}