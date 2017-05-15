using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using System.Collections.Generic;
using System.Linq;
using System;

namespace PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel
{
    public class ReferencesIndex : IDomainIndex
    {
        private static readonly Entry[] _noEntries = new Entry[0];

        // for each entity X, this stores the references from X to other entities
        private readonly Dictionary<Entry, HashSet<Entry>> _referencesOf;

        // for each entity X, this stores the entities referencing X
        private readonly Dictionary<Entry, HashSet<Entry>> _referencesTo;

        /// <summary>
        /// Checks whether an entity is referenced by any other entities.
        /// </summary>
        /// <returns>True if there are references to the entity</returns>
        public bool IsUsed<T>(int id)
        {
            var key = new Entry(typeof(T).GetMongoCollectionName(), id);
            return _referencesTo.TryGetValue(key, out var set) && set.Count > 0;
        }

        /// <summary>
        /// Returns the IDs of the entities referenced by the entity with the specified type and ID.
        /// </summary>
        public IReadOnlyCollection<Entry> ReferencesOf<T>(int id)
        {
            var key = new Entry(typeof(T).GetMongoCollectionName(), id);
            return _referencesOf.TryGetValue(key, out var set) ? set.ToArray() : Array.Empty<Entry>();
        }

        /// <summary>
        /// Returns the IDs of the entities that reference the entity with the specified type and ID.
        /// </summary>
        public IReadOnlyCollection<Entry> ReferencesTo<T>(int id)
        {
            var key = new Entry(typeof(T).GetMongoCollectionName(), id);
            return _referencesOf.TryGetValue(key, out var set) ? set.ToArray() : Array.Empty<Entry>();
        }

        public void ApplyEvent(IEvent ev)
        {
            if (ev is ReferenceEventBase e)
            {
                var source = new Entry(e.SourceCollectionName, e.SourceId);
                var target = new Entry(e.TargetCollectionName, e.TargetId);

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
            public string CollectionName { get; }
            public int Id { get; }

            public Entry(string collectionName, int id)
            {
                CollectionName = collectionName;
                Id = id;
            }
        }
    }
}
