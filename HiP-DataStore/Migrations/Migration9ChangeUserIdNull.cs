using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using PaderbornUniversity.SILab.Hip.EventSourcing.Migrations;
using System.Threading.Tasks;

namespace PaderbornUniversity.SILab.Hip.DataStore.Migrations
{    /// <summary>
     /// Updating to version 9. We have events in our EventStore that have UserId null. That`s not right. 
     /// In our migration we will replace them with admin userId = auth0|5968ed8cdd1b3733ca94865d
     /// </summary>
    [StreamMigration(from: 8, to: 9)]
    public class Migration9ChangeUserIdNull : IStreamMigration
    {
        public async Task MigrateAsync(IStreamMigrationArgs e)
        {
            var events = e.GetExistingEvents();
            string adminId = "auth0|5968ed8cdd1b3733ca94865d";
            while (await events.MoveNextAsync())
            {
                switch (events.Current)
                {
                    case RatingAdded ev:
                        if (ev.UserId == null)
                            ev.UserId = adminId;
                        break;
                }
                e.AppendEvent(events.Current);
            }
        }
    }
}
