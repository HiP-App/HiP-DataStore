using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    public class SliderPageImage
    {
        public string Date { get; set; }

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
