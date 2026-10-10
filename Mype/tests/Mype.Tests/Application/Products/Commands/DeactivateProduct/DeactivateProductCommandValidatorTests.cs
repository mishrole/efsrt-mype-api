using FluentAssertions;
using Mype.Application.Products.Commands.DeactivateProduct;
using System;
using System.Threading.Tasks;

namespace Mype.Tests.Application.Products.Commands.DeactivateProduct
{
    public class DeactivateProductCommandValidatorTests
    {
        private readonly DeactivateProductCommandValidator _validator = new();

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
            var command = new DeactivateProductCommand();

            var result = await _validator.ValidateAsync(command);

            result.Errors.Should().Contain(error => error.PropertyName == nameof(command.BusinessId));
            result.Errors.Should().Contain(error => error.PropertyName == nameof(command.ProductId));
            result.Errors.Should().Contain(error => error.PropertyName == nameof(command.CurrentUserId));
            result.Errors.Should().Contain(error => error.PropertyName == nameof(command.Version));
        }

        private static DeactivateProductCommand CreateCommand()
        {
            return new DeactivateProductCommand
            {
                BusinessId = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                CurrentUserId = Guid.NewGuid(),
                Version = 1
            };
        }
    }
}
