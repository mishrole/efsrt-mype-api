using System;
using Mype.Domain.Businesses;

namespace Mype.Application.BusinessMemberships.Models
{
    public sealed record BusinessSummaryProjection(
        Guid BusinessId,
        string DisplayName,
        string CurrencyCode,
        BusinessStatus Status,
        Guid MembershipId,
        string RoleCode
    );
}
