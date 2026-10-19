using FluentAssertions;
using Mype.Application.Common.Exceptions;
using Mype.Application.ProductCategories;
using Mype.Shared.Constants;

namespace Mype.Tests.Application.ProductCategories
{
    public sealed class ProductCategoryPersistenceErrorMapperTests
    {
        [Theory]
        [InlineData(ErrorCodes.ProductCategoryAlreadyExists)]
        [InlineData(ErrorCodes.ConcurrencyConflict)]
        public void Map_Should_Translate_Known_Persistence_Errors(string persistenceErrorCode)
        {
            var persistenceException = new ApplicationErrorException(
                persistenceErrorCode,
                "error",
                ApplicationErrorType.Conflict
            );
            var mappedError = ProductCategoryPersistenceErrorMapper.Map(persistenceException);
            if (persistenceErrorCode == ErrorCodes.ProductCategoryAlreadyExists)
            {
                mappedError.Should().BeSameAs(ProductCategoryErrors.AlreadyExists);
            }
            else
            {
                mappedError.Should().BeSameAs(ProductCategoryErrors.ConcurrencyConflict);
            }
        }

        [Fact]
        public void Map_Should_Return_PersistenceFailed_For_Unknown_Error()
        {
            var persistenceException = new ApplicationErrorException(
                "UNKNOWN",
                "error",
                ApplicationErrorType.Internal
            );
            ProductCategoryPersistenceErrorMapper
                .Map(persistenceException)
                .Should()
                .BeSameAs(ProductCategoryErrors.PersistenceFailed);
        }
    }
}
