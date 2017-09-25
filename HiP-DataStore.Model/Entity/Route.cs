using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using PaderbornUniversity.SILab.Hip.EventSourcing.Mongo;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    public class Route : ContentBase
    {
        // TODO: What about waypoints?
        
        public string Title { get; set; }

        public string Description { get; set; }

        public int Duration { get; set; }

        public double Distance { get; set; }

        [BsonElement]
        public DocRef<MediaElement> Image { get; private set; } =
            new DocRef<MediaElement>(ResourceType.Media.Name);

        [BsonElement]
        public DocRef<MediaElement> Audio { get; private set; } =
            new DocRef<MediaElement>(ResourceType.Media.Name);

        [BsonElement]
        public DocRefList<Exhibit> Exhibits { get; private set; } =
            new DocRefList<Exhibit>(ResourceType.Exhibit.Name);

        [BsonElement]
        public DocRefList<Tag> Tags { get; private set; } =
            new DocRefList<Tag>(ResourceType.Tag.Name);

        public Route()
        {
        }

        public Route(RouteArgs args)
        {
            Title = args.Title;
            Description = args.Description;
            Duration = args.Duration;
            Distance = args.Distance;
            Image.Id = args.Image;
            Audio.Id = args.Audio;
            Exhibits.Add(args.Exhibits?.Select(id => (BsonValue)id));
            Status = args.Status;
            Tags.Add(args.Tags?.Select(id => (BsonValue)id));
        }
    }
}
