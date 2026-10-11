using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;
using Moq;
using Mype.Api.Context;
using Mype.Api.Endpoints.v1.FinancialMovements;

namespace Mype.Tests.Api.Endpoints.v1.FinancialMovements
{
    public sealed class MovementEvidenceEndpointRequestValidationTests
    {
        [Fact]
        public async Task Attach_Should_Reject_Request_Without_Multipart_ContentType()
        {
            var fixture = new Fixture();

            var action = () =>
                AttachMovementEvidenceEndpoint.DoAsync(
                    fixture.BusinessId,
                    fixture.MovementId,
                    fixture.Sender.Object,
                    fixture.User.Object,
                    new DefaultHttpContext(),
                    CancellationToken.None
                );

            await action.Should().ThrowAsync<BadHttpRequestException>();
        }

        [Fact]
        public async Task Attach_Should_Reject_Multipart_Without_File()
        {
            var fixture = new Fixture();
            var context = CreateContext(
                new Dictionary<string, StringValues> { ["movementVersion"] = "1" },
                []
            );

            var action = () =>
                AttachMovementEvidenceEndpoint.DoAsync(
                    fixture.BusinessId,
                    fixture.MovementId,
                    fixture.Sender.Object,
                    fixture.User.Object,
                    context,
                    CancellationToken.None
                );

            await action.Should().ThrowAsync<BadHttpRequestException>();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("invalid")]
        [InlineData("-1")]
        public async Task Attach_Should_Reject_Invalid_MovementVersion(string movementVersion)
        {
            var fixture = new Fixture();
            using var stream = new MemoryStream([0x25, 0x50, 0x44, 0x46, 0x2D]);
            var values = new Dictionary<string, StringValues>();
            if (movementVersion != null)
                values["movementVersion"] = movementVersion;
            var context = CreateContext(values, [CreateFile(stream)]);

            var action = () =>
                AttachMovementEvidenceEndpoint.DoAsync(
                    fixture.BusinessId,
                    fixture.MovementId,
                    fixture.Sender.Object,
                    fixture.User.Object,
                    context,
                    CancellationToken.None
                );

            await action.Should().ThrowAsync<BadHttpRequestException>();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("invalid")]
        [InlineData("-1")]
        public async Task Replace_Should_Reject_Invalid_EvidenceVersion(string evidenceVersion)
        {
            var fixture = new Fixture();
            using var stream = new MemoryStream([0x25, 0x50, 0x44, 0x46, 0x2D]);
            var values = new Dictionary<string, StringValues> { ["movementVersion"] = "1" };
            if (evidenceVersion != null)
                values["evidenceVersion"] = evidenceVersion;
            var context = CreateContext(values, [CreateFile(stream)]);

            var action = () =>
                ReplaceMovementEvidenceEndpoint.DoAsync(
                    fixture.BusinessId,
                    fixture.MovementId,
                    fixture.EvidenceId,
                    fixture.Sender.Object,
                    fixture.User.Object,
                    context,
                    CancellationToken.None
                );

            await action.Should().ThrowAsync<BadHttpRequestException>();
        }

        private static DefaultHttpContext CreateContext(
            Dictionary<string, StringValues> values,
            IReadOnlyCollection<IFormFile> inputFiles
        )
        {
            var context = new DefaultHttpContext();
            context.Request.ContentType = "multipart/form-data; boundary=test-boundary";
            var files = new FormFileCollection();
            foreach (var file in inputFiles)
                files.Add(file);
            context.Features.Set<IFormFeature>(new FormFeature(new FormCollection(values, files)));
            return context;
        }

        private static FormFile CreateFile(MemoryStream stream) =>
            new(stream, 0, stream.Length, "file", "receipt.pdf")
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf",
            };

        private sealed class Fixture
        {
            public Guid BusinessId { get; } = Guid.NewGuid();
            public Guid MovementId { get; } = Guid.NewGuid();
            public Guid EvidenceId { get; } = Guid.NewGuid();
            public Mock<ISender> Sender { get; } = new();
            public Mock<IUserContextProvider> User { get; } = new();
        }
    }
}
