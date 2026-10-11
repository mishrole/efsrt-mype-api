using System;
using FluentAssertions;
using Mype.Application.MovementEvidences.Common;

namespace Mype.Tests.Application.MovementEvidences.Common
{
    public sealed class StorageObjectKeyGeneratorTests
    {
        [Fact]
        public void Generate_Should_Create_Isolated_Opaque_Key()
        {
            var businessId = Guid.NewGuid();
            var movementId = Guid.NewGuid();
            var evidenceId = Guid.NewGuid();
            var generator = new StorageObjectKeyGenerator();

            var key = generator.Generate(businessId, movementId, evidenceId, ".pdf");

            key.Should().StartWith(
                $"businesses/{businessId:N}/movements/{movementId:N}/evidences/{evidenceId:N}/"
            );
            key.Should().EndWith(".pdf");
            key.Should().NotContain("receipt");
        }

        [Fact]
        public void Generate_Should_Create_A_Unique_Token_Each_Time()
        {
            var generator = new StorageObjectKeyGenerator();
            var businessId = Guid.NewGuid();
            var movementId = Guid.NewGuid();
            var evidenceId = Guid.NewGuid();

            var first = generator.Generate(businessId, movementId, evidenceId, ".png");
            var second = generator.Generate(businessId, movementId, evidenceId, ".png");

            first.Should().NotBe(second);
        }
    }
}
