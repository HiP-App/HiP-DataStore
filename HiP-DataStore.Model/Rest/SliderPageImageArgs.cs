using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class SliderPageImageArgs
    {
        public int Date { get; set; }
        public int Image { get; set; }
    }

    public class SliderPageImageResult
    {
        public int Date { get; set; }
        public int Image { get; set; }

        public SliderPageImageResult(SliderPageImage img)
        {
            Date = img.Date;
            Image = (int)img.Image.Id;
        }
    }
}
