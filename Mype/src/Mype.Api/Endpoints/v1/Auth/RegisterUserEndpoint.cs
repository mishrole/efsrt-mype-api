using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mype.Api.Extensions;
using Mype.Application.Auth.Commands.RegisterUser;

namespace Mype.Api.Endpoints.v1.Auth
{
    public static class RegisterUserEndpoint
    {
        public sealed record RegisterUserRequest(
            string DisplayName,
            string Email,
            string Password,
            string PasswordConfirmation
        );

        public static async Task<IResult> DoAsync(
            [FromBody] RegisterUserRequest request,
            [FromServices] ISender sender,
            HttpContext context,
            CancellationToken cancellationToken
        )
        {
            var command = new RegisterUserCommand
            {
                DisplayName = request.DisplayName,
                Email = request.Email,
                Password = request.Password,
                PasswordConfirmation = request.PasswordConfirmation,
            };

            var result = await sender.Send(command, cancellationToken);

            return result.ToHttpResult(
                context,
                value => TypedResults.Created($"/api/v1/users/{value.UserId}", value)
            );
        }
    }
}
