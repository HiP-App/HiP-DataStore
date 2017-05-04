using System;
using System.Collections.Generic;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;

namespace PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel
{
    /// <summary>
    /// For each type of entity, this index stores the largest ID assigned to any entity of that type.
    /// Based on that, new, incremental IDs can be generated.
    /// </summary>
    public class IdIndex : IDomainIndex
    {
        private readonly Dictionary<Type, int> _maxIdByType = new Dictionary<Type, int>();
        private readonly object _lockObject = new object();

        public int NextId<T>()
        {
            lock (_lockObject)
            {
                var currentMaxId = _maxIdByType.TryGetValue(typeof(T), out var id) ? id : -1;
                _maxIdByType[typeof(T)] = currentMaxId + 1;
                return currentMaxId + 1;
            }
        }

        public void ApplyEvent(IEvent e)
        {
            if (e is ICrudEvent crudEvent)
            {
                lock (_lockObject)
                {
                    var currentMaxId = _maxIdByType.TryGetValue(crudEvent.EntityType, out var id) ? id : -1;
                    _maxIdByType[crudEvent.EntityType] = Math.Max(currentMaxId, crudEvent.Id);
                }
            }
        }
    }
}