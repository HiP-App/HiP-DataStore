using MongoDB.Bson.Serialization.Attributes;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using PaderbornUniversity.SILab.Hip.EventSourcing.Mongo;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    public class Tag : ContentBase
    {
        public string Title { get; set; }

        public string Description { get; set; }

        [BsonElement]
        public DocRef<MediaElement> Image { get; private set; } =
            new DocRef<MediaElement>(ResourceTypes.Media.Name);

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

        public TagArgs CreateTagArgs()
        {
            var args = new TagArgs();
            args.Title = Title;
            args.Description = Description;
            args.Image = Image.Id.AsNullableInt32;
            args.Status = Status;
            return args;
        }
    }
}
