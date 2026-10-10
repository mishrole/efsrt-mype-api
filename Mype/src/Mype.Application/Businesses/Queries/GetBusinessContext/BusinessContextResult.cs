using System;
using System.Collections.Generic;

namespace Mype.Application.Businesses.Queries.GetBusinessContext
{
    public sealed record BusinessContextResult(
        Guid BusinessId,
        string DisplayName,
        string CurrencyCode,
        Guid MembershipId,
        Guid RoleId,
        string RoleCode,
        IReadOnlyCollection<string> Permissions
    );
}
