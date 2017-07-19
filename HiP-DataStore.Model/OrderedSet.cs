using System.Collections;
using System.Collections.Generic;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model
{
    /// <summary>
    /// A hash set that preserves insertion order while still allowing basic set operations in O(1).
    /// Adapted from https://www.codeproject.com/Articles/627085/HashSet-that-Preserves-Insertion-Order-or-NET.
    /// </summary>
    public class OrderedSet<T> : ICollection<T>
    {
        private readonly IDictionary<T, LinkedListNode<T>> _dictionary;
        private readonly LinkedList<T> _linkedList = new LinkedList<T>();

        public OrderedSet() : this(EqualityComparer<T>.Default)
        {
        }

        public OrderedSet(IEqualityComparer<T> comparer)
        {
            _dictionary = new Dictionary<T, LinkedListNode<T>>(comparer);
        }

        public int Count => _dictionary.Count;

        public virtual bool IsReadOnly => false;

        void ICollection<T>.Add(T item) => Add(item);

        public bool Add(T item)
        {
            if (_dictionary.ContainsKey(item))
                return false;

            var node = _linkedList.AddLast(item);
            _dictionary.Add(item, node);
            return true;
        }

        public void Clear()
        {
            _linkedList.Clear();
            _dictionary.Clear();
        }

        public bool Remove(T item)
        {
            if (!_dictionary.TryGetValue(item, out var node))
                return false;

            _dictionary.Remove(item);
            _linkedList.Remove(node);
            return true;
        }

        public IEnumerator<T> GetEnumerator() => _linkedList.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool Contains(T item) => _dictionary.ContainsKey(item);

        public void CopyTo(T[] array, int arrayIndex) => _linkedList.CopyTo(array, arrayIndex);
    }
}
