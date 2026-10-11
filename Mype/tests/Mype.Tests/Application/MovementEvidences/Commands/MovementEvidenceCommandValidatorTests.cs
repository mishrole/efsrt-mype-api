using System;
using System.IO;
using FluentAssertions;
using FluentValidation.TestHelper;
using Mype.Application.MovementEvidences.Commands.AttachMovementEvidence;
using Mype.Application.MovementEvidences.Commands.RemoveMovementEvidence;
using Mype.Application.MovementEvidences.Commands.ReplaceMovementEvidence;
using Mype.Application.MovementEvidences.Models;

namespace Mype.Tests.Application.MovementEvidences.Commands
{
    public sealed class MovementEvidenceCommandValidatorTests
    {
        [Fact]
        public void AttachValidator_Should_Accept_Complete_Command()
        {
            var validator = new AttachMovementEvidenceCommandValidator();
            var command = new AttachMovementEvidenceCommand
            {
                BusinessId = Guid.NewGuid(),
                MovementId = Guid.NewGuid(),
                CurrentUserId = Guid.NewGuid(),
                MovementVersion = 1,
                File = File(),
            };

            validator.TestValidate(command).IsValid.Should().BeTrue();
        }

        [Fact]
        public void AttachValidator_Should_Reject_Empty_Identifiers_Version_And_File()
        {
            var validator = new AttachMovementEvidenceCommandValidator();
            var command = new AttachMovementEvidenceCommand();

            var result = validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.BusinessId);
            result.ShouldHaveValidationErrorFor(x => x.MovementId);
            result.ShouldHaveValidationErrorFor(x => x.CurrentUserId);
            result.ShouldHaveValidationErrorFor(x => x.MovementVersion);
            result.ShouldHaveValidationErrorFor(x => x.File);
        }

        [Fact]
        public void ReplaceValidator_Should_Reject_Empty_Identifiers_Versions_And_File()
        {
            var validator = new ReplaceMovementEvidenceCommandValidator();
            var command = new ReplaceMovementEvidenceCommand();

            var result = validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.BusinessId);
            result.ShouldHaveValidationErrorFor(x => x.MovementId);
            result.ShouldHaveValidationErrorFor(x => x.EvidenceId);
            result.ShouldHaveValidationErrorFor(x => x.CurrentUserId);
            result.ShouldHaveValidationErrorFor(x => x.MovementVersion);
            result.ShouldHaveValidationErrorFor(x => x.EvidenceVersion);
            result.ShouldHaveValidationErrorFor(x => x.File);
        }

        [Fact]
        public void RemoveValidator_Should_Reject_Empty_Identifiers_And_Versions()
        {
            var validator = new RemoveMovementEvidenceCommandValidator();
            var command = new RemoveMovementEvidenceCommand();

            var result = validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.BusinessId);
            result.ShouldHaveValidationErrorFor(x => x.MovementId);
            result.ShouldHaveValidationErrorFor(x => x.EvidenceId);
            result.ShouldHaveValidationErrorFor(x => x.CurrentUserId);
            result.ShouldHaveValidationErrorFor(x => x.MovementVersion);
            result.ShouldHaveValidationErrorFor(x => x.EvidenceVersion);
        }

        private static EvidenceFile File() =>
            new(
                "receipt.pdf",
                "application/pdf",
                5,
                new MemoryStream([0x25, 0x50, 0x44, 0x46, 0x2D])
            );
    }
}
