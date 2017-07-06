using PaderbornUniversity.SILab.Hip.DataStore.Core.Migrations;
using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

#pragma warning disable CS0612 // We explicitly work with obsolete types here, so disable warnings for that
namespace PaderbornUniversity.SILab.Hip.DataStore.Migrations
{
    /// <summary>
    /// Updates a stream to the new way of organizing exhibit pages, i.e.:
    /// * Pages no longer reference the containing exhibit
    /// * Exhibits now store an ordered array of their pages
    /// * Therefore, ExhibitPageDeleted-events now also store the exhibit ID so that
    ///   the page can be removed from the exhibit's pages array
    /// </summary>
    [StreamMigration(from: 0, to: 1)]
    public class PageOrderFeatureMigration : IStreamMigration
    {
        public async Task MigrateAsync(IStreamMigrationArgs e)
        {
            var events = e.GetExistingEvents();
            var pages = new Dictionary<int, List<int>>(); // stores the page IDs for each exhibit
            var exhibits = new Dictionary<int, int>(); // stores the exhibit ID for each page

            while (await events.MoveNextAsync())
            {
                switch (events.Current)
                {
                    case ReferenceAdded ev when ev.SourceType == ResourceType.ExhibitPage && ev.TargetType == ResourceType.Exhibit:
                    case ReferenceRemoved ev2 when ev2.SourceType == ResourceType.ExhibitPage && ev2.TargetType == ResourceType.Exhibit:
                        // pages no longer reference the containing exhibit => ignore such references
                        break;

                    case ExhibitUpdated ev:
                        // "PUT /Exhibit/{id}" now allows to reorder pages using the new 'pages'-field
                        // => emit new ExhibitUpdated-events with correctly populated 'pages' array
                        var pageIds = pages.TryGetValue(ev.Id, out var ids) ? ids : new List<int>();
                        e.AppendEvent(new ExhibitUpdated
                        {
                            Id = ev.Id,
                            Timestamp = DateTimeOffset.Now,
                            Properties = new ExhibitArgs
                            {
                                Pages = pageIds,
                                Description = ev.Properties.Description,
                                Image = ev.Properties.Image,
                                Latitude = ev.Properties.Latitude,
                                Longitude = ev.Properties.Longitude,
                                Name = ev.Properties.Name,
                                Status = ev.Properties.Status,
                                Tags = ev.Properties.Tags
                            }
                        });
                        break;

                    case ExhibitPageCreated2 ev:
                        // for each page, remember the exhibit containing it (we need it in case ExhibitPageDeleted)
                        exhibits[ev.Id] = ev.ExhibitId;

                        // ...and add the page ID to the exhibit's pages array
                        if (pages.TryGetValue(ev.ExhibitId, out var list))
                            list.Add(ev.Id);
                        else
                            pages[ev.ExhibitId] = new List<int> { ev.Id };

                        e.AppendEvent(ev);
                        break;

                    case ExhibitPageDeleted ev:
                        // In v0, ExhibitPageDeleted did not store the exhibit ID.
                        // So we have to find that one out here and emit a new ExhibitPageDeleted event.
                        var exhibitId = exhibits[ev.Id];
                        pages[exhibitId].Remove(ev.Id);

                        e.AppendEvent(new ExhibitPageDeleted
                        {
                            Id = ev.Id,
                            ExhibitId = exhibitId
                        });
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
#pragma warning restore CS0612 // Type or member is obsolete
