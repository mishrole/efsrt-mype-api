using FluentAssertions;
using Mype.Application.Products.Commands.DeactivateProduct;
using Mype.Shared.Constants;
namespace Mype.Tests.Application.Products.Commands.DeactivateProduct
{
    public class DeactivateProductCommandHandlerTests
    {
        [Fact]
        public void Errors_Should_Expose_Expected_Codes()
        {
            DeactivateProductErrors.ProductAccessForbidden.Code.Should().Be(ErrorCodes.ProductAccessForbidden);
            DeactivateProductErrors.ProductAlreadyInactive.Code.Should().Be(ErrorCodes.ProductAlreadyInactive);
            DeactivateProductErrors.ProductConcurrencyConflict.Code.Should().Be(ErrorCodes.ProductConcurrencyConflict);
        }
    }
}
