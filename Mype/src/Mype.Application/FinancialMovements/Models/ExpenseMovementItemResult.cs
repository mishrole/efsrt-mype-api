using System;

namespace Mype.Application.FinancialMovements.Models
{
    public sealed record ExpenseMovementItemResult(
        Guid Id,
        string Description,
        Guid CategoryId,
        string CategoryName,
        decimal Quantity,
        decimal UnitAmount,
        decimal SubtotalAmount,
        bool IsActive,
        DateTimeOffset? RetiredAt,
        uint Version
    );

    public sealed record ExpenseMovementItemMaintenanceResult(
        ExpenseMovementItemResult Item,
        decimal MovementTotal,
        uint MovementVersion
    );
}
