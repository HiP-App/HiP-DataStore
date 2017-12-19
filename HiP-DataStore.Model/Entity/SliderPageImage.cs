using MongoDB.Bson.Serialization.Attributes;
using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using PaderbornUniversity.SILab.Hip.EventSourcing.Mongo;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    public class SliderPageImage
    {
        public string Date { get; set; }

        [ResourceReference(nameof(ResourceTypes.Media))]
        public int Image { get; set; }

        public SliderPageImage()
        {
        }

        public SliderPageImage(SliderPageImageArgs args)
        {
            Date = args.Date;
            Image = args.Image;
        }
    }
}
