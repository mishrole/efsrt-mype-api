using FluentAssertions;
using Mype.Domain.Businesses;
using System;

namespace Mype.Tests.Domain.Businesses
{
    public class BusinessTests
    {
        private static readonly Guid CurrencyId =
            Guid.NewGuid();

        private static readonly Guid UserId =
            Guid.NewGuid();

        private static readonly DateTimeOffset CreatedAt =
            new(
                2026,
                10,
                9,
                12,
                0,
                0,
                TimeSpan.Zero
            );

        [Fact]
        public void Create_Should_Normalize_Values_And_Initialize_Business()
        {
            var business = Business.Create(
                "  Bodega Central  ",
                "  Comercial Central E.I.R.L.  ",
                "  20123456786  ",
                CurrencyId,
                UserId,
                CreatedAt
            );

            business.Id.Should().NotBeEmpty();
            business.DisplayName.Should().Be(
                "Bodega Central"
            );
            business.LegalName.Should().Be(
                "Comercial Central E.I.R.L."
            );
            business.Ruc.Should().Be("20123456786");
            business.CurrencyId.Should().Be(CurrencyId);
            business.Status.Should().Be(
                BusinessStatus.Active
            );
            business.CreatedByUserId.Should().Be(UserId);
            business.UpdatedByUserId.Should().Be(UserId);
            business.CreatedAt.Should().Be(CreatedAt);
            business.UpdatedAt.Should().Be(CreatedAt);
            business.IsActive().Should().BeTrue();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_Should_Normalize_Optional_Values_To_Null(
            string value
        )
        {
            var business = Business.Create(
                "Bodega Central",
                value,
                value,
                CurrencyId,
                UserId,
                CreatedAt
            );

            business.LegalName.Should().BeNull();
            business.Ruc.Should().BeNull();
        }

        [Fact]
        public void Deactivate_Should_Set_Business_As_Inactive()
        {
            var business = Business.Create(
                "Bodega Central",
                null,
                null,
                CurrencyId,
                UserId,
                CreatedAt
            );

            var currentUserId = Guid.NewGuid();
            var deactivatedAt = CreatedAt.AddHours(1);

            business.Deactivate(
                currentUserId,
                deactivatedAt
            );

            business.Status.Should().Be(
                BusinessStatus.Inactive
            );
            business.DeactivatedAt.Should().Be(
                deactivatedAt
            );
            business.UpdatedByUserId.Should().Be(
                currentUserId
            );
            business.UpdatedAt.Should().Be(
                deactivatedAt
            );
            business.IsActive().Should().BeFalse();
        }
    }
}
