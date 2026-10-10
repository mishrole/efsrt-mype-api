using System;
using Mype.Application.Common.Interfaces;

namespace Mype.Infrastructure.Common
{
    public class SystemClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
