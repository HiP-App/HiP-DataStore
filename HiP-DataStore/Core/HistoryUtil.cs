using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using System;
using System.Collections.Generic;
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
                if (enumerator.Current is ICrudEvent crudEvent &&
                    crudEvent.GetEntityType() == entityId.Type && crudEvent.Id == entityId.Id)
                {
                    var timestamp = crudEvent.Timestamp;
                    var user = (crudEvent as IUserActivityEvent).UserId;

                    switch (crudEvent)
                    {
                        case ICreateEvent createEvent:
                            if (summary.Created.HasValue)
                            {
                                // assumption: entity was deleted before and is now recreated (we don't check if there
                                // was a delete event before; it's not our job to validate the stream's consistency)
                                summary = new HistorySummary();
                            }

                            summary.Owner = user;
                            summary.Created = timestamp;
                            summary.LastModified = timestamp;
                            summary.Changes.Add(new Change(timestamp, "Created", user));
                            break;

                        case IUpdateEvent updateEvent:
                            summary.LastModified = timestamp;
                            summary.Changes.Add(new Change(timestamp, "Updated", user));
                            break;

                        case IDeleteEvent deleteEvent:
                            summary.LastModified = timestamp;
                            summary.Deleted = timestamp;
                            summary.Changes.Add(new Change(timestamp, "Deleted", user));
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

    public class HistorySummary
    {
        /// <summary>
        /// The time and date when the entity was created.
        /// If the entity is effectively non-existent (i.e. has never been created or has been deleted),
        /// no value is set.
        /// </summary>
        public DateTimeOffset? Created { get; set; }

        /// <summary>
        /// The time and date of the last creation, update or deletion.
        /// If the entity is effectively non-existent (i.e. has never been created or has been deleted),
        /// no value is set.
        /// </summary>
        public DateTimeOffset? LastModified { get; set; }

        /// <summary>
        /// The time and date when the entity was deleted.
        /// A value is only set if the entity has been created and deleted before, but not (yet) recreated.
        /// </summary>
        public DateTimeOffset? Deleted { get; set; }

        /// <summary>
        /// The ID of the user who created the entity.
        /// </summary>
        public string Owner { get; set; }

        /// <summary>
        /// A list of individual modifications (including creation and deletion).
        /// </summary>
        public IList<Change> Changes { get; set; } = new List<Change>();
    }

    public class Change
    {
        public DateTimeOffset Timestamp { get; set; }
        public string Description { get; set; }
        public string UserId { get; set; }

        public Change(DateTimeOffset timestamp, string description, string userId)
        {
            Timestamp = timestamp;
            Description = description;
            UserId = userId;
        }
    }
}
