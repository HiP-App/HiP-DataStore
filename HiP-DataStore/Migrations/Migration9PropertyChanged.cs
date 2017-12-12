using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using PaderbornUniversity.SILab.Hip.EventSourcing.Events;
using PaderbornUniversity.SILab.Hip.EventSourcing.Migrations;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PaderbornUniversity.SILab.Hip.DataStore.Migrations
{
    [StreamMigration(from: 8, to: 9)]
    public class Migration9PropertyChanged : IStreamMigration
    {
        private Dictionary<(ResourceType, int), object> argumentDictionary = new Dictionary<(ResourceType, int), object>();

        public async Task MigrateAsync(IStreamMigrationArgs e)
        {
            var events = e.GetExistingEvents();
            DateTimeOffset timestamp;
            while (await events.MoveNextAsync())
            {
                var currentEvent = events.Current;
                IEnumerable<PropertyChangedEvent> propEvents = new List<PropertyChangedEvent>();
                if (currentEvent is UserActivityBaseEvent userEvent)
                {
                    switch (currentEvent)
                    {
                        case ExhibitCreated ev:
                            var emptyExhibitArgs = new ExhibitArgs();
                            e.AppendEvent(new CreatedEvent(ev.GetEntityType().Name, ev.Id, ev.UserId)
                            {
                                Timestamp = ev.Timestamp
                            });
                            propEvents = EntityManager.CompareEntities(emptyExhibitArgs, ev.Properties, ev.GetEntityType(), ev.Id, ev.UserId);
                            argumentDictionary[(ev.GetEntityType(), ev.Id)] = ev.Properties;
                            timestamp = ev.Timestamp;
                            break;

                        case ExhibitPageCreated3 ev:
                            var emptyPageArgs = new ExhibitPageArgs2();
                            e.AppendEvent(new CreatedEvent(ev.GetEntityType().Name, ev.Id, ev.UserId)
                            {
                                Timestamp = ev.Timestamp
                            });
                            propEvents = EntityManager.CompareEntities(emptyPageArgs, ev.Properties, ev.GetEntityType(), ev.Id, ev.UserId);
                            argumentDictionary[(ev.GetEntityType(), ev.Id)] = ev.Properties;
                            timestamp = ev.Timestamp;
                            break;

                        case MediaCreated ev:
                            var emptyMediaArgs = new MediaArgs();
                            e.AppendEvent(new CreatedEvent(ev.GetEntityType().Name, ev.Id, ev.UserId)
                            {
                                Timestamp = ev.Timestamp
                            });
                            propEvents = EntityManager.CompareEntities(emptyMediaArgs, ev.Properties, ev.GetEntityType(), ev.Id, ev.UserId);
                            argumentDictionary[(ev.GetEntityType(), ev.Id)] = ev.Properties;
                            timestamp = ev.Timestamp;
                            break;

                        case RouteCreated ev:
                            var emptyRouteArgs = new RouteArgs();
                            e.AppendEvent(new CreatedEvent(ev.GetEntityType().Name, ev.Id, ev.UserId)
                            {
                                Timestamp = ev.Timestamp
                            });
                            propEvents = EntityManager.CompareEntities(emptyRouteArgs, ev.Properties, ev.GetEntityType(), ev.Id, ev.UserId);
                            argumentDictionary[(ev.GetEntityType(), ev.Id)] = ev.Properties;
                            timestamp = ev.Timestamp;
                            break;

                        case TagCreated ev:
                            var emptyTagArgs = new TagArgs();
                            e.AppendEvent(new CreatedEvent(ev.GetEntityType().Name, ev.Id, ev.UserId)
                            {
                                Timestamp = ev.Timestamp
                            });
                            propEvents = EntityManager.CompareEntities(emptyTagArgs, ev.Properties, ev.GetEntityType(), ev.Id, ev.UserId);
                            argumentDictionary[(ev.GetEntityType(), ev.Id)] = ev.Properties;
                            timestamp = ev.Timestamp;
                            break;

                        case ExhibitUpdated ev:
                            timestamp = ev.Timestamp;
                            var newArgs = ev.Properties;
                            var currentArgs = (ExhibitArgs)argumentDictionary[(ev.GetEntityType(), ev.Id)];
                            propEvents = EntityManager.CompareEntities(currentArgs, newArgs, ev.GetEntityType(), ev.Id, ev.UserId);
                            argumentDictionary[(ev.GetEntityType(), ev.Id)] = ev.Properties;
                            break;

                        case ExhibitPageUpdated3 ev:
                            timestamp = ev.Timestamp;
                            var newPageArgs = ev.Properties;
                            var currentPageArgs = (ExhibitPageArgs2)argumentDictionary[(ev.GetEntityType(), ev.Id)];
                            propEvents = EntityManager.CompareEntities(currentPageArgs, newPageArgs, ev.GetEntityType(), ev.Id, ev.UserId);
                            argumentDictionary[(ev.GetEntityType(), ev.Id)] = ev.Properties;
                            break;

                        case RouteUpdated ev:
                            timestamp = ev.Timestamp;
                            var newRouteArgs = ev.Properties;
                            var currentRouteArgs = (RouteArgs)argumentDictionary[(ev.GetEntityType(), ev.Id)];
                            propEvents = EntityManager.CompareEntities(currentRouteArgs, newRouteArgs, ev.GetEntityType(), ev.Id, ev.UserId);
                            argumentDictionary[(ev.GetEntityType(), ev.Id)] = ev.Properties;
                            break;

                        case TagUpdated ev:
                            timestamp = ev.Timestamp;
                            var newTagArgs = ev.Properties;
                            var currentTagArgs = (TagArgs)argumentDictionary[(ev.GetEntityType(), ev.Id)];
                            propEvents = EntityManager.CompareEntities(currentTagArgs, newTagArgs, ev.GetEntityType(), ev.Id, ev.UserId);
                            argumentDictionary[(ev.GetEntityType(), ev.Id)] = ev.Properties;
                            break;

                        case MediaUpdate ev:
                            timestamp = ev.Timestamp;
                            var newMediaArgs = ev.Properties;
                            var currentMediaArgs = (MediaArgs)argumentDictionary[(ev.GetEntityType(), ev.Id)];
                            propEvents = EntityManager.CompareEntities(currentMediaArgs, newMediaArgs, ev.GetEntityType(), ev.Id, ev.UserId);
                            argumentDictionary[(ev.GetEntityType(), ev.Id)] = ev.Properties;
                            break;

                        case IDeleteEvent ev:
                            e.AppendEvent(new DeletedEvent(ev.GetEntityType().Name, ev.Id, userEvent.UserId)
                            {
                                Timestamp = ev.Timestamp
                            });
                            break;

                        default:
                            //append all other events
                            e.AppendEvent(events.Current);
                            break;
                    }


                    foreach (var propEvent in propEvents)
                    {
                        propEvent.Timestamp = timestamp;
                        e.AppendEvent(propEvent);
                    }
                }
                else
                {
                    e.AppendEvent(events.Current);
                }

            }

        }
    }
}
