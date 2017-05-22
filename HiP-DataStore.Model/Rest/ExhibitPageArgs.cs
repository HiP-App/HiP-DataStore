using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class ExhibitPageArgs
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
    }
}
