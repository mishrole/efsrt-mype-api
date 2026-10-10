using System;
using Mype.Domain.Businesses;

namespace Mype.Application.Businesses.Queries.ListUserBusinesses
{
    public sealed record BusinessSummaryResult(
        Guid BusinessId,
        string DisplayName,
        string CurrencyCode,
        BusinessStatus Status,
        Guid MembershipId,
        string RoleCode
    );
}
