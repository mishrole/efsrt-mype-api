using FluentAssertions;
using Mype.Domain.Common;
using System;

namespace Mype.Tests.Domain.Common
{
    public class ReferenceEntityTests
    {
        [Fact]
        public void Constructor_Should_Initialize_Reference()
        {
            var id = Guid.NewGuid();

            var entity = new TestReferenceEntity(
                id,
                "CODE",
                "Nombre",
                true
            );

            entity.Id.Should().Be(id);
            entity.Code.Should().Be("CODE");
            entity.Name.Should().Be("Nombre");
            entity.IsActive.Should().BeTrue();
        }

        [Fact]
        public void Parameterless_Constructor_Should_Use_Default_Values()
        {
            var entity = new TestReferenceEntity();

            entity.Id.Should().Be(Guid.Empty);
            entity.Code.Should().BeEmpty();
            entity.Name.Should().BeEmpty();
            entity.IsActive.Should().BeFalse();
        }

        private sealed class TestReferenceEntity
            : ReferenceEntity
        {
            public TestReferenceEntity()
            {
            }

            public TestReferenceEntity(
                Guid id,
                string code,
                string name,
                bool isActive
            ) : base(
                id,
                code,
                name,
                isActive
            )
            {
            }
        }
    }
}
