using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using PaderbornUniversity.SILab.Hip.EventSourcing.Migrations;
using System.Threading.Tasks;

namespace PaderbornUniversity.SILab.Hip.DataStore.Migrations
{
    /// <summary>
    /// Updating to version 6. Version 6 adds the ability to specify an access radius (in km) for an exhibit.
    /// In prior versions, exhibits don't have this property, so a default value is assigned in this migration.
    /// </summary>
    [StreamMigration(from: 5, to: 6)]
    public class Migration6AccessRadius : IStreamMigration
    {
        public async Task MigrateAsync(IStreamMigrationArgs e)
        {
            var events = e.GetExistingEvents();

            while (await events.MoveNextAsync())
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (events.Current is ExhibitCreated exhibitCreated && exhibitCreated.Properties.AccessRadius == 0)
                {
                    // If AccessRadius is still 0, we assume that this exhibit was created in an earlier version
                    // of DataStore => thus, we now assign it a default radius of 10 meters
                    exhibitCreated.Properties.AccessRadius = .01f;
                }

                e.AppendEvent(events.Current);
            }
        }
    }
}
