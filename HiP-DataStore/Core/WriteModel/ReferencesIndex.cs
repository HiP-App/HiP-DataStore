﻿using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using PaderbornUniversity.SILab.Hip.DataStore.Utility;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using PaderbornUniversity.SILab.Hip.EventSourcing.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel
{
    public class ReferencesIndex : IDomainIndex
    {
        // for each entity X, this stores the references from X to other entities
        private readonly Dictionary<EntityId, HashSet<EntityId>> _referencesOf = new Dictionary<EntityId, HashSet<EntityId>>();

        // for each entity X, this stores the entities referencing X
        private readonly Dictionary<EntityId, HashSet<EntityId>> _referencesTo = new Dictionary<EntityId, HashSet<EntityId>>();

        // for each entity this stores the date and time of last modification (through POST and PUT APIs)
        private readonly Dictionary<EntityId, DateTimeOffset> _lastModificationCascading = new Dictionary<EntityId, DateTimeOffset>();

        /// <summary>
        /// Checks whether an entity is referenced by any other entities.
        /// </summary>
        /// <returns>True if there are references to the entity</returns>
        public bool IsUsed(ResourceType type, int id)
        {
            var key = new EntityId(type, id);
            return _referencesTo.TryGetValue(key, out var set) && set.Count > 0;
        }

        /// <summary>
        /// Returns the IDs of the entities referenced by the entity with the specified type and ID.
        /// </summary>
        public IReadOnlyCollection<EntityId> ReferencesOf(ResourceType type, int id)
        {
            var key = new EntityId(type, id);
            return _referencesOf.TryGetValue(key, out var set) ? set.ToArray() : Array.Empty<EntityId>();
        }

        /// <summary>
        /// Returns the IDs of the entities that reference the entity with the specified type and ID.
        /// </summary>
        public IReadOnlyCollection<EntityId> ReferencesTo(ResourceType type, int id)
        {
            var key = new EntityId(type, id);
            return _referencesTo.TryGetValue(key, out var set) ? set.ToArray() : Array.Empty<EntityId>();
        }

        /// <summary>
        /// Returns a timestamp indicating the last time the entity with the specified type and ID or
        /// any directly or indirectly referenced entity was modified (i.e. created or updated).
        /// </summary>
        public DateTimeOffset LastModificationCascading(ResourceType type, int id) =>
            _lastModificationCascading[(type, id)];

        public void ApplyEvent(IEvent ev)
        {
            ResourceType resourceType = null;
            int eventId = -1;
            DateTimeOffset timestamp;
            Type type = null;

            if (ev is EventBase baseEvent)
            {
                resourceType = baseEvent.GetEntityType();
                eventId = baseEvent.Id;
                timestamp = baseEvent.Timestamp;
                type = baseEvent.GetType();
            }
            else if (ev is ICrudEvent crudEvent)
            {
                resourceType = crudEvent.GetEntityType();
                eventId = crudEvent.Id;
                timestamp = crudEvent.Timestamp;
                type = crudEvent.GetType();
            }
            else throw new ArgumentException("Unexpected event type occured!");


            // 1) Update references
            var source = (resourceType, eventId);

            switch (ev)
            {
                case PropertyChangedEvent e:
                    var references = e.GetReferences();
                    if (references.Any())
                    {
                        var targetResourceType = references.First().Type;
                        ClearReferences(source, targetResourceType);
                        AddReferences(source, references);
                    }
                    break;

                case DeletedEvent _:
                    ClearReferences(source);
                    break;

                case IUpdateEvent e:
                    ClearReferences(source);
                    AddReferences(source, e.GetReferences());
                    break;
            }



            // 2) Handle propagation of timestamps
            var hasDoNotPropagateAttribute = type.GetTypeInfo().CustomAttributes
                .Any(attr => attr.AttributeType == typeof(DoNotPropagateTimestampToReferencersAttribute));

            if (hasDoNotPropagateAttribute)
            {
                // only set timestamp on the created/updated/deleted entity
                _lastModificationCascading[(resourceType, eventId)] = timestamp;
            }
            else
            {
                // set timestamp & propagate to entities referencing the created/updated/deleted entity
                SetTimestampRecursively(timestamp, resourceType, eventId);
            }

        }

        private void ClearReferences(EntityId source, ResourceType targetResourceType = null)
        {
            var oldReferences = ReferencesOf(source.Type, source.Id);
            if (targetResourceType == null)
            {
                _referencesOf.Remove(source);
            }
            else if (_referencesOf.TryGetValue(source, out var set))
            {
                set.RemoveWhere(e => e.Type == targetResourceType);
            }

            foreach (var target in oldReferences)
            {
                if (_referencesTo.TryGetValue(target, out var set))
                {
                    if (targetResourceType != null && target.Type != targetResourceType) continue;
                    set.Remove(source);
                    if (set.Count == 0)
                        _referencesTo.Remove(target);
                }
            }
        }

        private void AddReferences(EntityId source, IEnumerable<EntityId> targets)
        {
            foreach (var target in targets)
            {
                GetOrCreateSet(_referencesOf, source).Add(target);
                GetOrCreateSet(_referencesTo, target).Add(source);
            }
        }

        private void SetTimestampRecursively(DateTimeOffset timestamp, ResourceType entityType, int id)
        {
            var visitedEntities = new HashSet<EntityId>();
            var queue = new Queue<EntityId>();
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
                    queue.Enqueue(parent);
            }
        }

        private HashSet<EntityId> GetOrCreateSet(Dictionary<EntityId, HashSet<EntityId>> dict, EntityId entity)
        {
            if (dict.TryGetValue(entity, out var set))
                return set;

            return dict[entity] = new HashSet<EntityId>();
        }
    }
}
