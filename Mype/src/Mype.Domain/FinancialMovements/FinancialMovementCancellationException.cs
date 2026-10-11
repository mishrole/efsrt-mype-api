using System;

namespace Mype.Domain.FinancialMovements
{
    public sealed class FinancialMovementCancellationException : InvalidOperationException
    {
        public FinancialMovementCancellationException(FinancialMovementCancellationError error)
            : base(error.ToString())
        {
            Error = error;
        }

        public FinancialMovementCancellationError Error { get; }
    }
}
