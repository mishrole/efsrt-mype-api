using Mype.Domain.Businesses;
using System;

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