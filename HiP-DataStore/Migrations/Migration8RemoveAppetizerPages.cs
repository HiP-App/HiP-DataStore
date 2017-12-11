using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using PaderbornUniversity.SILab.Hip.EventSourcing.Migrations;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PaderbornUniversity.SILab.Hip.DataStore.Migrations
{
    /// <summary>
    /// Removes exhibit pages of type APPETIZER_PAGE.
    /// In more detail: Removes all ExhibitPageCreated/-Updated/-Deleted events for appetizer pages and
    /// modifies ExhibitCreated/-Updated events so they no longer reference these deleted pages.
    /// </summary>
    [StreamMigration(from: 7, to: 8)]
    public class Migration8RemoveAppetizerPages : IStreamMigration
    {
        // Enum member PageType.Appetizer_Page had value 0 before it was removed
        private const PageType AppetizerPageType = 0;

        public async Task MigrateAsync(IStreamMigrationArgs e)
        {
            var events = e.GetExistingEvents();
            var appetizerPageIds = new HashSet<int>();

            while (await events.MoveNextAsync())
            {
                switch (events.Current)
                {
                    case ExhibitPageCreated3 ev:
                        if (ev.Properties.Type == AppetizerPageType)
                        {
                            // remember ID, do not append this event to new stream
                            appetizerPageIds.Add(ev.Id);
                        }
                        else
                        {
                            // pages of other types are kept, but first filter out referenced appetizer pages
                            ev.Properties.AdditionalInformationPages =
                                ev.Properties.AdditionalInformationPages.Except(appetizerPageIds).ToList();
                            e.AppendEvent(ev);
                        }
                        break;

                    case ExhibitPageUpdated3 ev:
                        // do not append update-event for appetizer pages
                        // (note: page type can't be changed in update-events)
                        if (!appetizerPageIds.Contains(ev.Id))
                        {                        
                            // filter out referenced appetizer pages
                            ev.Properties.AdditionalInformationPages =
                                ev.Properties.AdditionalInformationPages.Except(appetizerPageIds).ToList();
                            e.AppendEvent(ev);
                        }
                        break;

                    case ExhibitPageDeleted2 ev:
                        // do not append delete-event for appetizer pages
                        if (!appetizerPageIds.Remove(ev.Id))
                            e.AppendEvent(ev);
                        break;

                    case ExhibitCreated ev:
                        // filter out referenced appetizer pages
                        ev.Properties.Pages = ev.Properties.Pages.Except(appetizerPageIds).ToList();
                        e.AppendEvent(ev);
                        break;

                    case ExhibitUpdated ev:
                        // filter out referenced appetizer pages
                        ev.Properties.Pages = ev.Properties.Pages.Except(appetizerPageIds).ToList();
                        e.AppendEvent(ev);
                        break;

                    default:
                        // all other events are copied without modification
                        e.AppendEvent(events.Current);
                        break;
                }
            }
        }
    }
}
