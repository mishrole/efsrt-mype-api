using System;
namespace Mype.Application.FinancialMovements.Models
{
    public sealed record FinancialMovementItemResult(Guid Id, Guid ProductId, string ProductName, Guid CategoryId, string CategoryName, decimal Quantity, decimal UnitAmount, decimal UnitCostSnapshot, decimal SubtotalAmount, decimal EstimatedCost, decimal EstimatedMargin, bool IsActive, DateTimeOffset? RetiredAt, uint Version);
    public sealed record FinancialMovementItemMaintenanceResult(FinancialMovementItemResult Item, decimal MovementTotal, uint MovementVersion);
    public sealed record RetiredFinancialMovementItemResult(Guid ItemId, bool IsActive, DateTimeOffset RetiredAt, uint ItemVersion, decimal MovementTotal, uint MovementVersion);
}
