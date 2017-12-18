using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using PaderbornUniversity.SILab.Hip.DataStore.Utility;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using PaderbornUniversity.SILab.Hip.EventSourcing.EventStoreLlp;
using PaderbornUniversity.SILab.Hip.EventSourcing.Mongo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Tag = PaderbornUniversity.SILab.Hip.DataStore.Model.Entity.Tag;
using ResourceType = PaderbornUniversity.SILab.Hip.DataStore.Model.ResourceType; // TODO: Remove after architectural changes
using PaderbornUniversity.SILab.Hip.DataStore.MongoTemp;

namespace PaderbornUniversity.SILab.Hip.DataStore.Core.ReadModel
{
    /// <summary>
    /// Subscribes to EventStore events to keep the cache database up to date.
    /// </summary>
    public class CacheDatabaseManager
    {
        private readonly EventStoreService _eventStore;
        private readonly IMongoDbContext _db;

        public IMongoDbContext Database => _db;

        public CacheDatabaseManager(
            IMongoDbContext db,
            EventStoreService eventStore,
            ILogger<CacheDatabaseManager> logger)
        {
            // For now, the cache database is always created from scratch by replaying all events.
            // This also implies that, for now, the cache database always contains the entire data (not a subset).
            // In order to receive all the events, a Catch-Up Subscription is created.
            
            // Subscribe to EventStore to receive all past and future events
            _eventStore = eventStore;
            _eventStore.EventStream.SubscribeCatchUp(ApplyEvent);
        }
        
        private void ApplyEvent(IEvent ev)
        {
            if (ev is ICrudEvent crudEvent)
            {
                var entity = (crudEvent.GetEntityType(), crudEvent.Id);
                if (crudEvent is IDeleteEvent)
                {
                    _db.ClearIncomingReferences(entity);
                    _db.ClearOutgoingReferences(entity);
                }
                else if (crudEvent is IUpdateEvent)
                {
                    _db.ClearOutgoingReferences(entity);
                }
            }

            switch (ev)
            {
                case ExhibitCreated e:
                    var newExhibit = new Exhibit(e.Properties)
                    {
                        Id = e.Id,
                        UserId = e.UserId,
                        Timestamp = e.Timestamp
                    };

                    _db.Add(ResourceType.Exhibit, newExhibit);
                    break;

                case ExhibitUpdated e:
                    var originalExhibit = _db.Get<Exhibit>((ResourceType.Exhibit, e.Id));

                    var updatedExhibit = new Exhibit(e.Properties)
                    {
                        Id = e.Id,
                        UserId = originalExhibit.UserId,
                        Timestamp = e.Timestamp
                    };

                    updatedExhibit.Referencers.AddRange(originalExhibit.Referencers);
                    _db.Replace((ResourceType.Exhibit, e.Id), updatedExhibit);
                    break;

                case ExhibitDeleted e:
                    MarkDeleted((ResourceType.Exhibit, e.Id));
                    break;

                case ExhibitPageCreated3 e:
                    var newPage = new ExhibitPage(e.Properties)
                    {
                        Id = e.Id,
                        UserId = e.UserId,
                        Timestamp = e.Timestamp
                    };

                    _db.Add(ResourceType.ExhibitPage, newPage);
                    break;

                case ExhibitPageUpdated3 e:
                    var originalPage = _db.Get<ExhibitPage>((ResourceType.ExhibitPage, e.Id));
                    var updatedPage = new ExhibitPage(e.Properties)
                    {
                        Id = e.Id,
                        UserId = originalPage.UserId,
                        Timestamp = e.Timestamp
                    };

                    updatedPage.Referencers.AddRange(originalPage.Referencers);
                    _db.Replace((ResourceType.ExhibitPage, e.Id), updatedPage);
                    break;

                case ExhibitPageDeleted2 e:
                    MarkDeleted((ResourceType.ExhibitPage, e.Id));
                    break;

                case RouteCreated e:
                    var newRoute = new Route(e.Properties)
                    {
                        Id = e.Id,
                        UserId = e.UserId,
                        Timestamp = e.Timestamp
                    };

                    _db.Add(ResourceType.Route, newRoute);
                    break;

                case RouteUpdated e:
                    var originalRoute = _db.Get<Route>((ResourceType.Route, e.Id));
                    var updatedRoute = new Route(e.Properties)
                    {
                        Id = e.Id,
                        UserId = originalRoute.UserId,
                        Timestamp = e.Timestamp
                    };

                    updatedRoute.Referencers.AddRange(originalRoute.Referencers);
                    _db.Replace((ResourceType.Route, e.Id), updatedRoute);
                    break;

                case RouteDeleted e:
                    MarkDeleted((ResourceType.Route, e.Id));
                    break;

                case MediaCreated e:
                    var newMedia = new MediaElement(e.Properties)
                    {
                        Id = e.Id,
                        UserId = e.UserId,
                        Timestamp = e.Timestamp
                    };

                    _db.Add(ResourceType.Media, newMedia);
                    break;

                case MediaUpdate e:
                    var originalMedia = _db.Get<MediaElement>((ResourceType.Media, e.Id));
                    var updatedMedia = new MediaElement(e.Properties)
                    {
                        Id = e.Id,
                        UserId = originalMedia.UserId,
                        Timestamp = e.Timestamp
                    };

                    updatedMedia.Referencers.AddRange(originalMedia.Referencers);
                    updatedMedia.File = originalMedia.File;
                    _db.Replace((ResourceType.Media, e.Id), updatedMedia);
                    break;

                case MediaDeleted e:
                    MarkDeleted((ResourceType.Media, e.Id));
                    break;

                case MediaFileUpdated e:
                    var fileDocBson = e.ToBsonDocument();
                    fileDocBson.Remove("_id");
                    var bsonDoc = new BsonDocument("$set", fileDocBson);

                    _db.Update<MediaElement>((ResourceType.Media, e.Id), update =>
                    {

                    });
                    break;

                case TagCreated e:
                    var newTag = new Tag(e.Properties)
                    {
                        Id = e.Id,
                        UserId = e.UserId,
                        Timestamp = e.Timestamp,
                    };

                    _db.Add(ResourceType.Tag, newTag);
                    break;

                case TagUpdated e:
                    var originalTag = _db.Get<Tag>((ResourceType.Tag, e.Id));
                    var updatedTag = new Tag(e.Properties)
                    {
                        Id = e.Id,
                        UserId = originalTag.UserId,
                        Timestamp = e.Timestamp,
                    };

                    updatedTag.Referencers.AddRange(originalTag.Referencers);
                    _db.Replace((ResourceType.Tag, e.Id), updatedTag);
                    break;

                case TagDeleted e:
                    MarkDeleted((ResourceType.Tag, e.Id));
                    break;

                case ScoreAdded e:
                    var newScoreRecord = new ScoreRecord
                    {
                        Id = e.Id,
                        UserId = e.UserId,
                        Score = e.Score,
                        Timestamp = e.Timestamp
                    };
                    _db.Add(ResourceType.ScoreRecord, newScoreRecord);
                    break;
            }

            if (ev is ICreateEvent createEvent)
                _db.AddReferences((createEvent.GetEntityType(), createEvent.Id), createEvent.GetReferences());
            else if (ev is IUpdateEvent updateEvent)
                _db.AddReferences((updateEvent.GetEntityType(), updateEvent.Id), updateEvent.GetReferences());
        }

        private void MarkDeleted(EntityId entity)
        {
            var result = _db.Update<ContentBase>(entity, update => update.Set(o => o.Status, ContentStatus.Published));
        }
    }
}
