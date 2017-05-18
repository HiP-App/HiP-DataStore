﻿using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using Tag = PaderbornUniversity.SILab.Hip.DataStore.Model.Entity.Tag;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using PaderbornUniversity.SILab.Hip.DataStore.Utility;
using System;
using System.Linq;
using System.Collections.Generic;

namespace PaderbornUniversity.SILab.Hip.DataStore.Core.ReadModel
{
    /// <summary>
    /// Subscribes to EventStore events to keep the cache database up to date.
    /// </summary>
    public class CacheDatabaseManager
    {
        private readonly EventStoreClient _eventStore;
        private readonly IMongoDatabase _db;
        private readonly ILogger<CacheDatabaseManager> _logger;

        public IMongoDatabase Database => _db;

        public CacheDatabaseManager(
            EventStoreClient eventStore,
            IOptions<EndpointConfig> config,
            ILogger<CacheDatabaseManager> logger)
        {
            // For now, the cache database is always created from scratch by replaying all events.
            // This also implies that, for now, the cache database always contains the entire data (not a subset).
            // In order to receive all the events, a Catch-Up Subscription is created.

            _logger = logger;

            // 1) Open MongoDB connection and clear existing database
            var mongo = new MongoClient(config.Value.MongoDbHost);
            mongo.DropDatabase(config.Value.MongoDbName);
            _db = mongo.GetDatabase(config.Value.MongoDbName);

            // 2) Subscribe to EventStore to receive all past and future events
            _eventStore = eventStore;

            _eventStore.Connection.SubscribeToStreamFrom(
                EventStoreClient.DefaultStreamName,
                null, // don't use StreamPosition.Start (see https://groups.google.com/forum/#!topic/event-store/8tpXJMNEMqI),
                CatchUpSubscriptionSettings.Default,
                OnEventAppeared);
        }

