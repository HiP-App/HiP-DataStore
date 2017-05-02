using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    public class Exhibit : ContentBase
    {
        public const string CollectionName = "exhibits";

        [BsonElement(nameof(Image))]
        private DocRef<Image> _image = new DocRef<Image>(Entity.Image.CollectionName);

        [BsonElement(nameof(Pages))]
        private List<ExhibitPage> _pages = new List<ExhibitPage>();

        [BsonElement(nameof(Tags))]
        private DocRefList<Tag> _tags = new DocRefList<Tag>();

        public ObjectId Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public float Latitude { get; set; }

        public float Longitude { get; set; }

        public DocRef<Image> Image => _image;

        public IList<ExhibitPage> Pages => _pages;

        public DocRefList<Tag> Tags => _tags;
    }
}
