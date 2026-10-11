using System;
using Mype.Application.MovementEvidences.Interfaces;

namespace Mype.Application.MovementEvidences.Common
{
    public sealed class StorageObjectKeyGenerator : IStorageObjectKeyGenerator
    {
        public string Generate(
            Guid businessId,
            Guid movementId,
            Guid evidenceId,
            string extension
        ) =>
            $"businesses/{businessId:N}/movements/{movementId:N}/evidences/{evidenceId:N}/{Guid.NewGuid():N}{extension}";
    }
}
