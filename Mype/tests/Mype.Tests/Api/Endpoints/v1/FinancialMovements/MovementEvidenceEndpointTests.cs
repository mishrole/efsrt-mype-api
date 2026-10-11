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
using Mype.Application.Common;
using Mype.Application.MovementEvidences.Commands.AttachMovementEvidence;
using Mype.Application.MovementEvidences.Commands.RemoveMovementEvidence;
using Mype.Application.MovementEvidences.Commands.ReplaceMovementEvidence;
using Mype.Application.MovementEvidences.Models;
using Mype.Application.MovementEvidences.Queries.ListMovementEvidences;
using Mype.Application.MovementEvidences.Queries.OpenMovementEvidence;
using Mype.Domain.MovementEvidences;

namespace Mype.Tests.Api.Endpoints.v1.FinancialMovements
{
    public sealed class MovementEvidenceEndpointTests
    {
        [Fact]
        public async Task Attach_Should_Read_Multipart_Send_Command_Dispose_Stream_And_Return_Created()
        {
            var fixture = new Fixture();
            using var stream = new MemoryStream(PdfBytes());
            var context = CreateMultipartContext(stream, "receipt.pdf", "application/pdf", "7");
            AttachMovementEvidenceCommand captured = null;
            fixture
                .Sender.Setup(x =>
                    x.Send(It.IsAny<AttachMovementEvidenceCommand>(), It.IsAny<CancellationToken>())
                )
                .Callback(
                    (IRequest<Result<MovementEvidenceResult>> request, CancellationToken _) =>
                        captured = (AttachMovementEvidenceCommand)request
                )
                .ReturnsAsync(Result<MovementEvidenceResult>.Success(fixture.Result()));

            var response = await AttachMovementEvidenceEndpoint.DoAsync(
                fixture.BusinessId,
                fixture.MovementId,
                fixture.Sender.Object,
                fixture.User.Object,
                context,
                CancellationToken.None
            );

            response
                .Should()
                .BeAssignableTo<IStatusCodeHttpResult>()
                .Which.StatusCode.Should()
                .Be(StatusCodes.Status201Created);
            captured.BusinessId.Should().Be(fixture.BusinessId);
            captured.MovementId.Should().Be(fixture.MovementId);
            captured.CurrentUserId.Should().Be(fixture.UserId);
            captured.MovementVersion.Should().Be(7);
            captured.File.OriginalFileName.Should().Be("receipt.pdf");
            captured.File.DeclaredContentType.Should().Be("application/pdf");
        }

