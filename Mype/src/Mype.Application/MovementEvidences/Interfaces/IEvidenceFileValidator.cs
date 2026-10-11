using Mype.Application.Common;
using Mype.Application.MovementEvidences.Models;

namespace Mype.Application.MovementEvidences.Interfaces
{
    public interface IEvidenceFileValidator
    {
        Result<ValidatedEvidenceFile> Validate(EvidenceFile file);
    }
}
