using FluentAssertions;
using Mype.Application.Products.Commands.ReactivateProduct;
using Mype.Shared.Constants;
namespace Mype.Tests.Application.Products.Commands.ReactivateProduct
{
    public class ReactivateProductCommandHandlerTests
    {
        [Fact]
        public void Errors_Should_Expose_Expected_Codes()
        {
            ReactivateProductErrors.ProductAccessForbidden.Code.Should().Be(ErrorCodes.ProductAccessForbidden);
            ReactivateProductErrors.ProductAlreadyActive.Code.Should().Be(ErrorCodes.ProductAlreadyActive);
            ReactivateProductErrors.ProductConcurrencyConflict.Code.Should().Be(ErrorCodes.ProductConcurrencyConflict);
        }
    }
}
