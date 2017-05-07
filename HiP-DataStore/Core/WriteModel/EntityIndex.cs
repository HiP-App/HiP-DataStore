using System;
using System.Collections.Generic;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using PaderbornUniversity.SILab.Hip.DataStore.Model;

namespace PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel
{
    /// <summary>
    /// Caches the IDs and the publication statuses of all entities.
    /// </summary>
    public class EntityIndex : IDomainIndex
    {
        private readonly Dictionary<Type, EntityTypeInfo> _types = new Dictionary<Type, EntityTypeInfo>();
        private readonly object _lockObject = new object();

        /// <summary>
        /// Gets a new, never-used-before ID for a new entity of the specified type.
        /// </summary>
        public int NextId<T>()
        {
            lock (_lockObject)
            {
                var info = GetOrCreateEntityTypeInfo(typeof(T));
                return ++info.MaximumId;
            }
        }

        /// <summary>
        /// Gets the current status of an entity given its type and ID.
        /// </summary>
        public ContentStatus? Status<T>(int id)
        {
            lock (_lockObject)
            {
                var info = GetOrCreateEntityTypeInfo(typeof(T));

                if (info.Entities.TryGetValue(id, out var entity))
                    return entity.Status;

                return null;
            }
        }

        public void ApplyEvent(IEvent e)
        {
            switch (e)
            {
                case ICreateEvent ev:
                    lock (_lockObject)
                    {
                        var info = GetOrCreateEntityTypeInfo(ev.EntityType);
                        info.MaximumId = Math.Max(info.MaximumId, ev.Id);
                        info.Entities.Add(ev.Id, new EntityInfo { Status = ev.Status });
                    }
                    break;

                case IUpdateEvent ev2:
                    lock (_lockObject)
                    {
                        var info2 = GetOrCreateEntityTypeInfo(ev2.EntityType);
                        if (info2.Entities.TryGetValue(ev2.Id, out var entity))
                            entity.Status = ev2.Status;
                    }
                    break;

                case IDeleteEvent ev3:
                    lock (_lockObject)
                    {
                        var info3 = GetOrCreateEntityTypeInfo(ev3.EntityType);
                        info3.Entities.Remove(ev3.Id);
                    }
                    break;
            }
        }

        private EntityTypeInfo GetOrCreateEntityTypeInfo(Type entityType)
        {
            if (_types.TryGetValue(entityType, out var info))
                return info;

            return _types[entityType] = new EntityTypeInfo();
        }

        class EntityTypeInfo
        {
            /// <summary>
            /// The largest ID ever assigned to an entity of the type.
            /// </summary>
            public int MaximumId { get; set; } = -1;

            /// <summary>
            /// Stores only the most basic information about all entities of the type.
            /// It is assumed that this easily fits in RAM.
            /// </summary>
            public Dictionary<int, EntityInfo> Entities { get; } = new Dictionary<int, EntityInfo>();
        }

        class EntityInfo
        {
            public ContentStatus Status { get; set; }
        }
    }
}
