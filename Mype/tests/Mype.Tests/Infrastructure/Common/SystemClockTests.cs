using System;
using FluentAssertions;
using Mype.Infrastructure.Common;

namespace Mype.Tests.Infrastructure.Common
{
    public class SystemClockTests
    {
        [Fact]
        public void UtcNow_Should_Return_Current_Utc_Time()
        {
            var clock = new SystemClock();
            var before = DateTimeOffset.UtcNow;

            var result = clock.UtcNow;

            var after = DateTimeOffset.UtcNow;

            result.Should().BeOnOrAfter(before);
            result.Should().BeOnOrBefore(after);
            result.Offset.Should().Be(TimeSpan.Zero);
        }
    }
}
