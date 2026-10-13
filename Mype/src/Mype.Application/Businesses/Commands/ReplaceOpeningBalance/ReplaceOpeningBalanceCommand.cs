using System;
using MediatR;
using Mype.Application.Common;

namespace Mype.Application.Businesses.Commands.ReplaceOpeningBalance
{
    public sealed class ReplaceOpeningBalanceCommand : IRequest<Result<ReplaceOpeningBalanceResult>>
    {
        public Guid BusinessId { get; set; }
        public Guid CurrentUserId { get; set; }
        public decimal Amount { get; set; }
        public string Reason { get; set; }
        public uint MovementVersion { get; set; }
    }

    public sealed record ReplaceOpeningBalanceResult(
        Guid Id,
        decimal Amount,
        DateOnly MovementDate,
        uint Version
    );
}
