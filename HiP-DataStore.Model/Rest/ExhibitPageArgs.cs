using PaderbornUniversity.SILab.Hip.DataStore.Model.Events;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    // Version info: 'Images' now stores not just a list of IDs, but a list of pairs (Date, Image ID)
    public class ExhibitPageArgs2
    {
        [Required]
        public PageType Type { get; set; }

        public string Title { get; set; }

        [Required]
        public string Text { get; set; }

        public string Description { get; set; }

        public string FontFamily { get; set; }

        public int? Audio { get; set; }

        public int? Image { get; set; }

        public IReadOnlyCollection<SliderPageImageArgs> Images { get; set; }

        public bool? HideYearNumbers { get; set; }

        public IReadOnlyCollection<int> AdditionalInformationPages { get; set; }

        public ContentStatus Status { get; set; }
    }

    public class ExhibitPageArgs : IMigratable<ExhibitPageArgs2>
    {
        [Required]
        public PageType Type { get; set; }

        public string Title { get; set; }

        [Required]
        public string Text { get; set; }

        public string Description { get; set; }

        public string FontFamily { get; set; }

        public int? Audio { get; set; }

        public int? Image { get; set; }

        public IReadOnlyCollection<int> Images { get; set; }

        public bool? HideYearNumbers { get; set; }

        public IReadOnlyCollection<int> AdditionalInformationPages { get; set; }

        public ContentStatus Status { get; set; }

        public ExhibitPageArgs2 Migrate() => new ExhibitPageArgs2
        {
            // for migration of the 'Images' property, we have to choose an arbitrary default date
            Images = Images?.Select(imageId => new SliderPageImageArgs
            {
                Date = 2017,
                Image = imageId
            }).ToList(),

            // the other properties are just copied over
            Type = Type,
            Title = Title,
            Text = Text,
            Description = Description,
            FontFamily = FontFamily,
            Audio = Audio,
            Image = Image,
            HideYearNumbers = HideYearNumbers,
            AdditionalInformationPages = AdditionalInformationPages,
            Status = Status
        };
    }
}
