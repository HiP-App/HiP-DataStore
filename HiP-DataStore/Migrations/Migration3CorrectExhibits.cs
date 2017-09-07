using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using PaderbornUniversity.SILab.Hip.EventSourcing.Migrations;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PaderbornUniversity.SILab.Hip.DataStore.Migrations
{
    /// <summary>
    /// On ExhibitCreated/ExhibitUpdated, 'Pages' may contain IDs of pages that are not yet created.
    /// This migration corrects that by removing the invalid page IDs.
    /// The issue originates from a (now fixed) bug at the time of migration to stream version 2.
    /// </summary>
    [StreamMigration(from: 2, to: 3)]
    public class Migration3CorrectExhibits : IStreamMigration
    {
        public async Task MigrateAsync(IStreamMigrationArgs e)
        {
            var events = e.GetExistingEvents();
            var pages = new HashSet<int>(); // IDs of all currently existing pages

            while (await events.MoveNextAsync())
            {
                switch (events.Current)
                {
                    case ExhibitCreated ev:
                        ev.Properties.Pages = ev.Properties.Pages?.Where(pages.Contains).ToList();
                        break;

                    case ExhibitUpdated ev:
                        ev.Properties.Pages = ev.Properties.Pages?.Where(pages.Contains).ToList();
                        break;

                    case ExhibitPageCreated3 ev:
                        pages.Add(ev.Id);
                        break;

                    case ExhibitPageDeleted2 ev:
                        pages.Remove(ev.Id);
                        break;
                }

                e.AppendEvent(events.Current);
            }
        }
    }
}
