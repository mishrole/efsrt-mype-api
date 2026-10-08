using MediatR;
using Mype.Application.Common;
using System;

namespace Mype.Application.Businesses.Commands.CreateBusiness
{
    public class CreateBusinessCommand
        : IRequest<Result<CreateBusinessResult>>
    {
        public string DisplayName { get; set; } = string.Empty;

        public string LegalName { get; set; }

        public string Ruc { get; set; }

        public string CurrencyCode { get; set; } = string.Empty;

        public Guid CurrentUserId { get; set; }
    }
}