using System;

namespace Mype.Application.Common.Models
{
    public sealed record TemporaryObjectAccessResult(Uri Url, DateTimeOffset ExpiresAt);
}
