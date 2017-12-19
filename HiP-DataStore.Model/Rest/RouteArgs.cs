using PaderbornUniversity.SILab.Hip.DataStore.Model.Utility;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PaderbornUniversity.SILab.Hip.DataStore.Model.Rest
{
    public class RouteArgs : IContentArgs
    {
        [Required]
        public string Title { get; set; }

        public string Description { get; set; }

        [Range(0, int.MaxValue)]
        public int Duration { get; set; }

        [Range(0, double.PositiveInfinity)]
        public double Distance { get; set; }

        [Reference(nameof(ResourceTypes.Media))]
        public int? Image { get; set; }
        [Reference(nameof(ResourceTypes.Media))]
        public int? Audio { get; set; }

        [Reference(nameof(ResourceTypes.Exhibit))]
        public List<int> Exhibits { get; set; }

        [AllowedStatuses]
        public ContentStatus Status { get; set; }

        [Reference(nameof(ResourceTypes.Tag))]
        public List<int> Tags { get; set; }
    }
}
