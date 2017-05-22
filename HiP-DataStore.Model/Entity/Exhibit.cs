using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using System.Linq;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    public class Exhibit : ContentBase
    {
        [BsonElement(nameof(Image))]
        private DocRef<MediaElement> _image = new DocRef<MediaElement>(ResourceType.Media.Name);

        [BsonElement(nameof(Pages))]
        private DocRefList<ExhibitPage> _pages = new DocRefList<ExhibitPage>(ResourceType.ExhibitPage.Name);

        [BsonElement(nameof(Tags))]
        private DocRefList<Tag> _tags = new DocRefList<Tag>(ResourceType.Tag.Name);

        public string Name { get; set; }

        public string Description { get; set; }

        public float Latitude { get; set; }

        public float Longitude { get; set; }

        public DocRef<MediaElement> Image => _image;

        public DocRefList<ExhibitPage> Pages => _pages;

        public DocRefList<Tag> Tags => _tags;

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
        }
    }
}
