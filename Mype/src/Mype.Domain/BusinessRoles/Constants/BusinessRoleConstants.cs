using System;

namespace Mype.Domain.BusinessRoles.Constants
{
    public static class BusinessRoleConstants
    {
        public static readonly Guid OwnerId = Guid.Parse("20000000-0000-0000-0000-000000000001");

        public static readonly Guid CollaboratorId = Guid.Parse(
            "20000000-0000-0000-0000-000000000002"
        );

        public const string OwnerCode = "OWNER";

        public const string CollaboratorCode = "COLLABORATOR";

        public static readonly DateTimeOffset SeededAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    }
}
