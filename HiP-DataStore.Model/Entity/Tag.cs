using MongoDB.Bson.Serialization.Attributes;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    public class Tag : ContentBase
    {
        [BsonElement(nameof(Image))]
        private DocRef<MediaElement> _image = new DocRef<MediaElement>(ResourceType.Media.Name);

        public string Title { get; set; }

        public string Description { get; set; }

        public DocRef<MediaElement> Image => _image;

        public Tag()
        {
        }

        public Tag(TagArgs args)
        {
            Title = args.Title;
            Description = args.Description;
            Image.Id = args.Image;
            Status = args.Status;
        }
    }
}
