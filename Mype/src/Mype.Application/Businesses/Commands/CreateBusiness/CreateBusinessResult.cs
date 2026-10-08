using System;
using System.Collections.Generic;

namespace Mype.Application.Businesses.Commands.CreateBusiness
{
    public class CreateBusinessResult
    {
        public Guid BusinessId { get; set; }

        public string DisplayName { get; set; } = string.Empty;

        public string CurrencyCode { get; set; } = string.Empty;

        public Guid MembershipId { get; set; }

        public string RoleCode { get; set; } = string.Empty;

        public IReadOnlyCollection<CategoryResult>
            DefaultCategories
        { get; set; } =
                Array.Empty<CategoryResult>();

        public uint Version { get; set; }
    }
}