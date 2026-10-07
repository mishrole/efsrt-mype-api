using Mype.Application.Common.Interfaces;
using System;

namespace Mype.Infrastructure.Common
{
    public class SystemClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
