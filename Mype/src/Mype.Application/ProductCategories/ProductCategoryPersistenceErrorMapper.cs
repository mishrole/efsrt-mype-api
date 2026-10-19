using Mype.Application.Common;
using Mype.Application.Common.Exceptions;
using Mype.Shared.Constants;

namespace Mype.Application.ProductCategories
{
    public static class ProductCategoryPersistenceErrorMapper
    {
        public static ApplicationError Map(ApplicationErrorException persistenceException)
        {
            if (persistenceException.Code == ErrorCodes.ProductCategoryAlreadyExists)
            {
                return ProductCategoryErrors.AlreadyExists;
            }

            if (persistenceException.Code == ErrorCodes.ConcurrencyConflict)
            {
                return ProductCategoryErrors.ConcurrencyConflict;
            }

            return ProductCategoryErrors.PersistenceFailed;
        }
    }
}
