namespace Mype.Domain.FinancialMovements
{
    public enum FinancialMovementConfirmationError
    {
        MovementNotEditable = 1,
        MovementRequiresActiveItems = 2,
        InvalidSaleItem = 3,
        InvalidExpenseItem = 4,
        InvalidItemAmounts = 5,
    }
}
