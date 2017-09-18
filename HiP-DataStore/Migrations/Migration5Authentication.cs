using PaderbornUniversity.SILab.Hip.DataStore.Core.Migrations;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using System.Threading.Tasks;

namespace PaderbornUniversity.SILab.Hip.DataStore.Migrations
{
    /// <summary>
    /// Updating to version 5. In version 5 were added Authentication. Each content has owner. 
    /// All old content is assigned to admin user (id:auth0|596c67a14349fc3abb248b58)
    /// </summary>
    [StreamMigration(from: 4, to: 5)]
    public class Migration5Authentication : IStreamMigration
    {
        public async Task MigrateAsync(IStreamMigrationArgs e)
        {
            var adminId = "auth0|5968ed8cdd1b3733ca94865d";
            var events = e.GetExistingEvents();


            while (await events.MoveNextAsync())
            {
                switch (events.Current)
                {
                    case UserActivityBaseEvent ev:
                        ev.UserId = ev.UserId ?? adminId;
                        e.AppendEvent(ev);
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
