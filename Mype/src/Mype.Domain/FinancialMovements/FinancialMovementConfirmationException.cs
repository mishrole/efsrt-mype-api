using System;

namespace Mype.Domain.FinancialMovements
{
    public sealed class FinancialMovementConfirmationException : InvalidOperationException
    {
        public FinancialMovementConfirmationException(
            FinancialMovementConfirmationError error,
            Guid? itemId = null
        )
            : base(error.ToString())
        {
            Error = error;
            ItemId = itemId;
        }

        public FinancialMovementConfirmationError Error { get; }
        public Guid? ItemId { get; }
    }
}
