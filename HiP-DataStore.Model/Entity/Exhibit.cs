using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using PaderbornUniversity.SILab.Hip.EventSourcing.Mongo;
using System.Linq;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    public class Exhibit : ContentBase
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public float Latitude { get; set; }

        public float Longitude { get; set; }

        public float AccessRadius { get; set; }

        [BsonElement]
        public DocRef<MediaElement> Image { get; private set; } =
            new DocRef<MediaElement>(ResourceTypes.Media.Name);

        [BsonElement]
        public DocRefList<Tag> Tags { get; private set; } =
            new DocRefList<Tag>(ResourceTypes.Tag.Name);

        [BsonElement]
        public DocRefList<ExhibitPage> Pages { get; private set; } =
            new DocRefList<ExhibitPage>(ResourceTypes.ExhibitPage.Name);

        public Exhibit()
        {
        }

        public Exhibit(ExhibitArgs args)
        {
            Name = args.Name;
            Description = args.Description;
            Image.Id = args.Image;
            Latitude = args.Latitude;
            Longitude = args.Longitude;
            Status = args.Status;
            Tags.Add(args.Tags?.Select(id => (BsonValue)id));
            Pages.Add(args.Pages?.Select(id => (BsonValue)id));
            AccessRadius = args.AccessRadius;
        }

        public ExhibitArgs CreateExhibitArgs()
        {
            var args = new ExhibitArgs();
            args.Name = Name;
            args.Description = Description;
            args.Image = Image.Id.AsNullableInt32;
            args.Latitude = Latitude;
            args.Longitude = Longitude;
            args.Status = Status;
            args.Tags = Tags?.Ids.Select(id => id.AsInt32).ToList();
            args.Pages = Pages?.Ids.Select(id => id.AsInt32).ToList();
            args.AccessRadius = AccessRadius;
            return args;
        }
    }
}