        private void OnEventAppeared(EventStoreCatchUpSubscription subscription, ResolvedEvent resolvedEvent)
        {
            try
            {
                var ev = resolvedEvent.Event.ToIEvent();

                switch (ev)
                {
                    case ExhibitCreated e:
                        var newExhibit = new Exhibit
                        {
                            Id = e.Id,
                            Name = e.Properties.Name,
                            Description = e.Properties.Description,
                            Image = { Id = e.Properties.Image },
                            Latitude = e.Properties.Latitude,
                            Longitude = e.Properties.Longitude,
                            Status = e.Properties.Status,
                            Tags = { e.Properties.Tags?.Select(id => (BsonValue)id) },
                            Timestamp = DateTimeOffset.Now
                        };

                        _db.GetCollection<Exhibit>(ResourceType.Exhibit.Name).InsertOne(newExhibit);
                        break;

                    case ExhibitDeleted e:
                        _db.GetCollection<Exhibit>(ResourceType.Exhibit.Name).DeleteOne(x => x.Id == e.Id);
                        break;

                    case RouteCreated e:
                        var newRoute = new Route
                        {
                            Id = e.Id,
                            Title = e.Properties.Title,
                            Description = e.Properties.Description,
                            Duration = e.Properties.Duration,
                            Distance = e.Properties.Distance,
                            Image = { Id = e.Properties.Image },
                            Audio = { Id = e.Properties.Audio },
                            Exhibits = { e.Properties.Exhibits?.Select(id => (BsonValue)id) },
                            Status = e.Properties.Status,
                            Tags = { e.Properties.Tags?.Select(id => (BsonValue)id) },
                            Timestamp = DateTimeOffset.Now
                        };

                        _db.GetCollection<Route>(ResourceType.Route.Name).InsertOne(newRoute);
                        break;

                    case RouteDeleted e:
                        _db.GetCollection<Route>(ResourceType.Route.Name).DeleteOne(r => r.Id == e.Id);
                        break;

                    case MediaCreated e:
                        var newMedia = new MediaElement
                        {
                            Id = e.Id,
                            Title = e.Properties.Title,
                            Description = e.Properties.Description,
                            Type = e.Properties.Type,
                            Status = e.Properties.Status,
                            Timestamp = DateTimeOffset.Now
                        };

                        _db.GetCollection<MediaElement>(ResourceType.Media.Name).InsertOne(newMedia);
                        break;

                    case MediaDeleted e:
                        _db.GetCollection<MediaElement>(ResourceType.Media.Name).DeleteOne(m => m.Id == e.Id);
                        break;

                    case MediaUpdate e:

                        var filter = Builders<MediaElement>.Filter.Eq(x => x.Id, e.Id);
                        var timestamp = new { Timestamp = e.Timestamp }.ToBsonDocument();
                        var bsonDoc = new BsonDocument("$set", e.Properties.ToBsonDocument().AddRange(timestamp));

                        _db.GetCollection<MediaElement>(ResourceType.Media.Name).UpdateOne(filter, bsonDoc);
                        break;
                    case MediaFileUpdated e:
                        var fileDocBson = e.ToBsonDocument();
                        fileDocBson.Remove("Id");
                        bsonDoc = new BsonDocument("$set", fileDocBson);
                        _db.GetCollection<MediaElement>(ResourceType.Media.Name).UpdateOne(x => x.Id == e.Id, bsonDoc);
                        break;

                    case ReferenceAdded e:
                        // a reference (source -> target) was added, so we have to create a new DocRef pointing to the
                        // source and add it to the target's referencees list
                        var newReference = new DocRef<ContentBase>(e.SourceId, e.SourceType.Name);
                        var update = Builders<ContentBase>.Update.Push(nameof(ContentBase.Referencees), newReference);
                        _db.GetCollection<ContentBase>(e.TargetType.Name).UpdateOne(x => x.Id == e.TargetId, update);
                        break;
                case TagCreated e:
                    var newTag = new Tag
                    {
                        Id = e.Id,
                        Title = e.Properties.Tille,
                        Description = e.Properties.Description,
                        Status = e.Properties.Status,
                        Timestamp=DateTimeOffset.Now,
                        Image = { Id=e.Properties.Image },
                        IsUsed=false
                    };

                    newTag.Image.Id = e.Properties.Image;
                    _db.GetCollection<Tag>(ResourceType.Tag.Name).InsertOne(newTag);
                    break;
                case TagUpdated e:
                        timestamp = new { Timestamp = e.Timestamp }.ToBsonDocument();
                        bsonDoc = e.Properties.ToBsonDocument();
                        bsonDoc.AddRange(timestamp);
                        if (bsonDoc.Contains("Image"))
                            bsonDoc["Image"] = e.Image.ToBsonDocument();

                        bsonDoc = new BsonDocument("$set", bsonDoc);
                        _db.GetCollection<Tag>(ResourceType.Tag.Name).UpdateOne(x => x.Id == e.Id, bsonDoc);
                    break;

                    case ReferenceRemoved e:
                        // a reference (source -> target) was removed, so we have to delete the DocRef pointing to the
                        // source from the target's referencees list

                        // ladies and gentlemen, fasten your seatbelts and prepare for the
                        // ugly truth of the MongoDB API:
                        var update2 = Builders<dynamic>.Update.PullFilter(
                            nameof(ContentBase.Referencees),
                            Builders<dynamic>.Filter.And(
                                Builders<dynamic>.Filter.Eq(nameof(DocRefBase.Collection), e.SourceType.Name),
                                Builders<dynamic>.Filter.Eq("_id", e.SourceId)));
                        
                        _db.GetCollection<dynamic>(e.TargetType.Name).UpdateOne(
                            Builders<dynamic>.Filter.Eq("_id", e.TargetId), update2);
                        break;

                        // TODO: Handle further events
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning($"{nameof(CacheDatabaseManager)} could not process an event: {e}");
            }
        }
    }
}
