using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    public class SliderPageImage
    {
        public string Date { get; set; }

        public DocRef<MediaElement> Image { get; set; } =
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
