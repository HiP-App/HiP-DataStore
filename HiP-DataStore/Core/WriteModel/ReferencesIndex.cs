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
            return _referencesOf.TryGetValue(key, out var set) ? set.ToArray() : Array.Empty<Entry>();
        }

        public void ApplyEvent(IEvent ev)
        {
            if (ev is ReferenceEventBase e)
            {
                var source = new Entry(e.SourceType, e.SourceId);
                var target = new Entry(e.TargetType, e.TargetId);

                switch (e)
                {
                    case ReferenceAdded addedEvent:
                        GetOrCreateSet(_referencesOf, source).Add(target);
                        GetOrCreateSet(_referencesTo, target).Add(source);
                        break;

                    case ReferenceRemoved removedEvent:
                        var referencesOfSource = GetOrCreateSet(_referencesOf, source);
                        referencesOfSource.Remove(target);
                        if (referencesOfSource.Count == 0)
                            _referencesOf.Remove(source);

                        var referenceesOfTarget = GetOrCreateSet(_referencesTo, target);
                        referenceesOfTarget.Remove(source);
                        if (referenceesOfTarget.Count == 0)
                            _referencesOf.Remove(target);
                        break;
                }
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
        }
    }
}
