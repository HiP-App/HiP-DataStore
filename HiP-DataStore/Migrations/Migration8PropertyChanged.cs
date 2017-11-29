using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using PaderbornUniversity.SILab.Hip.EventSourcing.Events;
using PaderbornUniversity.SILab.Hip.EventSourcing.Migrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PaderbornUniversity.SILab.Hip.DataStore.Migrations
{
    public class Migration8PropertyChanged : IStreamMigration
    {
        private Dictionary<(ResourceType, int), object> dictionary = new Dictionary<(ResourceType, int), object>();

        public async Task MigrateAsync(IStreamMigrationArgs e)
        {
            var events = e.GetExistingEvents();
            IEnumerable<PropertyChangedEvent> propEvents = new List<PropertyChangedEvent>();
            DateTimeOffset timestamp;
            while (await events.MoveNextAsync())
            {
                var currentEvent = events.Current;
                if (currentEvent is UserActivityBaseEvent userEvent)
                {
                    switch (currentEvent)
                    {
                        case ICreateEvent ev:
                            e.AppendEvent(new CreatedEvent(ev.GetEntityType().Name, ev.Id, userEvent.UserId)
                            {
                                Timestamp = ev.Timestamp
                            });
                            break;

                        case ExhibitUpdated ev:
                            var newArgs = ev.Properties;
                            var currentArgs = (ExhibitArgs)dictionary[(ev.GetEntityType(), ev.Id)];
                            propEvents = EntityManager.CompareEntities(currentArgs, newArgs, ev.GetEntityType(), ev.Id, ev.UserId);
                            break;

                        case ExhibitPageUpdated3 ev:
                            timestamp = ev.Timestamp;
                            var newPageArgs = ev.Properties;
                            var currentPageArgs = (ExhibitPageArgs2)dictionary[(ev.GetEntityType(), ev.Id)];
                            propEvents = EntityManager.CompareEntities(currentPageArgs, newPageArgs, ev.GetEntityType(), ev.Id, ev.UserId);
                            break;
                        case RouteUpdated ev:
                            timestamp = ev.Timestamp;
                            var newRouteArgs = ev.Properties;
                            var currentRouteArgs = (RouteArgs)dictionary[(ev.GetEntityType(), ev.Id)];
                            propEvents = EntityManager.CompareEntities(currentRouteArgs, newRouteArgs, ev.GetEntityType(), ev.Id, ev.UserId);
                            break;
                        case TagUpdated ev:
                            timestamp = ev.Timestamp;
                            var newTagArgs = ev.Properties;
                            var currentTagArgs = (TagArgs)dictionary[(ev.GetEntityType(), ev.Id)];
                            propEvents = EntityManager.CompareEntities(currentTagArgs, newTagArgs, ev.GetEntityType(), ev.Id, ev.UserId);
                            break;

                        case MediaUpdate ev:
                            timestamp = ev.Timestamp;
                            var newMediaArgs = ev.Properties;
                            var currentMediaArgs = (MediaArgs)dictionary[(ev.GetEntityType(), ev.Id)];
                            propEvents = EntityManager.CompareEntities(newMediaArgs, currentMediaArgs, ev.GetEntityType(), ev.Id, ev.UserId);
                            break;

                        case IDeleteEvent ev:
                            e.AppendEvent(new DeletedEvent(ev.GetEntityType().Name, ev.Id, userEvent.UserId)
                            {
                                Timestamp = ev.Timestamp
                            });
                            break;
                    }

                    foreach (var propEvent in propEvents)
                    {
                        propEvent.Timestamp = timestamp;
                        e.AppendEvent(propEvent);
                    }
                }
            }

        }
    }
}
