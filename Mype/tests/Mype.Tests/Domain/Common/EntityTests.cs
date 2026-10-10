using FluentAssertions;
using Mype.Domain.Common;
using System;

namespace Mype.Tests.Domain.Common
{
    public class EntityTests
    {
        [Fact]
        public void Constructor_Should_Initialize_Id()
        {
            var id = Guid.NewGuid();

            var entity = new TestEntity(id);

            entity.Id.Should().Be(id);
            entity.CreatedAt.Should().Be(default);
            entity.UpdatedAt.Should().Be(default);
        }

        [Fact]
        public void Parameterless_Constructor_Should_Leave_Default_Id()
        {
            var entity = new TestEntity();

            entity.Id.Should().Be(Guid.Empty);
        }

        private sealed class TestEntity : Entity
        {
            public TestEntity()
            {
            }

            public TestEntity(Guid id)
                : base(id)
            {
            }
        }
    }
}