        [Fact]
        public async Task Replace_Should_Read_EvidenceVersion_Send_Command_Dispose_Stream_And_Return_Ok()
        {
            var fixture = new Fixture();
            using var stream = new MemoryStream(PdfBytes());
            var context = CreateMultipartContext(
                stream,
                "replacement.pdf",
                "application/pdf",
                "9",
                "8"
            );
            ReplaceMovementEvidenceCommand captured = null;
            fixture
                .Sender.Setup(x =>
                    x.Send(
                        It.IsAny<ReplaceMovementEvidenceCommand>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .Callback(
                    (IRequest<Result<MovementEvidenceResult>> request, CancellationToken _) =>
                        captured = (ReplaceMovementEvidenceCommand)request
                )
                .ReturnsAsync(Result<MovementEvidenceResult>.Success(fixture.Result()));

            var response = await ReplaceMovementEvidenceEndpoint.DoAsync(
                fixture.BusinessId,
                fixture.MovementId,
                fixture.EvidenceId,
                fixture.Sender.Object,
                fixture.User.Object,
                context,
                CancellationToken.None
            );

            response
                .Should()
                .BeAssignableTo<IStatusCodeHttpResult>()
                .Which.StatusCode.Should()
                .Be(StatusCodes.Status200OK);
            captured.EvidenceId.Should().Be(fixture.EvidenceId);
            captured.MovementVersion.Should().Be(9);
            captured.EvidenceVersion.Should().Be(8);
            captured.CurrentUserId.Should().Be(fixture.UserId);
        }

        [Fact]
        public async Task Remove_Should_Send_Command_And_Return_NoContent()
        {
            var fixture = new Fixture();
            RemoveMovementEvidenceCommand captured = null;
            fixture
                .Sender.Setup(x =>
                    x.Send(It.IsAny<RemoveMovementEvidenceCommand>(), It.IsAny<CancellationToken>())
                )
                .Callback(
                    (IRequest<Result<MovementEvidenceResult>> request, CancellationToken _) =>
                        captured = (RemoveMovementEvidenceCommand)request
                )
                .ReturnsAsync(Result<MovementEvidenceResult>.Success(fixture.Result()));

            var response = await RemoveMovementEvidenceEndpoint.DoAsync(
                fixture.BusinessId,
                fixture.MovementId,
                fixture.EvidenceId,
                new RemoveMovementEvidenceEndpoint.Request(11, 10),
                fixture.Sender.Object,
                fixture.User.Object,
                new DefaultHttpContext(),
                CancellationToken.None
            );

            response
                .Should()
                .BeAssignableTo<IStatusCodeHttpResult>()
                .Which.StatusCode.Should()
                .Be(StatusCodes.Status204NoContent);
            captured.BusinessId.Should().Be(fixture.BusinessId);
            captured.MovementId.Should().Be(fixture.MovementId);
            captured.EvidenceId.Should().Be(fixture.EvidenceId);
            captured.CurrentUserId.Should().Be(fixture.UserId);
            captured.MovementVersion.Should().Be(11);
            captured.EvidenceVersion.Should().Be(10);
        }

        [Fact]
        public async Task List_Should_Send_Query_And_Return_Ok()
        {
            var fixture = new Fixture();
            ListMovementEvidencesQuery captured = null;
            fixture
                .Sender.Setup(x =>
                    x.Send(It.IsAny<ListMovementEvidencesQuery>(), It.IsAny<CancellationToken>())
                )
                .Callback(
                    (
                        IRequest<Result<IReadOnlyCollection<MovementEvidenceResult>>> request,
                        CancellationToken _
                    ) => captured = (ListMovementEvidencesQuery)request
                )
                .ReturnsAsync(
                    Result<IReadOnlyCollection<MovementEvidenceResult>>.Success([fixture.Result()])
                );

            var response = await ListMovementEvidencesEndpoint.DoAsync(
                fixture.BusinessId,
                fixture.MovementId,
                fixture.Sender.Object,
                fixture.User.Object,
                new DefaultHttpContext(),
                CancellationToken.None
            );

            response
                .Should()
                .BeAssignableTo<IStatusCodeHttpResult>()
                .Which.StatusCode.Should()
                .Be(StatusCodes.Status200OK);
            captured.BusinessId.Should().Be(fixture.BusinessId);
            captured.MovementId.Should().Be(fixture.MovementId);
            captured.CurrentUserId.Should().Be(fixture.UserId);
        }

        [Fact]
        public async Task Open_Should_Send_Query_And_Return_Ok()
        {
            var fixture = new Fixture();
            OpenMovementEvidenceQuery captured = null;
            fixture
                .Sender.Setup(x =>
                    x.Send(It.IsAny<OpenMovementEvidenceQuery>(), It.IsAny<CancellationToken>())
                )
                .Callback(
                    (
                        IRequest<Result<TemporaryEvidenceAccessResult>> request,
                        CancellationToken _
                    ) => captured = (OpenMovementEvidenceQuery)request
                )
                .ReturnsAsync(
                    Result<TemporaryEvidenceAccessResult>.Success(
                        new(new Uri("https://storage.test/read"), fixture.Now.AddMinutes(5))
                    )
                );

            var response = await OpenMovementEvidenceEndpoint.DoAsync(
                fixture.BusinessId,
                fixture.MovementId,
                fixture.EvidenceId,
                fixture.Sender.Object,
                fixture.User.Object,
                new DefaultHttpContext(),
                CancellationToken.None
            );

            response
                .Should()
                .BeAssignableTo<IStatusCodeHttpResult>()
                .Which.StatusCode.Should()
                .Be(StatusCodes.Status200OK);
            captured.BusinessId.Should().Be(fixture.BusinessId);
            captured.MovementId.Should().Be(fixture.MovementId);
            captured.EvidenceId.Should().Be(fixture.EvidenceId);
            captured.CurrentUserId.Should().Be(fixture.UserId);
        }

        private static DefaultHttpContext CreateMultipartContext(
            MemoryStream stream,
            string fileName,
            string contentType,
            string movementVersion,
            string evidenceVersion = null
        )
        {
            var context = new DefaultHttpContext();
            context.Request.ContentType = "multipart/form-data; boundary=test-boundary";
            var file = new FormFile(stream, 0, stream.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType,
            };
            var files = new FormFileCollection { file };
            var values = new Dictionary<string, StringValues>
            {
                ["movementVersion"] = movementVersion,
            };
            if (evidenceVersion != null)
                values["evidenceVersion"] = evidenceVersion;
            context.Features.Set<IFormFeature>(new FormFeature(new FormCollection(values, files)));
            return context;
        }

        private static byte[] PdfBytes() => [0x25, 0x50, 0x44, 0x46, 0x2D];

        private sealed class Fixture
        {
            public Guid BusinessId { get; } = Guid.NewGuid();
            public Guid MovementId { get; } = Guid.NewGuid();
            public Guid EvidenceId { get; } = Guid.NewGuid();
            public Guid UserId { get; } = Guid.NewGuid();
            public DateTimeOffset Now { get; } = new(2026, 10, 10, 20, 0, 0, TimeSpan.Zero);
            public Mock<ISender> Sender { get; } = new();
            public Mock<IUserContextProvider> User { get; } = new();

            public Fixture()
            {
                User.Setup(x => x.GetCurrentUserId()).Returns(UserId);
            }

            public MovementEvidenceResult Result() =>
                new(
                    EvidenceId,
                    MovementId,
                    null,
                    "receipt.pdf",
                    "application/pdf",
                    5,
                    MovementEvidenceStatus.Active,
                    null,
                    Now,
                    null,
                    null,
                    null,
                    1
                );
        }
    }
}
