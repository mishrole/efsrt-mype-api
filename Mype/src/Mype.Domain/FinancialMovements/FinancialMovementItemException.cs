using System;

namespace Mype.Domain.FinancialMovements
{
    public sealed class FinancialMovementItemException : InvalidOperationException
    {
        public FinancialMovementItemException(FinancialMovementItemError error)
            : base(error.ToString())
        {
            Error = error;
        }

        public FinancialMovementItemError Error { get; }
    }

    public enum FinancialMovementItemError
    {
        MovementNotEditable = 1,
        MovementMustBeSale = 2,
        ItemNotFound = 3,
        ItemAlreadyRetired = 4,
        ProductBusinessMismatch = 5,
        InvalidQuantity = 6,
        InvalidUnitAmount = 7,
    }
}
