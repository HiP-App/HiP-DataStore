using System.Collections.Generic;
using PaderbornUniversity.SILab.Hip.DataStore.Core.Migrations;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using System.Threading.Tasks;
using System.Linq;

#pragma warning disable CS0612 // We explicitly work with obsolete types here, so disable warnings for that
namespace PaderbornUniversity.SILab.Hip.DataStore.Migrations
{
    /// <summary>
    /// Updates a stream to a flat exhibit page hierarchy where pages can be reused across exhibits (similar to media).
    /// * Pages no longer store the ID of the containing exhibit (since multiple exhibits may now have the page)
    /// * Exhibits now store references to pages
    /// * (Pages keep storing references to additional information pages)
    /// </summary>
    [StreamMigration(from: 1, to: 2)]
    public class FlatPageHierarchyMigration : IStreamMigration
    {
        public async Task MigrateAsync(IStreamMigrationArgs e)
        {
            var events = e.GetExistingEvents();

            var exhibits = new Dictionary<int, ExhibitArgs>();

            while (await events.MoveNextAsync())
            {
                switch (events.Current)
                {
                    case ExhibitPageCreated2 pageCreated:
                        // In v1, 'ExhibitPageCreated'-events were for creating a page and adding it to an exhibit.
                        // For v2, this needs to be split into two events.
                        e.AppendEvent(new ExhibitPageCreated3
                        {
                            Id = pageCreated.Id,
                            Properties = pageCreated.Properties,
                            Timestamp = pageCreated.Timestamp
                        });

                        var exhibit = exhibits[pageCreated.ExhibitId];

                        e.AppendEvent(new ExhibitUpdated
                        {
                            Id = pageCreated.ExhibitId,
                            Timestamp = pageCreated.Timestamp,
                            Properties = new ExhibitArgs
                            {
                                Name = exhibit.Name,
                                Description = exhibit.Description,
                                Image = exhibit.Image,
                                Latitude = exhibit.Latitude,
                                Longitude = exhibit.Longitude,
                                Status = exhibit.Status,
                                Tags = exhibit.Tags,
                                Pages = exhibit.Pages.Append(pageCreated.Id).ToList()
                            }
                        });
                        break;

                    case ExhibitPageUpdated2 ev:
                        e.AppendEvent(new ExhibitPageUpdated3
                        {
                            Id = ev.Id,
                            Properties = ev.Properties,
                            Timestamp = ev.Timestamp
                        });
                        break;

                    case ExhibitPageDeleted ev:
                        e.AppendEvent(new ExhibitPageDeleted2 { Id = ev.Id });
                        break;

                    case ExhibitCreated ev:
                        exhibits[ev.Id] = ev.Properties;
                        e.AppendEvent(ev);
                        break;

                    case ExhibitUpdated ev:
                        exhibits[ev.Id] = ev.Properties;
                        e.AppendEvent(ev);
                        break;

                    case ExhibitDeleted ev:
                        exhibits.Remove(ev.Id);
                        e.AppendEvent(ev);
                        break;

                    default:
                        // all other events remain the same
                        e.AppendEvent(events.Current);
                        break;
                }
            }
        }
    }
}
#pragma warning restore CS0612
