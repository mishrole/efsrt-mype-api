using FluentAssertions;
using Mype.Application.Products.Commands.ReactivateProduct;
using System;
using System.Threading.Tasks;

namespace Mype.Tests.Application.Products.Commands.ReactivateProduct
{
    public class ReactivateProductCommandValidatorTests
    {
        private readonly ReactivateProductCommandValidator _validator = new();

        [Fact]
        public async Task Validate_Should_Succeed_For_Valid_Command()
        {
            var result = await _validator.ValidateAsync(CreateCommand());

            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task Validate_Should_Fail_For_Empty_Ids_And_Version()
        {
            var command = new ReactivateProductCommand();

            var result = await _validator.ValidateAsync(command);

            result.Errors.Should().Contain(error => error.PropertyName == nameof(command.BusinessId));
            result.Errors.Should().Contain(error => error.PropertyName == nameof(command.ProductId));
            result.Errors.Should().Contain(error => error.PropertyName == nameof(command.CurrentUserId));
            result.Errors.Should().Contain(error => error.PropertyName == nameof(command.Version));
        }

        private static ReactivateProductCommand CreateCommand()
        {
            return new ReactivateProductCommand
            {
                BusinessId = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                CurrentUserId = Guid.NewGuid(),
                Version = 1
            };
        }
    }
}
