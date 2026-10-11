using System;

namespace Mype.Domain.MovementEvidences
{
    public sealed class MovementEvidenceException : InvalidOperationException
    {
        public MovementEvidenceException(MovementEvidenceError error)
            : base(error.ToString())
        {
            Error = error;
        }

        public MovementEvidenceError Error { get; }
    }
}
