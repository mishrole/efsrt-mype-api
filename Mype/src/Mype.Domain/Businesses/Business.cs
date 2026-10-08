using Mype.Domain.Common;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Mype.Domain.Businesses
{
    public class Business : Entity
    {
        private Business()
        {
        }

        private Business(
            Guid id,
            string displayName,
            string legalName,
            string ruc,
            Guid currencyId,
            Guid userId,
            DateTimeOffset utcNow
        ) : base(id)
        {
            DisplayName = displayName;
            LegalName = legalName;
            Ruc = ruc;
            CurrencyId = currencyId;
            Status = BusinessStatus.Active;
            CreatedByUserId = userId;
            UpdatedByUserId = userId;
            CreatedAt = utcNow;
            UpdatedAt = utcNow;
        }

        public string DisplayName { get; private set; } =
            string.Empty;

        public string LegalName { get; private set; }

        public string Ruc { get; private set; }

        public Guid CurrencyId { get; private set; }

        public BusinessStatus Status { get; private set; }

        public Guid CreatedByUserId { get; private set; }

        public Guid UpdatedByUserId { get; private set; }

        public DateTimeOffset? DeactivatedAt { get; private set; }

        public uint Version { get; private set; }

        public static Business Create(
            string displayName,
            string legalName,
            string ruc,
            Guid currencyId,
            Guid userId,
            DateTimeOffset utcNow
        )
        {
            return new Business(
                Guid.NewGuid(),
                displayName.Trim(),
                NormalizeOptionalValue(legalName),
                NormalizeOptionalValue(ruc),
                currencyId,
                userId,
                utcNow
            );
        }

        public bool IsActive()
        {
            return Status == BusinessStatus.Active;
        }

        public void Deactivate(
            Guid currentUserId,
            DateTimeOffset utcNow
        )
        {
            Status = BusinessStatus.Inactive;
            DeactivatedAt = utcNow;
            UpdatedByUserId = currentUserId;
            UpdatedAt = utcNow;
        }

        private static string NormalizeOptionalValue(
            string value
        )
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
    }
}