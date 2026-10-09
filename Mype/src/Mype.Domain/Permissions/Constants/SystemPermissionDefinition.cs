using System;

namespace Mype.Domain.Permissions.Constants
{
    public sealed record SystemPermissionDefinition(
        Guid Id,
        string Code,
        string Name,
        string Description
    );
}