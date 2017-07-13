using EventStore.ClientAPI;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PaderbornUniversity.SILab.Hip.DataStore.Core
{
    /// <summary>
    /// A transaction which aggregates multiple events in order to emit them in a single batch.
    /// </summary>
    public class EventStoreClientTransaction : IDisposable
    {
        private readonly EventStoreClient _client;
        private readonly List<IEvent> _events = new List<IEvent>();
        private bool _isCommitted;
        private bool _isDisposed;

        public EventStoreClientTransaction(EventStoreClient client)
        {
            _client = client;
        }

        public void Append(IEvent ev)
        {
            VerifyState();
            _events.Add(ev);
        }

        public void Append(IEnumerable<IEvent> events)
        {
            VerifyState();
            _events.AddRange(events);
        }

        /// <summary>
        /// Persists all events added to this transaction in the Event Store stream.
        /// </summary>
        /// <returns></returns>
        public async Task<WriteResult> CommitAsync()
        {
            VerifyState();
            _isCommitted = true;
            return await _client.AppendEventsAsync(_events);
        }

        private void VerifyState()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(EventStoreClientTransaction));

            if (_isCommitted)
                throw new InvalidOperationException("A commit has already been executed");
        }

        public void Dispose() => _isDisposed = true;
    }
}
