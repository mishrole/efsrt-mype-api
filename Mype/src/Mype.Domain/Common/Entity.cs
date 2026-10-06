using System;

namespace Mype.Domain.Common
{
    public abstract class Entity
    {
        protected Entity() { }

        protected Entity(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; protected set; }

        public DateTimeOffset CreatedAt { get; protected set; }

        public DateTimeOffset UpdatedAt { get; protected set; }
    }
}
