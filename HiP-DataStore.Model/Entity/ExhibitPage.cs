using PaderbornUniversity.SILab.Hip.DataStore.Model.Rest;
using System.Collections.Generic;
using System.Linq;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Entity
{
    /// <remarks>
    /// An <see cref="ExhibitPage"/> is part of an <see cref="Exhibit"/> document.
    /// There is no need to store pages in their own Mongo collection, since a page is only ever
    /// referenced by a single exhibit.
    /// </remarks>
    public class ExhibitPage : ContentBase
    {
        public PageType Type { get; set; }

        public string Title { get; set; }

        public string Text { get; set; }

        public string Description { get; set; }

        public string FontFamily { get; set; }

        public int? Audio { get; set; }

        public int? Image { get; set; }

        public List<SliderPageImage> Images { get; private set; } = new List<SliderPageImage>();

        public bool HideYearNumbers { get; set; }

        public List<int> AdditionalInformationPages { get; set; } = new List<int>();

        public ExhibitPage()
        {
        }

        public ExhibitPage(ExhibitPageArgs2 args)
        {
            Type = args.Type;
            Title = args.Title;
            Text = args.Text;
            Description = args.Description;
            FontFamily = args.FontFamily;
            Audio = args.Audio;
            Image = args.Image;
            Images = args.Images?.Select(img => new SliderPageImage(img)).ToList() ?? new List<SliderPageImage>();
            HideYearNumbers = args.HideYearNumbers ?? false;
            AdditionalInformationPages = args.AdditionalInformationPages?.ToList() ?? new List<int>();
            Status = args.Status;
        }

        public ExhibitPageArgs2 CreateExhibitPageArgs()
        {
            var args = new ExhibitPageArgs2();
            args.Type = Type;
            args.Title = Title;
            args.Text = Text;
            args.Description = Description;
            args.FontFamily = FontFamily;
            args.Audio = Audio;
            args.Image = Image;
            args.Images = Images?.Select(i => new SliderPageImageArgs() { Date = i.Date, Image = i.Image }).ToList();
            args.HideYearNumbers = HideYearNumbers;
            args.AdditionalInformationPages = AdditionalInformationPages;
            args.Status = Status;
            return args;
        }
    }
}
