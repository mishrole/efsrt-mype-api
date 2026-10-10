using System;
using System.Collections.Generic;
using System.Linq;
using Mype.Domain.Common;
using Mype.Domain.Products;

namespace Mype.Domain.FinancialMovements
{
    public class FinancialMovement : Entity
    {
        private readonly List<FinancialMovementItem> _items = new();

        private FinancialMovement() { }

        private FinancialMovement(
            Guid id,
            Guid businessId,
            FinancialMovementType type,
            DateOnly movementDate,
            string description,
            string currencyCode,
            Guid userId,
            DateTimeOffset utcNow
        )
            : base(id)
        {
            BusinessId = businessId;
            Type = type;
            Status = FinancialMovementStatus.Draft;
            MovementDate = movementDate;
            Description = NormalizeOptionalValue(description);
            CurrencyCode = currencyCode;
            TotalAmount = 0m;
            CreatedByUserId = userId;
            UpdatedByUserId = userId;
            CreatedAt = utcNow;
            UpdatedAt = utcNow;
        }

        public Guid BusinessId { get; private set; }
        public FinancialMovementType Type { get; private set; }
        public FinancialMovementStatus Status { get; private set; }
        public DateOnly MovementDate { get; private set; }
        public string Description { get; private set; }
        public string CurrencyCode { get; private set; } = string.Empty;
        public decimal TotalAmount { get; private set; }
        public Guid CreatedByUserId { get; private set; }
        public Guid UpdatedByUserId { get; private set; }
        public Guid? ConfirmedByUserId { get; private set; }
        public Guid? CancelledByUserId { get; private set; }
        public string CancellationReason { get; private set; }
        public DateTimeOffset? ConfirmedAt { get; private set; }
        public DateTimeOffset? CancelledAt { get; private set; }
        public DateTimeOffset? DiscardedAt { get; private set; }
        public uint Version { get; private set; }
        public IReadOnlyCollection<FinancialMovementItem> Items => _items.AsReadOnly();

        public bool IsEditable() => Status == FinancialMovementStatus.Draft;

        public void UpdateDraftHeader(
            DateOnly movementDate,
            string description,
            Guid currentUserId,
            DateTimeOffset utcNow
        )
        {
            EnsureEditable();
            MovementDate = movementDate;
            Description = NormalizeOptionalValue(description);
            Touch(currentUserId, utcNow);
        }

        public FinancialMovementItem AddSaleItem(
            Product product,
            decimal quantity,
            decimal unitAmount,
            Guid userId,
            DateTimeOffset utcNow
        )
        {
            EnsureDraftSale();
            var item = FinancialMovementItem.CreateSale(
                BusinessId,
                Id,
                product,
                quantity,
                unitAmount,
                userId,
                utcNow
            );
            _items.Add(item);
            RecalculateTotal();
            Touch(userId, utcNow);
            return item;
        }

        public FinancialMovementItem UpdateSaleItem(
            Guid itemId,
            Product product,
            decimal quantity,
            decimal unitAmount,
            Guid userId,
            DateTimeOffset utcNow
        )
        {
            EnsureDraftSale();
            var item = GetRequiredItem(itemId);
            item.UpdateSale(product, quantity, unitAmount, userId, utcNow);
            RecalculateTotal();
            Touch(userId, utcNow);
            return item;
        }

        public FinancialMovementItem AddExpenseItem(
            Guid categoryId,
            string description,
            decimal quantity,
            decimal unitAmount,
            Guid userId,
            DateTimeOffset utcNow
        )
        {
            EnsureDraftExpense();
            var item = FinancialMovementItem.CreateExpense(
                BusinessId,
                Id,
                categoryId,
                description,
                quantity,
                unitAmount,
                userId,
                utcNow
            );
            _items.Add(item);
            RecalculateTotal();
            Touch(userId, utcNow);
            return item;
        }

        public FinancialMovementItem UpdateExpenseItem(
            Guid itemId,
            Guid categoryId,
            string description,
            decimal quantity,
            decimal unitAmount,
            Guid userId,
            DateTimeOffset utcNow
        )
        {
            EnsureDraftExpense();
            var item = GetRequiredItem(itemId);
            item.UpdateExpense(categoryId, description, quantity, unitAmount, userId, utcNow);
            RecalculateTotal();
            Touch(userId, utcNow);
            return item;
        }

        public FinancialMovementItem RetireItem(Guid itemId, Guid userId, DateTimeOffset utcNow)
        {
            EnsureEditable();
            var item = GetRequiredItem(itemId);
            item.Retire(userId, utcNow);
            RecalculateTotal();
            Touch(userId, utcNow);
            return item;
        }

        public static FinancialMovement CreateDraft(
            Guid businessId,
            FinancialMovementType type,
            DateOnly movementDate,
            string description,
            string currencyCode,
            Guid userId,
            DateTimeOffset utcNow
        ) =>
            new(
                Guid.NewGuid(),
                businessId,
                type,
                movementDate,
                description,
                currencyCode,
                userId,
                utcNow
            );

        private FinancialMovementItem GetRequiredItem(Guid itemId) =>
            _items.SingleOrDefault(item => item.Id == itemId)
            ?? throw new FinancialMovementItemException(FinancialMovementItemError.ItemNotFound);

        private void EnsureEditable()
        {
            if (!IsEditable())
                throw new FinancialMovementItemException(
                    FinancialMovementItemError.MovementNotEditable
                );
        }

        private void EnsureDraftSale()
        {
            EnsureEditable();
            if (Type != FinancialMovementType.Sale)
                throw new FinancialMovementItemException(
                    FinancialMovementItemError.MovementMustBeSale
                );
        }

        private void EnsureDraftExpense()
        {
            EnsureEditable();
            if (Type != FinancialMovementType.Expense)
                throw new FinancialMovementItemException(
                    FinancialMovementItemError.MovementMustBeExpense
                );
        }

        private void RecalculateTotal() =>
            TotalAmount = _items.Where(item => item.IsActive).Sum(item => item.SubtotalAmount);

        private void Touch(Guid userId, DateTimeOffset utcNow)
        {
            UpdatedByUserId = userId;
            UpdatedAt = utcNow;
        }

        private static string NormalizeOptionalValue(string value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
