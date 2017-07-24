using PaderbornUniversity.SILab.Hip.DataStore.Core.Migrations;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using System;
using System.Threading.Tasks;

namespace PaderbornUniversity.SILab.Hip.DataStore.Migrations
{
    /// <summary>
    /// Updates a stream to version 3. Version 3 introduces timestamps in <see cref="IDeleteEvent"/>.
    /// In previous versions, timestamps were only stored for <see cref="ICreateEvent"/> and
    /// <see cref="IUpdateEvent"/>.
    /// </summary>
    [StreamMigration(from: 2, to: 3)]
    public class DeleteEventTimestampsMigration : IStreamMigration
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
                        // In v2, IDeleteEvents did not store a timestamp.
                        // For v3, we have to choose a "realistic" timestamp
                        // => just use the timestamp of the last CRUD event
                        ev.Timestamp = lastTimestamp;
                        break;

                    case ICrudEvent ev: lastTimestamp = ev.Timestamp; break;
                }

                e.AppendEvent(events.Current);
            }
        }
    }
}
