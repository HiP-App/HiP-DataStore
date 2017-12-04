using PaderbornUniversity.SILab.Hip.DataStore.Model.Entity;
using System.Collections.Generic;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class SliderPageImageArgs : IReference
    {
        public string Date { get; set; }
        public int Image { get; set; }

        public int ReferenceId => Image;

        public override bool Equals(object obj)
        {
            var args = obj as SliderPageImageArgs;
            return args != null &&
                   Date == args.Date &&
                   Image == args.Image &&
                   ReferenceId == args.ReferenceId;
        }

        public override int GetHashCode()
        {
            var hashCode = 2128754378;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Date);
            hashCode = hashCode * -1521134295 + Image.GetHashCode();
            hashCode = hashCode * -1521134295 + ReferenceId.GetHashCode();
            return hashCode;
        }
    }

    public class SliderPageImageResult
    {
        public string Date { get; set; }
        public int Image { get; set; }

        public SliderPageImageResult(SliderPageImage img)
        {
            Date = img.Date;
            Image = (int)img.Image.Id;
        }
    }
}
