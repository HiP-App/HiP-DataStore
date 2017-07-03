using MongoDB.Bson.Serialization.Attributes;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    public class Tag : ContentBase
    {
        public string Title { get; set; }

        public string Description { get; set; }

        [BsonElement]
        public DocRef<MediaElement> Image { get; private set; } =
            new DocRef<MediaElement>(ResourceType.Media.Name);

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
