using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Mype.Application.MovementEvidences.Models;
using Mype.Shared.Constants;

namespace Mype.Api.Endpoints.v1.FinancialMovements
{
    internal static class MovementEvidenceEndpointRequestReader
    {
        internal static async Task<EvidenceUploadForm> ReadAsync(
            HttpRequest request,
            bool requireEvidenceVersion,
            CancellationToken cancellationToken
        )
        {
            if (!request.HasFormContentType)
                throw new BadHttpRequestException(ErrorMessages.ValidationFailed);

            var form = await request.ReadFormAsync(cancellationToken);
            var file = form.Files.GetFile("file");

            if (file == null || !uint.TryParse(form["movementVersion"], out var movementVersion))
                throw new BadHttpRequestException(ErrorMessages.ValidationFailed);

            uint evidenceVersion = 0;
            if (
                requireEvidenceVersion
                && !uint.TryParse(form["evidenceVersion"], out evidenceVersion)
            )
                throw new BadHttpRequestException(ErrorMessages.ValidationFailed);

            return new EvidenceUploadForm(
                new EvidenceFile(
                    file.FileName,
                    file.ContentType,
                    file.Length,
                    file.OpenReadStream()
                ),
                movementVersion,
                evidenceVersion
            );
        }

        internal sealed record EvidenceUploadForm(
            EvidenceFile File,
            uint MovementVersion,
            uint EvidenceVersion
        );
    }
}
