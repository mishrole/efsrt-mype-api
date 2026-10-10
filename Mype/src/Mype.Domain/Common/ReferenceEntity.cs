using System;

namespace Mype.Domain.Common
{
    public abstract class ReferenceEntity
    {
        protected ReferenceEntity() { }

        protected ReferenceEntity(Guid id, string code, string name, bool isActive)
        {
            Id = id;
            Code = code;
            Name = name;
            IsActive = isActive;
        }

        public Guid Id { get; protected set; }

        public string Code { get; protected set; } = string.Empty;

        public string Name { get; protected set; } = string.Empty;

        public bool IsActive { get; protected set; }
    }
}
