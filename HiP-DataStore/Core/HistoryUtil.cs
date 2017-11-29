using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using PaderbornUniversity.SILab.Hip.EventSourcing.Events;
using System;
using System.Threading.Tasks;

namespace PaderbornUniversity.SILab.Hip.DataStore.Core
{
    public static class HistoryUtil
    {
        /// <summary>
        /// Walks through the specified event stream to obtain a summary of when the specified entity
        /// was created, modified and deleted. This is a potentially expensive operation.
        /// </summary>
        /// <remarks>
        /// This method only looks for standard CRUD events (<see cref="ICreateEvent"/>, <see cref="IUpdateEvent"/>,   
        /// <see cref="IDeleteEvent"/>). If for your specific entity type there are other event types the semantically
        /// represent a change to the entity, these won't be part of the generated summary.
        /// 
        /// In case the entity lived different lives (i.e. has been recreated after deletion), only the most
        /// recent "life" is considered for the history. Consider the following example event stream (with timestamps):
        /// create (12:00), update (12:04), delete (13:20), create (13:55), delete (14:00), create (16:10), update (16:25).
        /// In this case only the events from 16:10 and 16:25 will be considered and the property
        /// <see cref="HistorySummary.Deleted"/> will not be set.
        /// 
        /// The event stream is assumed to be consistent. If the stream is inconsistent (e.g. has a create event
        /// immediately followed by another create event), the behavior and resulting summary is undefined.
        /// </remarks>
        public static async Task<HistorySummary> GetSummaryAsync(IEventStream eventStream, EntityId entityId)
        {
            var enumerator = eventStream.GetEnumerator();
            var summary = new HistorySummary();

            while (await enumerator.MoveNextAsync())
            {
                if (enumerator.Current is EventBase baseEvent &&
                    baseEvent.GetEntityType() == entityId.Type && baseEvent.Id == entityId.Id)
                {
                    var timestamp = baseEvent.Timestamp;
                    var user = baseEvent.UserId;

                    switch (baseEvent)
                    {
                        case CreatedEvent _:
                            if (summary.Created.HasValue)
                            {
                                // assumption: entity was deleted before and is now recreated (we don't check if there
                                // was a delete event before; it's not our job to validate the stream's consistency)
                                summary = new HistorySummary();
                            }

                            summary.Owner = user;
                            summary.Created = timestamp;
                            summary.LastModified = timestamp;
                            summary.Changes.Add(new HistorySummary.Change(timestamp, "Created", user));
                            break;

                        case PropertyChangedEvent ev:
                            summary.LastModified = timestamp;
                            summary.Changes.Add(new HistorySummary.Change(timestamp, "Updated", user, ev.PropertyName, ev.Value));
                            break;

                        case DeletedEvent _:
                            summary.LastModified = timestamp;
                            summary.Deleted = timestamp;
                            summary.Changes.Add(new HistorySummary.Change(timestamp, "Deleted", user));
                            break;
                    }
                }
            }

            return summary;
        }

        /// <summary>
        /// Gets an entity as it was present at a specific point in time.
        /// </summary>
        public static Task<T> GetVersionAsync<T>(IEventStream eventStream, EntityId entityId, DateTimeOffset timestamp)
            where T : ContentBase
        {
            // TODO
            throw new NotImplementedException();
        }
    }
}
