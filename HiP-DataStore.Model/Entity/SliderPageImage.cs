using MongoDB.Bson.Serialization.Attributes;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using PaderbornUniversity.SILab.Hip.EventSourcing.Mongo;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    public class SliderPageImage
    {
        public string Date { get; set; }

        [BsonElement]
        public DocRef<MediaElement> Image { get; private set; } =
            new DocRef<MediaElement>(ResourceType.Media.Name);

        public SliderPageImage()
        {
        }

        public SliderPageImage(SliderPageImageArgs args)
        {
            Date = args.Date;
            Image.Id = args.Image;
        }
    }
}
