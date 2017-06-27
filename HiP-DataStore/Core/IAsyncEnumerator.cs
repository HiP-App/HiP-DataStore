using System.Threading.Tasks;

namespace PaderbornUniversity.SILab.Hip.DataStore.Core
{
    /// <summary>
    /// An asynchronous enumerator interface, similar to the built-in synchronous
    /// <see cref="System.Collections.Generic.IEnumerator{T}"/> interface.
    /// </summary>
    public interface IAsyncEnumerator<out T>
    {
        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        T Current { get; }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>True if successfully moved.</returns>
        Task<bool> MoveNextAsync();

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        void Reset();
    }
}
