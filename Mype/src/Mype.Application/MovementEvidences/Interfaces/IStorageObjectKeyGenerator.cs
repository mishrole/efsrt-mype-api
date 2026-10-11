using System;

namespace Mype.Application.MovementEvidences.Interfaces
{
    public interface IStorageObjectKeyGenerator
    {
        string Generate(Guid businessId, Guid movementId, Guid evidenceId, string extension);
    }
}
