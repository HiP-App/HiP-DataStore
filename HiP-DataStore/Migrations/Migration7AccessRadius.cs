using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using PaderbornUniversity.SILab.Hip.EventSourcing.Migrations;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PaderbornUniversity.SILab.Hip.DataStore.Migrations
{
    /// <summary>
    /// Updating to version 7. In migration 6 we migrated ExhibitCreated-events to include a default AccessRadius, but
    /// we forgot to migrate ExhibitUpdated-events, so they still have AccessRadius == 0. This migration fixes that.
    /// </summary>
    [StreamMigration(from: 6, to: 7)]
    public class Migration7AccessRadius : IStreamMigration
    {
        public async Task MigrateAsync(IStreamMigrationArgs e)
        {
            var events = e.GetExistingEvents();
            var accessRadius = new Dictionary<int, float>();

            while (await events.MoveNextAsync())
            {
                switch (events.Current)
                {
                    case ExhibitCreated created: accessRadius[created.Id] = created.Properties.AccessRadius; break;
                    case ExhibitDeleted deleted: accessRadius.Remove(deleted.Id); break;

                    case ExhibitUpdated updated:
                        // ReSharper disable once CompareOfFloatsByEqualityOperator
                        if (updated.Properties.AccessRadius == 0)
                            updated.Properties.AccessRadius = accessRadius[updated.Id];
                        break;
                }
                
                e.AppendEvent(events.Current);
            }
        }
    }
}
