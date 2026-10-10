using FluentAssertions;
using Mype.Application.FinancialMovements.Commands.AddSaleItem;
using Mype.Application.FinancialMovements.Commands.Common;
using Mype.Application.FinancialMovements.Commands.RetireFinancialMovementItem;
using Mype.Application.FinancialMovements.Commands.UpdateSaleItem;

namespace Mype.Tests.Application.FinancialMovements.Commands
{
    public class FinancialMovementItemErrorsTests
    {
        [Fact]
        public void Operation_Error_Catalogs_Should_Reuse_Shared_Instances()
        {
            AddSaleItemErrors
                .BusinessAccessForbidden.Should()
                .BeSameAs(FinancialMovementItemErrors.BusinessAccessForbidden);
            AddSaleItemErrors
                .BusinessUnavailable.Should()
                .BeSameAs(FinancialMovementItemErrors.BusinessUnavailable);
            AddSaleItemErrors
                .MovementAccessForbidden.Should()
                .BeSameAs(FinancialMovementItemErrors.MovementAccessForbidden);
            AddSaleItemErrors
                .MovementNotFound.Should()
                .BeSameAs(FinancialMovementItemErrors.MovementNotFound);
            AddSaleItemErrors
                .MovementNotEditable.Should()
                .BeSameAs(FinancialMovementItemErrors.MovementNotEditable);
            AddSaleItemErrors
                .MovementMustBeSale.Should()
                .BeSameAs(FinancialMovementItemErrors.MovementMustBeSale);
            AddSaleItemErrors
                .ProductNotFound.Should()
                .BeSameAs(FinancialMovementItemErrors.ProductNotFound);
            AddSaleItemErrors
                .ProductInactive.Should()
                .BeSameAs(FinancialMovementItemErrors.ProductInactive);
            AddSaleItemErrors
                .ProductCategoryUnavailable.Should()
                .BeSameAs(FinancialMovementItemErrors.ProductCategoryUnavailable);
            UpdateSaleItemErrors
                .ItemNotFound.Should()
                .BeSameAs(FinancialMovementItemErrors.ItemNotFound);
            UpdateSaleItemErrors
                .ItemAlreadyRetired.Should()
                .BeSameAs(FinancialMovementItemErrors.ItemAlreadyRetired);
            RetireFinancialMovementItemErrors
                .ConcurrencyConflict.Should()
                .BeSameAs(FinancialMovementItemErrors.ConcurrencyConflict);
            RetireFinancialMovementItemErrors
                .OperationFailed.Should()
                .BeSameAs(FinancialMovementItemErrors.OperationFailed);
        }
    }
}
