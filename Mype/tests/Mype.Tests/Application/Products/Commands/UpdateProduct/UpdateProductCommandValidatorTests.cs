using System;
using System.Threading.Tasks;
using FluentAssertions;
using Mype.Application.Products.Commands.UpdateProduct;

namespace Mype.Tests.Application.Products.Commands.UpdateProduct
{
    public class UpdateProductCommandValidatorTests
    {
        private readonly UpdateProductCommandValidator _validator = new();

        [Fact]
        public async Task Validate_Should_Accept_Valid_Command() =>
            (await _validator.ValidateAsync(Valid())).IsValid.Should().BeTrue();

        [Fact]
        public async Task Validate_Should_Reject_Empty_Ids_And_Version()
        {
            var c = Valid();
            c.BusinessId = Guid.Empty;
            c.ProductId = Guid.Empty;
            c.CurrentUserId = Guid.Empty;
            c.CategoryId = Guid.Empty;
            c.Version = 0;
            (await _validator.ValidateAsync(c)).Errors.Should().HaveCountGreaterThanOrEqualTo(5);
        }

        [Fact]
        public async Task Validate_Should_Reject_Invalid_Name_And_Amounts()
        {
            var c = Valid();
            c.Name = "   ";
            c.SalePrice = -1;
            c.UnitCost = 1.001m;
            (await _validator.ValidateAsync(c)).IsValid.Should().BeFalse();
        }

        private static UpdateProductCommand Valid() =>
            new()
            {
                BusinessId = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                CurrentUserId = Guid.NewGuid(),
                CategoryId = Guid.NewGuid(),
                Name = "Gaseosa",
                SalePrice = 3.5m,
                UnitCost = 2.2m,
                Version = 1,
            };
    }
}
