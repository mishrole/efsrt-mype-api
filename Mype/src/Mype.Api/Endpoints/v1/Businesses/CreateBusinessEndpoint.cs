using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mype.Api.Context;
using Mype.Api.Extensions;
using Mype.Application.Businesses.Commands.CreateBusiness;
using System.Threading;
using System.Threading.Tasks;

namespace Mype.Api.Endpoints.v1.Businesses
{
    public static class CreateBusinessEndpoint
    {
        public sealed record CreateBusinessRequest(
            string DisplayName,
            string LegalName,
            string Ruc,
            string CurrencyCode
        );

        public static async Task<IResult> DoAsync(
            [FromBody] CreateBusinessRequest request,
            [FromServices] ISender sender,
            [FromServices] IUserContextProvider userContextProvider,
            HttpContext context,
            CancellationToken cancellationToken
        )
        {
            var command = new CreateBusinessCommand
            {
                DisplayName = request.DisplayName,
                LegalName = request.LegalName,
                Ruc = request.Ruc,
                CurrencyCode = request.CurrencyCode,
                CurrentUserId =
                    userContextProvider.GetCurrentUserId()
            };

            var result = await sender.Send(
                command,
                cancellationToken
            );

            return result.ToHttpResult(
                context,
                response => TypedResults.Created(
                    $"/api/v1/businesses/{response.BusinessId}",
                    response
                )
            );
        }
    }
}