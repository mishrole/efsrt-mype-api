using System;
using System.Threading.Tasks;
using FluentAssertions;
using Mype.Application.Products.Commands.CreateProduct;

namespace Mype.Tests.Application.Products.Commands.CreateProduct
{
    public class CreateProductCommandValidatorTests
    {
        private readonly CreateProductCommandValidator _validator = new();

        [Fact]
        public async Task Validate_Should_Succeed_For_Valid_Command()
        {
            var result = await _validator.ValidateAsync(CreateCommand());

            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public async Task Validate_Should_Allow_Zero_And_Positive_Amounts(int value)
        {
            var command = CreateCommand();

            command.SalePrice = value;
            command.UnitCost = value;

            var result = await _validator.ValidateAsync(command);

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public async Task Validate_Should_Fail_For_Empty_Ids()
        {
            var command = CreateCommand();

            command.BusinessId = Guid.Empty;
            command.CurrentUserId = Guid.Empty;
            command.CategoryId = Guid.Empty;

            var result = await _validator.ValidateAsync(command);

            result
                .Errors.Should()
                .Contain(error => error.PropertyName == nameof(command.BusinessId));

            result
                .Errors.Should()
                .Contain(error => error.PropertyName == nameof(command.CurrentUserId));

            result
                .Errors.Should()
                .Contain(error => error.PropertyName == nameof(command.CategoryId));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Validate_Should_Fail_For_Empty_Name(string name)
        {
            var command = CreateCommand();

            command.Name = name;

            var result = await _validator.ValidateAsync(command);

            result.IsValid.Should().BeFalse();

            result.Errors.Should().Contain(error => error.PropertyName == nameof(command.Name));
        }

        [Fact]
        public async Task Validate_Should_Fail_For_Negative_Amounts()
        {
            var command = CreateCommand();

            command.SalePrice = -0.01m;
            command.UnitCost = -0.01m;

            var result = await _validator.ValidateAsync(command);

            result
                .Errors.Should()
                .Contain(error => error.PropertyName == nameof(command.SalePrice));

            result.Errors.Should().Contain(error => error.PropertyName == nameof(command.UnitCost));
        }

        [Fact]
        public async Task Validate_Should_Fail_For_More_Than_Two_Decimals()
        {
            var command = CreateCommand();

            command.SalePrice = 1.001m;
            command.UnitCost = 2.999m;

            var result = await _validator.ValidateAsync(command);

            result
                .Errors.Should()
                .Contain(error => error.PropertyName == nameof(command.SalePrice));

            result.Errors.Should().Contain(error => error.PropertyName == nameof(command.UnitCost));
        }

        private static CreateProductCommand CreateCommand()
        {
            return new CreateProductCommand
            {
                BusinessId = Guid.NewGuid(),
                CurrentUserId = Guid.NewGuid(),
                CategoryId = Guid.NewGuid(),
                Name = "Gaseosa 500 ml",
                SalePrice = 3.50m,
                UnitCost = 2.20m,
            };
        }
    }
}
