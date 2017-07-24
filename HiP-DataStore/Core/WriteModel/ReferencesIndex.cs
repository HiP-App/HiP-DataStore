using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using System.Collections.Generic;
using System.Linq;
using System;
using PaderbornUniversity.SILab.Hip.DataStore.Model;

namespace PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel
{
    public class ReferencesIndex : IDomainIndex
    {
        // for each entity X, this stores the references from X to other entities
        private readonly Dictionary<Entry, HashSet<Entry>> _referencesOf = new Dictionary<Entry, HashSet<Entry>>();

        // for each entity X, this stores the entities referencing X
        private readonly Dictionary<Entry, HashSet<Entry>> _referencesTo = new Dictionary<Entry, HashSet<Entry>>();

        // for each entity this stores the date and time of last modification (through POST and PUT APIs)
        private readonly Dictionary<Entry, DateTimeOffset> _lastModificationCascading = new Dictionary<Entry, DateTimeOffset>();

        /// <summary>
        /// Checks whether an entity is referenced by any other entities.
        /// </summary>
        /// <returns>True if there are references to the entity</returns>
        public bool IsUsed(ResourceType type, int id)
        {
            var key = new Entry(type, id);
            return _referencesTo.TryGetValue(key, out var set) && set.Count > 0;
        }

        /// <summary>
        /// Returns the IDs of the entities referenced by the entity with the specified type and ID.
        /// </summary>
        public IReadOnlyCollection<Entry> ReferencesOf(ResourceType type, int id)
        {
            var key = new Entry(type, id);
            return _referencesOf.TryGetValue(key, out var set) ? set.ToArray() : Array.Empty<Entry>();
        }

        /// <summary>
        /// Returns the IDs of the entities that reference the entity with the specified type and ID.
        /// </summary>
        public IReadOnlyCollection<Entry> ReferencesTo(ResourceType type, int id)
        {
            var key = new Entry(type, id);
            return _referencesTo.TryGetValue(key, out var set) ? set.ToArray() : Array.Empty<Entry>();
        }

        /// <summary>
        /// Returns a timestamp indicating the last time the entity with the specified type and ID or
        /// any directly or indirectly referenced entity was modified (i.e. created or updated).
        /// </summary>
        public DateTimeOffset LastModificationCascading(ResourceType type, int id) =>
            _lastModificationCascading[(type, id)];

        public void ApplyEvent(IEvent ev)
        {
            if (ev is ReferenceEventBase refEvent)
            {
                var source = new Entry(refEvent.SourceType, refEvent.SourceId);
                var target = new Entry(refEvent.TargetType, refEvent.TargetId);

                switch (refEvent)
                {
                    case ReferenceAdded _:
                        GetOrCreateSet(_referencesOf, source).Add(target);
                        GetOrCreateSet(_referencesTo, target).Add(source);
                        break;

                    case ReferenceRemoved _:
                        var referencesOfSource = GetOrCreateSet(_referencesOf, source);
                        referencesOfSource.Remove(target);
                        if (referencesOfSource.Count == 0)
                            _referencesOf.Remove(source);

                        var referencersOfTarget = GetOrCreateSet(_referencesTo, target);
                        referencersOfTarget.Remove(source);
                        if (referencersOfTarget.Count == 0)
                            _referencesTo.Remove(target);
                        break;
                }
            }
            else
            {
                switch (ev)
                {
                    case ICreateEvent e:
                        SetTimestampRecursively(e.Timestamp, e.GetEntityType(), e.Id);
                        break;

                    case IUpdateEvent e:
                        SetTimestampRecursively(e.Timestamp, e.GetEntityType(), e.Id);
                        break;

                    case IDeleteEvent e:
                        SetTimestampRecursively(e.Timestamp, e.GetEntityType(), e.Id);
                        _lastModificationCascading.Remove((e.GetEntityType(), e.Id));
                        break;
                }
            }
        }

        private void SetTimestampRecursively(DateTimeOffset timestamp, ResourceType entityType, int id)
        {
            var visitedEntities = new HashSet<Entry>();
            var queue = new Queue<Entry>();
            queue.Enqueue((entityType, id));

            // Do a breadth-first search through the graph of references.
            // Using 'visitedEntities' it is ensured that each entity is only visited once, even if there are
            // cyclic references in the graph (such as a page A referencing a page B and vice versa).
            while (queue.Count > 0)
            {
                var currentEntity = queue.Dequeue();
                _lastModificationCascading[currentEntity] = timestamp;
                visitedEntities.Add(currentEntity);

                var parents = ReferencesTo(currentEntity.Type, currentEntity.Id)
                    .Where(e => !visitedEntities.Contains(e));

                foreach (var parent in parents)
                    queue.Enqueue(currentEntity);
            }
        }
        
        private HashSet<Entry> GetOrCreateSet(Dictionary<Entry, HashSet<Entry>> dict, Entry entity)
        {
            if (dict.TryGetValue(entity, out var set))
                return set;

            return dict[entity] = new HashSet<Entry>();
        }

        public struct Entry
        {
            public ResourceType Type { get; }
            public int Id { get; }

            public Entry(ResourceType type, int id)
            {
                Type = type;
                Id = id;
            }

            public static implicit operator Entry((ResourceType type, int id) tuple) =>
                new Entry(tuple.type, tuple.id);
        }
    }
}
