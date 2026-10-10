using FluentAssertions;
using Mype.Application.Products.Commands.UpdateProduct;
using Mype.Shared.Constants;
namespace Mype.Tests.Application.Products.Commands.UpdateProduct
{
    public class UpdateProductCommandHandlerTests
    {
        [Fact]
        public void Errors_Should_Expose_Expected_Codes()
        {
            UpdateProductErrors.ProductAccessForbidden.Code.Should().Be(ErrorCodes.ProductAccessForbidden);
            UpdateProductErrors.ProductAlreadyExists.Code.Should().Be(ErrorCodes.ProductAlreadyExists);
            UpdateProductErrors.ProductConcurrencyConflict.Code.Should().Be(ErrorCodes.ProductConcurrencyConflict);
        }
    }
}
