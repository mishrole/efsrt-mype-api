using System;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;

namespace Mype.Application.BusinessMemberships.Models
{
    public sealed record BusinessContextProjection(
        Guid BusinessId,
        string DisplayName,
        string CurrencyCode,
        BusinessStatus BusinessStatus,
        Guid MembershipId,
        BusinessMembershipStatus MembershipStatus,
        Guid RoleId,
        string RoleCode,
        bool RoleIsActive
    );
}
