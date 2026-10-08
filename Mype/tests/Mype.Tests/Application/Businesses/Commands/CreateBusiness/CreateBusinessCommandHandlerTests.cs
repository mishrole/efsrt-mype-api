using FluentAssertions;
using Moq;
using Mype.Application.Auth.Commands.Login;
using Mype.Application.Businesses.Commands.CreateBusiness;
using Mype.Application.Businesses.Interfaces;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.BusinessRoles.Interfaces;
using Mype.Application.Categories.Interfaces;
using Mype.Application.Common.Interfaces;
using Mype.Application.Currencies.Interfaces;
using Mype.Application.Users.Interfaces;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.BusinessRoles;
using Mype.Domain.BusinessRoles.Constants;
using Mype.Domain.Categories;
using Mype.Domain.Currencies;
using Mype.Domain.Currencies.Constants;
using Mype.Domain.Users;
using Mype.Infrastructure.Categories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mype.Tests.Application.Businesses.Commands.CreateBusiness
{
    public class CreateBusinessCommandHandlerTests
    {
        private const string DisplayName = "Bodega Central";

        private const string LegalName =
            "Comercial Central E.I.R.L.";

        private const string Ruc = "20123456786";

        private const string Email =
            "usuario@dominio.com";

        private const string NormalizedEmail =
            "USUARIO@DOMINIO.COM";

        private const string PasswordHash =
            "password-hash";

        private const string UserDisplayName =
            "Usuario de prueba";

        private static readonly Guid CurrentUserId =
            Guid.NewGuid();

        private static readonly DateTimeOffset UtcNow =
            new(
                2026,
                10,
                8,
                12,
                0,
                0,
                TimeSpan.Zero
            );

        private readonly Mock<IUserRepository>
            _userRepositoryMock = new();

        private readonly Mock<ICurrencyRepository>
            _currencyRepositoryMock = new();

        private readonly Mock<IBusinessRoleRepository>
            _businessRoleRepositoryMock = new();

        private readonly Mock<IBusinessRepository>
            _businessRepositoryMock = new();

        private readonly Mock<IBusinessMembershipRepository>
            _membershipRepositoryMock = new();

        private readonly Mock<ICategoryRepository>
            _categoryRepositoryMock = new();

        private readonly Mock<IUnitOfWork>
            _unitOfWorkMock = new();

        private readonly Mock<IClock>
            _clockMock = new();

        private readonly IDefaultCategoryProvider
            _defaultCategoryProvider =
                new DefaultCategoryProvider();

        private readonly CreateBusinessCommandHandler _handler;

        public CreateBusinessCommandHandlerTests()
        {
            _clockMock
                .SetupGet(clock => clock.UtcNow)
                .Returns(UtcNow);

            _handler = new CreateBusinessCommandHandler(
                _userRepositoryMock.Object,
                _currencyRepositoryMock.Object,
                _businessRoleRepositoryMock.Object,
                _businessRepositoryMock.Object,
                _membershipRepositoryMock.Object,
                _categoryRepositoryMock.Object,
                _defaultCategoryProvider,
                _unitOfWorkMock.Object,
                _clockMock.Object
            );
        }

        [Fact]
        public async Task Handle_Should_Create_Business_Membership_And_Default_Categories_When_Request_Is_Valid()
        {
            var user = CreateActiveUser();
            var currency = CreateCurrency();
            var ownerRole = CreateOwnerRole();

            SetupSuccessfulDependencies(
                user,
                currency,
                ownerRole
            );

            Business createdBusiness = null;

            BusinessMembership createdMembership = null;

            var createdCategories = new List<Category>();

            CaptureCreatedEntities(
                business =>
                    createdBusiness = business,
                membership =>
                    createdMembership = membership,
                createdCategories
            );

            var result = await _handler.Handle(
                CreateCommand(),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();
            result.Error.Should().BeNull();
            result.Value.Should().NotBeNull();

            createdBusiness.Should().NotBeNull();
            createdMembership.Should().NotBeNull();

            AssertBusiness(
                createdBusiness,
                currency.Id,
                LegalName,
                Ruc
            );

            AssertMembership(
                createdMembership,
                createdBusiness.Id,
                ownerRole.Id
            );

            AssertDefaultCategories(
                createdCategories,
                createdBusiness.Id
            );

            AssertResult(
                result.Value,
                createdBusiness,
                createdMembership,
                createdCategories
            );

            VerifySuccessfulPersistence();
        }

        [Fact]
        public async Task Handle_Should_Create_Informal_Business_When_Optional_Data_Is_Not_Provided()
        {
            SetupSuccessfulDependencies(
                CreateActiveUser(),
                CreateCurrency(),
                CreateOwnerRole()
            );

            Business createdBusiness = null;

            _businessRepositoryMock
                .Setup(repository =>
                    repository.AddAsync(
                        It.IsAny<Business>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .Callback<Business, CancellationToken>(
                    (business, _) =>
                        createdBusiness = business
                )
                .Returns(Task.CompletedTask);

            var command = CreateCommand();

            command.LegalName = null;
            command.Ruc = null;

            var result = await _handler.Handle(
                command,
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();

            createdBusiness.Should().NotBeNull();

            createdBusiness.LegalName.Should().BeNull();
            createdBusiness.Ruc.Should().BeNull();

            _unitOfWorkMock.Verify(
                unitOfWork =>
                    unitOfWork.SaveChangesAsync(
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_Normalize_Currency_Code_Before_Querying()
        {
            SetupSuccessfulDependencies(
                CreateActiveUser(),
                CreateCurrency(),
                CreateOwnerRole()
            );

            var command = CreateCommand();

            command.CurrencyCode = "  pen  ";

            var result = await _handler.Handle(
                command,
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();

            _currencyRepositoryMock.Verify(
                repository =>
                    repository.GetActiveByCodeAsync(
                        CurrencyConstants.PenCode,
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_Query_Active_Owner_Role()
        {
            SetupSuccessfulDependencies(
                CreateActiveUser(),
                CreateCurrency(),
                CreateOwnerRole()
            );

            var result = await _handler.Handle(
                CreateCommand(),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();

            _businessRoleRepositoryMock.Verify(
                repository =>
                    repository.GetActiveByCodeAsync(
                        BusinessRoleConstants.OwnerCode,
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_Return_AccountUnavailable_When_User_Does_Not_Exist()
        {
            SetupUser(null);

            var result = await _handler.Handle(
                CreateCommand(),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeFalse();

            result.Error.Should().BeSameAs(
                LoginErrors.AccountUnavailable
            );

            _currencyRepositoryMock.Verify(
                repository =>
                    repository.GetActiveByCodeAsync(
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Never
            );

            VerifyNothingWasPersisted();
        }

        [Fact]
        public async Task Handle_Should_Return_AccountUnavailable_When_User_Is_Inactive()
        {
            var user = CreateActiveUser();

            user.Deactivate(
                UtcNow.AddMinutes(-1)
            );

            SetupUser(user);

            var result = await _handler.Handle(
                CreateCommand(),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeFalse();

            result.Error.Should().BeSameAs(
                LoginErrors.AccountUnavailable
            );

            _currencyRepositoryMock.Verify(
                repository =>
                    repository.GetActiveByCodeAsync(
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Never
            );

            VerifyNothingWasPersisted();
        }

        [Fact]
        public async Task Handle_Should_Return_UnsupportedCurrency_When_Currency_Does_Not_Exist()
        {
            SetupUser(
                CreateActiveUser()
            );

            _currencyRepositoryMock
                .Setup(repository =>
                    repository.GetActiveByCodeAsync(
                        CurrencyConstants.PenCode,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync((Currency)null);

            var result = await _handler.Handle(
                CreateCommand(),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeFalse();

            result.Error.Should().BeSameAs(
                CreateBusinessErrors.UnsupportedCurrency
            );

            _businessRoleRepositoryMock.Verify(
                repository =>
                    repository.GetActiveByCodeAsync(
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Never
            );

            VerifyNothingWasPersisted();
        }

        [Fact]
        public async Task Handle_Should_Return_SystemRoleUnavailable_When_Owner_Role_Does_Not_Exist()
        {
            SetupUser(
                CreateActiveUser()
            );

            SetupCurrency(
                CreateCurrency()
            );

            _businessRoleRepositoryMock
                .Setup(repository =>
                    repository.GetActiveByCodeAsync(
                        BusinessRoleConstants.OwnerCode,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync((BusinessRole)null);

            var result = await _handler.Handle(
                CreateCommand(),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeFalse();

            result.Error.Should().BeSameAs(
                CreateBusinessErrors.SystemRoleUnavailable
            );

            VerifyNothingWasPersisted();
        }

        [Fact]
        public async Task Handle_Should_Return_BusinessCreationFailed_When_SaveChanges_Fails()
        {
            SetupUser(
                CreateActiveUser()
            );

            SetupCurrency(
                CreateCurrency()
            );

            SetupOwnerRole(
                CreateOwnerRole()
            );

            SetupRepositoryAdds();

            _unitOfWorkMock
                .Setup(unitOfWork =>
                    unitOfWork.SaveChangesAsync(
                        It.IsAny<CancellationToken>()
                    )
                )
                .ThrowsAsync(
                    new InvalidOperationException(
                        "Persistence failed."
                    )
                );

            var result = await _handler.Handle(
                CreateCommand(),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeFalse();

            result.Error.Should().BeSameAs(
                CreateBusinessErrors.BusinessCreationFailed
            );

            _businessRepositoryMock.Verify(
                repository =>
                    repository.AddAsync(
                        It.IsAny<Business>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );

            _membershipRepositoryMock.Verify(
                repository =>
                    repository.AddAsync(
                        It.IsAny<BusinessMembership>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );

            _categoryRepositoryMock.Verify(
                repository =>
                    repository.AddAsync(
                        It.IsAny<Category>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Exactly(DefaultCategoryCount)
            );

            _unitOfWorkMock.Verify(
                unitOfWork =>
                    unitOfWork.SaveChangesAsync(
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_Propagate_OperationCanceledException()
        {
            SetupUser(
                CreateActiveUser()
            );

            SetupCurrency(
                CreateCurrency()
            );

            SetupOwnerRole(
                CreateOwnerRole()
            );

            SetupRepositoryAdds();

            _unitOfWorkMock
                .Setup(unitOfWork =>
                    unitOfWork.SaveChangesAsync(
                        It.IsAny<CancellationToken>()
                    )
                )
                .ThrowsAsync(
                    new OperationCanceledException()
                );

            var action = async () =>
                await _handler.Handle(
                    CreateCommand(),
                    CancellationToken.None
                );

            await action.Should()
                .ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task Handle_Should_Persist_Complete_Operation_With_One_SaveChanges_Call()
        {
            SetupSuccessfulDependencies(
                CreateActiveUser(),
                CreateCurrency(),
                CreateOwnerRole()
            );

            var result = await _handler.Handle(
                CreateCommand(),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();

            _businessRepositoryMock.Verify(
                repository =>
                    repository.AddAsync(
                        It.IsAny<Business>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );

            _membershipRepositoryMock.Verify(
                repository =>
                    repository.AddAsync(
                        It.IsAny<BusinessMembership>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );

            _categoryRepositoryMock.Verify(
                repository =>
                    repository.AddAsync(
                        It.IsAny<Category>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Exactly(DefaultCategoryCount)
            );

            _unitOfWorkMock.Verify(
                unitOfWork =>
                    unitOfWork.SaveChangesAsync(
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }

        private int DefaultCategoryCount =>
            _defaultCategoryProvider
                .GetDefaultCategories()
                .Count;

        private void SetupSuccessfulDependencies(
            User user,
            Currency currency,
            BusinessRole ownerRole
        )
        {
            SetupUser(user);
            SetupCurrency(currency);
            SetupOwnerRole(ownerRole);
            SetupRepositoryAdds();

            _unitOfWorkMock
                .Setup(unitOfWork =>
                    unitOfWork.SaveChangesAsync(
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(
                    DefaultCategoryCount + 2
                );
        }

        private void SetupUser(
            User user
        )
        {
            _userRepositoryMock
                .Setup(repository =>
                    repository.GetByIdAsync(
                        CurrentUserId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(user);
        }

        private void SetupCurrency(
            Currency currency
        )
        {
            _currencyRepositoryMock
                .Setup(repository =>
                    repository.GetActiveByCodeAsync(
                        CurrencyConstants.PenCode,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(currency);
        }

        private void SetupOwnerRole(
            BusinessRole ownerRole
        )
        {
            _businessRoleRepositoryMock
                .Setup(repository =>
                    repository.GetActiveByCodeAsync(
                        BusinessRoleConstants.OwnerCode,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(ownerRole);
        }

        private void SetupRepositoryAdds()
        {
            _businessRepositoryMock
                .Setup(repository =>
                    repository.AddAsync(
                        It.IsAny<Business>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .Returns(Task.CompletedTask);

            _membershipRepositoryMock
                .Setup(repository =>
                    repository.AddAsync(
                        It.IsAny<BusinessMembership>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .Returns(Task.CompletedTask);

            _categoryRepositoryMock
                .Setup(repository =>
                    repository.AddAsync(
                        It.IsAny<Category>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .Returns(Task.CompletedTask);
        }

        private void CaptureCreatedEntities(
            Action<Business> captureBusiness,
            Action<BusinessMembership> captureMembership,
            ICollection<Category> categories
        )
        {
            _businessRepositoryMock
                .Setup(repository =>
                    repository.AddAsync(
                        It.IsAny<Business>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .Callback<Business, CancellationToken>(
                    (business, _) =>
                        captureBusiness(business)
                )
                .Returns(Task.CompletedTask);

            _membershipRepositoryMock
                .Setup(repository =>
                    repository.AddAsync(
                        It.IsAny<BusinessMembership>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .Callback<
                    BusinessMembership,
                    CancellationToken
                >(
                    (membership, _) =>
                        captureMembership(membership)
                )
                .Returns(Task.CompletedTask);

            _categoryRepositoryMock
                .Setup(repository =>
                    repository.AddAsync(
                        It.IsAny<Category>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .Callback<Category, CancellationToken>(
                    (category, _) =>
                        categories.Add(category)
                )
                .Returns(Task.CompletedTask);
        }

        private static void AssertBusiness(
            Business business,
            Guid currencyId,
            string legalName,
            string ruc
        )
        {
            business.DisplayName.Should().Be(
                DisplayName
            );

            business.LegalName.Should().Be(
                legalName
            );

            business.Ruc.Should().Be(ruc);

            business.CurrencyId.Should().Be(
                currencyId
            );

            business.Status.Should().Be(
                BusinessStatus.Active
            );

            business.IsActive().Should().BeTrue();

            business.CreatedByUserId.Should().Be(
                CurrentUserId
            );

            business.UpdatedByUserId.Should().Be(
                CurrentUserId
            );

            business.CreatedAt.Should().Be(UtcNow);
            business.UpdatedAt.Should().Be(UtcNow);
            business.DeactivatedAt.Should().BeNull();
        }

        private static void AssertMembership(
            BusinessMembership membership,
            Guid businessId,
            Guid roleId
        )
        {
            membership.BusinessId.Should().Be(
                businessId
            );

            membership.UserId.Should().Be(
                CurrentUserId
            );

            membership.RoleId.Should().Be(roleId);

            membership.Status.Should().Be(
                BusinessMembershipStatus.Active
            );

            membership.IsActive().Should().BeTrue();

            membership.JoinedAt.Should().Be(UtcNow);

            membership.CreatedByUserId.Should().Be(
                CurrentUserId
            );

            membership.UpdatedByUserId.Should().Be(
                CurrentUserId
            );

            membership.CreatedAt.Should().Be(UtcNow);
            membership.UpdatedAt.Should().Be(UtcNow);
            membership.DeactivatedAt.Should().BeNull();
            membership.ReactivatedAt.Should().BeNull();
        }

        private void AssertDefaultCategories(
            IReadOnlyCollection<Category> categories,
            Guid businessId
        )
        {
            var definitions =
                _defaultCategoryProvider
                    .GetDefaultCategories();

            categories.Should().HaveCount(
                definitions.Count
            );

            categories.Should().OnlyContain(
                category =>
                    category.BusinessId == businessId &&
                    category.IsDefault &&
                    category.IsActive &&
                    category.CreatedByUserId ==
                        CurrentUserId &&
                    category.UpdatedByUserId ==
                        CurrentUserId &&
                    category.CreatedAt == UtcNow &&
                    category.UpdatedAt == UtcNow &&
                    category.DeactivatedAt == null
            );

            categories
                .Select(category => new
                {
                    category.Type,
                    category.NormalizedName
                })
                .Should()
                .OnlyHaveUniqueItems();

            foreach (var definition in definitions)
            {
                categories.Should().ContainSingle(
                    category =>
                        category.Name ==
                            definition.Name &&
                        category.Type ==
                            definition.Type
                );
            }
        }

        private static void AssertResult(
            CreateBusinessResult result,
            Business business,
            BusinessMembership membership,
            IReadOnlyCollection<Category> categories
        )
        {
            result.BusinessId.Should().Be(
                business.Id
            );

            result.DisplayName.Should().Be(
                business.DisplayName
            );

            result.CurrencyCode.Should().Be(
                CurrencyConstants.PenCode
            );

            result.MembershipId.Should().Be(
                membership.Id
            );

            result.RoleCode.Should().Be(
                BusinessRoleConstants.OwnerCode
            );

            result.DefaultCategories.Should().HaveCount(
                categories.Count
            );

            result.DefaultCategories
                .Select(category => category.Id)
                .Should()
                .BeEquivalentTo(
                    categories.Select(category =>
                        category.Id
                    )
                );

            result.DefaultCategories.Should().OnlyContain(
                category =>
                    category.IsDefault &&
                    category.IsActive
            );
        }

        private void VerifySuccessfulPersistence()
        {
            _businessRepositoryMock.Verify(
                repository =>
                    repository.AddAsync(
                        It.IsAny<Business>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );

            _membershipRepositoryMock.Verify(
                repository =>
                    repository.AddAsync(
                        It.IsAny<BusinessMembership>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );

            _categoryRepositoryMock.Verify(
                repository =>
                    repository.AddAsync(
                        It.IsAny<Category>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Exactly(DefaultCategoryCount)
            );

            _unitOfWorkMock.Verify(
                unitOfWork =>
                    unitOfWork.SaveChangesAsync(
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }

        private void VerifyNothingWasPersisted()
        {
            _businessRepositoryMock.Verify(
                repository =>
                    repository.AddAsync(
                        It.IsAny<Business>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Never
            );

            _membershipRepositoryMock.Verify(
                repository =>
                    repository.AddAsync(
                        It.IsAny<BusinessMembership>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Never
            );

            _categoryRepositoryMock.Verify(
                repository =>
                    repository.AddAsync(
                        It.IsAny<Category>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Never
            );

            _unitOfWorkMock.Verify(
                unitOfWork =>
                    unitOfWork.SaveChangesAsync(
                        It.IsAny<CancellationToken>()
                    ),
                Times.Never
            );
        }

        private static CreateBusinessCommand
            CreateCommand()
        {
            return new CreateBusinessCommand
            {
                DisplayName = DisplayName,
                LegalName = LegalName,
                Ruc = Ruc,
                CurrencyCode =
                    CurrencyConstants.PenCode,
                CurrentUserId = CurrentUserId
            };
        }

        private static User CreateActiveUser()
        {
            return User.Create(
                Email,
                NormalizedEmail,
                PasswordHash,
                UserDisplayName,
                UtcNow.AddDays(-1)
            );
        }

        private static Currency CreateCurrency()
        {
            return Currency.CreateSystem(
                CurrencyConstants.PenId,
                CurrencyConstants.PenCode,
                "Sol peruano",
                "S/",
                (short)2
            );
        }

        private static BusinessRole CreateOwnerRole()
        {
            return BusinessRole.CreateSystem(
                BusinessRoleConstants.OwnerId,
                BusinessRoleConstants.OwnerCode,
                "Propietario",
                "Administra el negocio y sus miembros.",
                UtcNow
            );
        }
    }
}