using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using PaderbornUniversity.SILab.Hip.EventSourcing.Migrations;
using System;
using System.Threading.Tasks;

#pragma warning disable CS0612 // We explicitly work with obsolete types here, so disable warnings for that
namespace PaderbornUniversity.SILab.Hip.DataStore.Migrations
{
    /// <summary>
    /// Updates a stream to version 4. Version 4 completely removes ReferenceAdded and ReferenceRemoved events
    /// and introduces timestamps in <see cref="IDeleteEvent"/>. In previous versions, timestamps were only
    /// stored for <see cref="ICreateEvent"/> and <see cref="IUpdateEvent"/>.
    /// </summary>
    [StreamMigration(from: 3, to: 4)]
    public class Migration4RemoveReferenceEventsAndAddDeleteEventTimestamps : IStreamMigration
    {
        public async Task MigrateAsync(IStreamMigrationArgs e)
        {
            var events = e.GetExistingEvents();
            var lastTimestamp = DateTimeOffset.MinValue;

            while (await events.MoveNextAsync())
            {
                switch (events.Current)
                {
                    case IDeleteEvent ev:
                        // In v3, IDeleteEvents did not store a timestamp.
                        // For v4, we have to choose a "realistic" timestamp
                        // => just use the timestamp of the last CRUD event
                        ev.Timestamp = lastTimestamp;
                        e.AppendEvent(ev);
                        break;

                    case ICrudEvent ev:
                        lastTimestamp = ev.Timestamp;
                        e.AppendEvent(ev);
                        break;

                    case ReferenceAdded _:
                        // Ignore, do not add ReferenceAdded/Removed events to the v4 stream
                        break;

                    case ReferenceRemoved _:
                        // Ignore, do not add ReferenceAdded/Removed events to the v4 stream
                        break;

                    default:
                        // All other events are copied without modifications
                        e.AppendEvent(events.Current);
                        break;
                }
            }
        }
    }
}
#pragma warning restore CS0612