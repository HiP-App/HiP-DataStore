using System.Collections.Generic;
using System.Threading.Tasks;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using EventStore.ClientAPI;
using System;

namespace PaderbornUniversity.SILab.Hip.DataStore.Core
{
    /// <summary>
    /// Provides an easy-to-consume async enumerator to iterate over all events in an Event Store stream.
    /// </summary>
    public class EventStoreStreamEnumerator : IAsyncEnumerator<IEvent>
    {
        private const int PageSize = 4096; // only 4096 events can be retrieved in one call

        private readonly IEventStoreConnection _connection;
        private readonly string _streamName;
        private readonly Queue<IEvent> _buffer = new Queue<IEvent>();

        private long _startPosition = StreamPosition.Start;
        private bool _isEndOfStream;

        public IEvent Current => _buffer.Peek();

        public event EventHandler<EventParsingException> EventParsingFailed;

        public EventStoreStreamEnumerator(IEventStoreConnection connection, string streamName)
        {
            _connection = connection;
            _streamName = streamName;
        }

        public async Task<bool> MoveNextAsync()
        {
            if (_buffer.Count > 0)
            {
                _buffer.Dequeue();
                return _buffer.Count > 0;
            }

            if (_isEndOfStream)
                return false;

            // Why the 'while'? If the stream was soft-deleted in the past it starts at an event with number != 0.
            // So the first read operation returns 0 events, but reports the actual start position of the stream
            // (currentSlice.NextEventNumber). Then, a second read is required to actually read the first few events.
            while (_buffer.Count == 0 && !_isEndOfStream)
            {
                var currentSlice = await _connection.ReadStreamEventsForwardAsync(_streamName, _startPosition, PageSize, false);

                foreach (var eventData in currentSlice.Events)
                {
                    try
                    {
                        var ev = eventData.Event.ToIEvent().MigrateToLatestVersion();
                        _buffer.Enqueue(ev);
                    }
                    catch (ArgumentException e)
                    {
                        EventParsingFailed?.Invoke(this, new EventParsingException(eventData, e));
                    }
                }

                _startPosition = currentSlice.NextEventNumber;
                _isEndOfStream = currentSlice.IsEndOfStream;
            }

            return _buffer.Count > 0;
        }

        public void Reset()
        {
            _startPosition = 0;
            _isEndOfStream = false;
            _buffer.Clear();
        }
    }

    /// <summary>
    /// The exception that is thrown when raw event data cannot be deserialized into a corresponding CLR object.
    /// </summary>
    public class EventParsingException : Exception
    {
        public ResolvedEvent RawEvent { get; }

        public EventParsingException(ResolvedEvent rawEvent, Exception innerException)
            : base($"The event data of type '{rawEvent.Event.EventType}' could not be deserialized into " +
                   $"an instance of '{nameof(IEvent)}'", innerException)
        {
            RawEvent = rawEvent;
        }
    }
}
