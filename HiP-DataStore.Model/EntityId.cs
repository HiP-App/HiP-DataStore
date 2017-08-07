using System;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model
{
    /// <summary>
    /// Uniquely identifies an entity by its type and ID.
    /// EntityId can be converted to and from a (ResourceType, int)-tuple.
    /// </summary>
    public struct EntityId : IEquatable<EntityId>
    {
        public ResourceType Type { get; }

        public int Id { get; }

        public EntityId(ResourceType type, int id)
        {
            Type = type;
            Id = id;
        }

        /// <summary>
        /// Enables tuple deconstruction, e.g. "var (theType, theId) = [some EntityId instance];".
        /// </summary>
        public void Deconstruct(out ResourceType type, out int id)
        {
            type = Type;
            id = Id;
        }

        public override int GetHashCode() => (Type, Id).GetHashCode();

        public override bool Equals(object obj) => obj is EntityId other && Equals(other);

        public bool Equals(EntityId other) => Type == other.Type && Id == other.Id;

        
        public static bool operator ==(EntityId a, EntityId b) => Equals(a, b);

        public static bool operator !=(EntityId a, EntityId b) => !Equals(a, b);

        /// <summary>
        /// Enables <see cref="EntityId"/>-construction from a tuple, e.g. "EntityId x = (ResourceType.Tag, 17);".
        /// </summary>
        public static implicit operator EntityId((ResourceType type, int id) tuple) => new EntityId(tuple.type, tuple.id);

        public override string ToString() => $"{Type?.Name} {Id}";
    }
}
